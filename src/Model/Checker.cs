using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Schema;

namespace XmlNotepad
{
    public enum Severity { None, Hint, Warning, Error }

    public abstract class ErrorHandler
    {
        public abstract void HandleError(Severity sev, string reason, string filename, int line, int col, object data);
    }

    public enum IntellisensePosition { OnNode, AfterNode, FirstChild }

    public class Checker : IDisposable
    {
        private XmlCache _cache;
        private XmlSchemaValidator _validator;
        private XmlSchemaInfo _info;
        private ErrorHandler _eh;
        private MyXmlNamespaceResolver _nsResolver;
        private Uri _baseUri;
        private Dictionary<XmlNode, XmlSchemaInfo> _typeInfo = new Dictionary<XmlNode, XmlSchemaInfo>();
        private XmlSchemaAttribute[] _expectedAttributes;
        private XmlSchemaParticle[] _expectedParticles;
        private XmlElement _node;
        private Hashtable _parents;
        private IntellisensePosition _position;

        internal const int SurHighStart = 0xd800;
        internal const int SurHighEnd = 0xdbff;
        internal const int SurLowStart = 0xdc00;
        internal const int SurLowEnd = 0xdfff;

        // Construct a checker for getting expected information about the given element.
        public Checker(XmlElement node, IntellisensePosition position)
        {
            this._node = node;
            this._position = position;
            _parents = new Hashtable();
            XmlNode p = node.ParentNode;
            while (p != null)
            {
                _parents[p] = p;
                p = p.ParentNode;
            }
        }

        public Checker(ErrorHandler eh)
        {
            this._eh = eh;
        }

        public XmlSchemaAttribute[] GetExpectedAttributes()
        {
            return this._expectedAttributes;
        }

        public XmlSchemaParticle[] GetExpectedParticles()
        {
            return this._expectedParticles;
        }

        public void ValidateContext(XmlCache xcache)
        {
            this._cache = xcache;
            if (string.IsNullOrEmpty(_cache.FileName))
            {
                _baseUri = null;
            }
            else
            {
                _baseUri = new Uri(new Uri(xcache.FileName), new Uri(".", UriKind.Relative));
            }

            SchemaResolver resolver = xcache.SchemaResolver as SchemaResolver;
            resolver.Handler = OnValidationEvent;
            XmlDocument doc = xcache.Document;
            this._info = new XmlSchemaInfo();
            this._nsResolver = new MyXmlNamespaceResolver(doc.NameTable);
            XmlSchemaSet set = new XmlSchemaSet();
            set.XmlResolver = resolver;
            // Make sure the SchemaCache is up to date with document.
            SchemaCache sc = xcache.SchemaCache;
            foreach (XmlSchema s in doc.Schemas.Schemas())
            {
                sc.Add(s);
            }

            if (LoadSchemas(doc, set, resolver))
            {
                set.ValidationEventHandler += OnValidationEvent;
                set.Compile();
                set.ValidationEventHandler -= OnValidationEvent;
            }

            try
            {
                this._validator = new XmlSchemaValidator(doc.NameTable, set, _nsResolver,
                    XmlSchemaValidationFlags.AllowXmlAttributes |
                    XmlSchemaValidationFlags.ProcessIdentityConstraints |
                    XmlSchemaValidationFlags.ProcessInlineSchema);
            } 
            catch (Exception ex)
            {
                ReportError(Severity.Error, ex.Message, doc);
                this._validator = null;
            }

            if (this._validator != null)
            {
                this._validator.ValidationEventHandler += OnValidationEvent;
                this._validator.XmlResolver = resolver;
                this._validator.Initialize();

                this._nsResolver.Context = doc;
                if (doc.DocumentElement == null)
                {
                    GetExpectedRootElements(sc);
                }
                else
                {
                    ValidateContent(doc);
                }
                this._nsResolver.Context = doc;

                this._validator.EndValidation();
            }
        }

        private void GetExpectedRootElements(SchemaCache cache)
        {
            List<XmlSchemaParticle> expected = new List<XmlSchemaParticle>();
            try
            {
                foreach (XmlSchemaElement root in cache.GetPossibleTopLevelElements())
                {
                    expected.Add(root);
                }
            } 
            catch (Exception)
            {
                // ignore compile errors
                // todo: add them as task list errors?
            }
            this._expectedParticles = expected.ToArray();
        }

        public void Validate(XmlCache xcache)
        {
            this.ValidateContext(xcache);
            xcache.TypeInfoMap = _typeInfo; // save schema type information for intellisense.
        }

        public XmlSchemaInfo GetTypeInfo(XmlNode node)
        {
            if (node == null) return null;
            XmlSchemaInfo si;
            _typeInfo.TryGetValue(node, out si);
            return si;
        }

        bool LoadSchemas(XmlDocument doc, XmlSchemaSet set, SchemaResolver resolver)
        {
            XmlElement root = doc.DocumentElement;
            if (root != null)
            {
                // Give Xsi schemas highest priority.
                bool result = LoadXsiSchemas(doc, set, resolver);
                SchemaCache sc = this._cache.SchemaCache;
                foreach (string nsuri in this._cache.AllNamespaces)
                {
                    result |= LoadSchemasForNamespace(set, resolver, sc, nsuri, root);
                }
            }
            // Make sure all the required includes or imports are there. 
            // This is making up for a possible bug in XmlSchemaSet where it
            // refuses to load an XmlSchema containing a DTD.  Our XmlSchemaResolver
            // doesn't have that problem.
            var visited = new HashSet<XmlSchema>();
            foreach (XmlSchema s in doc.Schemas.Schemas())
            {
                CopyImports(s, set, visited);
            }

            return true;
        }

        private void CopyImports(XmlSchema s, XmlSchemaSet set, HashSet<XmlSchema> visited)
        {
            visited.Add(s);
            set.Add(s);
            foreach (var o in s.Includes)
            {
                if (o is XmlSchemaInclude i && i.Schema != null && !visited.Contains(i.Schema))
                {
                    CopyImports(i.Schema, set, visited);
                }
                else if (o is XmlSchemaImport j)
                {
                    XmlSchema js = j.Schema;
                    if (js == null && !string.IsNullOrEmpty(j.Namespace))
                    {
                        js = this._cache.SchemaCache.FindSchemasByNamespace(j.Namespace)?.Schema;
                    }
                    if (js != null && !visited.Contains(js))
                    {
                        CopyImports(js, set, visited);
                    }
                }
            }
        }
             
        private bool LoadSchemasForNamespace(XmlSchemaSet set, SchemaResolver resolver, SchemaCache sc, string nsuri, XmlNode ctx)
        {
            bool result = false;
            if (set.Schemas(nsuri).Count == 0)
            {
                CacheEntry ce = sc.FindSchemasByNamespace(nsuri);
                while (ce != null)
                {
                    if (!ce.Disabled)
                    {
                        if (!ce.HasUpToDateSchema)
                        {
                            // delay loaded!
                            LoadSchema(set, resolver, ctx, nsuri, ce.Location.AbsoluteUri);
                        }
                        else
                        {
                            set.Add(ce.Schema);
                        }
                        result = true;
                    }
                    ce = ce.Next;
                }
            }
            return result;
        }

        bool LoadXsiSchemas(XmlDocument doc, XmlSchemaSet set, SchemaResolver resolver)
        {
            if (doc.DocumentElement == null) return false;
            bool result = false;
            foreach (XmlAttribute a in doc.DocumentElement.Attributes)
            {
                if (a.NamespaceURI == "http://www.w3.org/2001/XMLSchema-instance")
                {
                    if (a.LocalName == "noNamespaceSchemaLocation")
                    {
                        string path = a.Value;
                        if (!string.IsNullOrEmpty(path))
                        {
                            result = LoadSchema(set, resolver, a, "", a.Value);
                        }
                    }
                    else if (a.LocalName == "schemaLocation")
                    {
                        string[] words = a.Value.Split(new char[] { ' ', '\t', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                        for (int i = 0, n = words.Length; i + 1 < n; i++)
                        {
                            string nsuri = words[i];
                            string location = words[++i];
                            result |= LoadSchema(set, resolver, a, nsuri, location);
                        }
                    }
                }
            }
            return result;
        }

        bool LoadSchema(XmlSchemaSet set, SchemaResolver resolver, XmlNode ctx, string nsuri, string filename)
        {
            try
            {
                if (set.Contains(nsuri))
                {
                    return false;
                }
                Uri baseUri = this._baseUri;
                if (!string.IsNullOrEmpty(ctx.BaseURI))
                {
                    baseUri = new Uri(ctx.BaseURI);
                }
                Uri resolved;
                if (baseUri != null)
                {
                    resolved = new Uri(baseUri, filename);
                }
                else
                {
                    resolved = new Uri(filename, UriKind.RelativeOrAbsolute);
                }
                XmlSchema s = null;
                SchemaCache sc = this._cache.SchemaCache;
                var ce = sc.FindSchemaByUri(resolved.AbsoluteUri);
                if (ce != null && ce.Schema != null)
                {
                    s = ce.Schema;
                }
                else
                {
                    s = resolver.GetEntity(resolved, "", typeof(XmlSchema)) as XmlSchema;
                }
                if ((s.TargetNamespace + "") != (nsuri + ""))
                {
                    ReportError(Severity.Warning, Strings.TNSMismatch, ctx);
                }
                else if (!set.Contains(s))
                {
                    set.Add(s);
                    return true;
                }
            }
            catch (Exception e)
            {
                ReportError(Severity.Warning, string.Format(Strings.SchemaLoadError, filename, e.Message), ctx);
            }
            return false;
        }

        void ReportError(Severity sev, string msg, XmlNode ctx)
        {
            if (_eh == null) return;
            int line = 0, col = 0;
            string filename = _cache.FileName;
            LineInfo li = _cache.GetLineInfo(ctx);
            if (li != null)
            {
                line = li.LineNumber;
                col = li.LinePosition;
                filename = GetRelative(li.BaseUri);
            }
            _eh.HandleError(sev, msg, filename, line, col, ctx);
        }

        void ValidateContent(XmlNode container)
        {
            foreach (XmlNode n in container.ChildNodes)
            {
                // If we are validating up to a given node for intellisense info, then
                // we can prune out any nodes that are not connected to the same parent chain.
                if (_parents == null || _parents.Contains(n.ParentNode))
                {
                    ValidateNode(n);
                }
                if (n == this._node)
                {
                    break; // we're done!
                }
            }
        }

        void ValidateNode(XmlNode node)
        {
            XmlElement e = node as XmlElement;
            if (e != null)
            {
                ValidateElement(e);
                return;
            }
            XmlText t = node as XmlText;
            if (t != null)
            {
                ValidateText(t);
                return;
            }
            XmlCDataSection cd = node as XmlCDataSection;
            if (cd != null)
            {
                ValidateText(cd);
                return;
            }
            XmlWhitespace w = node as XmlWhitespace;
            if (w != null)
            {
                ValidateWhitespace(w);
                return;
            }
        }

        XmlSchemaInfo GetInfo()
        {
            XmlSchemaInfo i = this._info;
            XmlSchemaInfo copy = new XmlSchemaInfo();
            copy.ContentType = i.ContentType;
            copy.IsDefault = i.IsDefault;
            copy.IsNil = i.IsNil;
            copy.MemberType = i.MemberType;
            copy.SchemaAttribute = i.SchemaAttribute;
            copy.SchemaElement = i.SchemaElement;
            copy.SchemaType = i.SchemaType;
            copy.Validity = i.Validity;
            return copy;
        }

        void ValidateElement(XmlElement e)
        {
            this._nsResolver.Context = e;
            if (this._node == e && _position == IntellisensePosition.OnNode)
            {
                this._expectedParticles = _validator.GetExpectedParticles();
            }
            string xsiType = null;
            string xsiNil = null;
            foreach (XmlAttribute a in e.Attributes)
            {
                if (XmlHelpers.IsXsiAttribute(a))
                {
                    string name = a.LocalName;
                    if (name == "type")
                    {
                        xsiType = a.Value;
                    }
                    else if (name == "nil")
                    {
                        xsiNil = a.Value;
                    }
                }
            }
            _validator.ValidateElement(e.LocalName, e.NamespaceURI, this._info, xsiType, xsiNil, null, null);
            if (this._info.SchemaType != null)
            {
                _typeInfo[e] = GetInfo();
            }
            foreach (XmlAttribute a in e.Attributes)
            {
                if (!XmlHelpers.IsXmlnsNode(a))
                {
                    ValidateAttribute(a);
                }
            }
            if (this._node == e)
            {
                this._expectedAttributes = _validator.GetExpectedAttributes();
            }
            this._nsResolver.Context = e;
            _validator.ValidateEndOfAttributes(this._info);
            if (this._node == e && _position == IntellisensePosition.FirstChild)
            {
                this._expectedParticles = _validator.GetExpectedParticles();
            }
            if (this._node != e)
            {
                ValidateContent(e);
            }
            this._nsResolver.Context = e;
            _validator.ValidateEndElement(this._info);
            if (this._node == e && _position == IntellisensePosition.AfterNode)
            {
                this._expectedParticles = _validator.GetExpectedParticles();
            }

        }
        void ValidateText(XmlCharacterData text)
        {
            this._nsResolver.Context = text;
            CheckCharacters();
            _validator.ValidateText(new XmlValueGetter(GetText));
        }

        /// <summary>
        /// We turned off Character checking on the XmlReader so we could load more
        /// XML documents, so here we implement that part of the W3C spec:
        /// [2]    Char    ::=    #x9 | #xA | #xD | [#x20-#xD7FF] | [#xE000-#xFFFD] | 
        ///                       [#x10000-#x10FFFF] 
        /// </summary>
        /// <param name="text"></param>
        void CheckCharacters()
        {
            if (_eh == null) return;

            XmlNode node = this._nsResolver.Context;
            if (node == null) return;
            string text = node.InnerText;
            if (text == null) return;
            XmlNode ctx = node.ParentNode;
            if (ctx == null) ctx = node;

            for (int i = 0, n = text.Length; i < n; i++)
            {
                char ch = text[i];
                if ((ch < 0x20 && ch != 0x9 && ch != 0xa && ch != 0xd) || ch > 0xfffe)
                {
                    ReportError(Severity.Error, string.Format(Strings.InvalidCharacter, ((int)ch).ToString(), i), ctx);
                }
                else if (ch >= SurHighStart && ch <= SurHighEnd)
                {
                    if (i + 1 < n)
                    {
                        char nc = text[i + 1];
                        if (nc < SurLowStart || nc > SurLowEnd)
                        {
                            ReportError(Severity.Error, string.Format(Strings.IllegalSurrogatePair, Convert.ToInt32(ch).ToString("x", CultureInfo.CurrentUICulture), Convert.ToInt32(nc).ToString("x", CultureInfo.CurrentUICulture), i), ctx);
                        }
                        else
                        {
                            i++;
                        }
                    }
                }
                else if (ch >= 0xd800 && ch < 0xe000)
                {
                    ReportError(Severity.Error, string.Format(Strings.InvalidCharacter, ((int)ch).ToString(), i), ctx);
                }
            }
        }

        object GetText()
        {
            return this._nsResolver.Context.InnerText; ;
        }

        void ValidateWhitespace(XmlWhitespace w)
        {
            this._nsResolver.Context = w;
            _validator.ValidateWhitespace(w.InnerText);
        }

        void ValidateAttribute(XmlAttribute a)
        {
            this._nsResolver.Context = a;
            CheckCharacters();
            _validator.ValidateAttribute(a.LocalName, a.NamespaceURI, a.Value, this._info);
            _typeInfo[a] = GetInfo();
        }

        void OnValidationEvent(object sender, ValidationEventArgs e)
        {
            if (_eh != null)
            {
                string filename = _cache.FileName;
                int line = 0;
                int col = 0;
                XmlNode node = this._nsResolver.Context;
                Severity sev = e.Severity == XmlSeverityType.Error ? Severity.Error : Severity.Warning;
                XmlSchemaException se = e.Exception;
                if (se != null && !string.IsNullOrEmpty(se.SourceUri))
                {
                    filename = GetRelative(se.SourceUri);
                    line = se.LineNumber;
                    col = se.LinePosition;
                }
                else
                {
                    LineInfo li = _cache.GetLineInfo(node);
                    if (li != null)
                    {
                        line = li.LineNumber;
                        col = li.LinePosition;
                        filename = GetRelative(li.BaseUri);
                    }
                }
                _eh.HandleError(sev, e.Message, filename, line, col, node);
                Exception inner = e.Exception.InnerException;
                while (inner != null)
                {
                    _eh.HandleError(sev, inner.Message, filename, line, col, node);
                    inner = inner.InnerException;
                }
            }
        }

        string GetRelative(string s)
        {
            if (_baseUri == null) return s;
            if (string.IsNullOrEmpty(s)) return s;
            Uri uri = new Uri(s);
            Uri rel = this._baseUri.MakeRelativeUri(uri);
            return rel.GetComponents(UriComponents.SerializationInfoString, UriFormat.SafeUnescaped);
        }

        public void Dispose()
        {
            if (_validator != null)
            {
                this._validator.ValidationEventHandler -= OnValidationEvent;
            }
        }

    }

}
