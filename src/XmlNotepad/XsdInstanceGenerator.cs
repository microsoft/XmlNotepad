//------------------------------------------------------------------------------
// <copyright file="Snippets.cs"  company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
// <owner current="true" primary="true">sdub</owner>
//------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;
using System.Collections;
using System.Globalization;
using System.Reflection;
using System.IO;
using System.Diagnostics;

namespace XmlNotepad
{

    public class XsdInstanceGenerator
    {
        XmlResolver xmlResolver;
        XmlSchemaSet schemaSet;
        Hashtable complexTypesGuard;
        XmlDocument doc;
        XmlElement root;
        int compileErrors;
        NamespacePrefixGenerator map;

        // We must use CultureInfo.InvariantCulture throughout to generate valid XSD values.
        // XSD uses CultureInfo.InvariantCulture by definition because you want the XML
        // to be interoperable across platforms and users.

        public XsdInstanceGenerator(XmlSchema schema, IXmlNamespaceResolver mgr, XmlResolver xmlResolver)
        {
            if (schema == null)
            {
                throw new ArgumentNullException("schemaSet", "Schema set cannot be null.");
            }
            if (mgr == null)
            {
                throw new ArgumentNullException("mgr", "mgr cannot be null.");
            }
            this.xmlResolver = xmlResolver;
            this.schemaSet = new XmlSchemaSet();
            this.schemaSet.Add(schema);
            Init(mgr);
        }

        private void Init(IXmlNamespaceResolver mgr)
        {
            this.schemaSet.ValidationEventHandler += new ValidationEventHandler(ValidationCallBack);
            this.schemaSet.XmlResolver = this.xmlResolver;
            this.map = new NamespacePrefixGenerator(mgr);
        }

        public XsdInstanceGenerator(XmlSchemaSet schemaSet, IXmlNamespaceResolver mgr)
        {
            if (schemaSet == null)
                throw new ArgumentNullException("schemaSet", "Schema set cannot be null.");
            this.schemaSet = schemaSet;
            Init(mgr);
        }

        public XmlResolver XmlResolver
        {
            set
            {
                this.xmlResolver = value;
            }
        }

        public XmlDocument Generate(XmlQualifiedName name)
        {
            if (name == null) return null;
            this.complexTypesGuard = new Hashtable();
            this.schemaSet.Compile();
            if (this.schemaSet.IsCompiled)
            {
                XmlSchemaElement schemaElem = schemaSet.GlobalElements[name] as XmlSchemaElement;
                if (schemaElem != null)
                {
                    return GenerateInstance(schemaElem);
                }
            }
            return null;
        }

        public XmlDocument Generate(XmlSchemaElement e)
        {
            if (e == null) return null;
            this.complexTypesGuard = new Hashtable();
            this.schemaSet.Compile();
            return GenerateInstance(e);
        }

        private XmlDocument GenerateInstance(XmlSchemaElement e)
        {
            if (!e.RefName.IsEmpty)
            {
                XmlSchemaElement f = (XmlSchemaElement)schemaSet.GlobalElements[e.RefName];
                if (f == null)
                {
                    return null;
                }
                e = f;
            }
            if (e.IsAbstract)
            {
                return null;
            }

            this.doc = new XmlDocument();

            GenerateElement(e, null);

            return this.doc;
        }

        internal static XmlSchemaForm GetElementForm(XmlSchemaElement e)
        {
            XmlSchemaForm result = e.Form;
            if (e.Parent is XmlSchema || !e.RefName.IsEmpty) return XmlSchemaForm.Qualified;
            if (e.Form == XmlSchemaForm.None)
            {
                // Check the XmlSchema setting.
                XmlSchema s = GetSchema(e);
                if (s != null)
                {
                    result = s.ElementFormDefault;
                    if (result == XmlSchemaForm.None)
                    {
                        // default is unqualified!
                        result = XmlSchemaForm.Unqualified;
                    }
                }
            }
            return result;
        }

        public static XmlSchema GetSchema(XmlSchemaObject so)
        {
            for (XmlSchemaObject p = so; p != null; p = p.Parent)
            {
                XmlSchema s = p as XmlSchema;
                if (s != null)
                {
                    return s;
                }
            }
            return null;
        }


        public static bool IsAbstract(XmlElement e)
        {
            //XmlSchemaElement et = e.SchemaType as XmlSchemaElement;
            //Debug.Assert(e.Doc != null, "Doc is not supposed to be null here");
            //return IsAbstract(et, e.Doc.SchemaSet);
            return false;
        }

        public static bool IsAbstract(XmlSchemaElement et, XmlSchemaSet set)
        {
            if (et == null || set == null) return false;
            if (et.IsAbstract) return true;
            if (!et.RefName.IsEmpty)
            { //if this is ref then get IsAbstract from global
                et = set.GlobalElements[et.RefName] as XmlSchemaElement;
                if (et.IsAbstract) return true;
            }

            XmlSchemaComplexType ct = et.ElementSchemaType as XmlSchemaComplexType;
            if (ct != null)
            {
                return ct.IsAbstract;
            }
            return false;
        }

        void GenerateElement(XmlSchemaElement e, XmlElement parent)
        {
            string nsuri = e.QualifiedName.Namespace;
            bool needXmlnsDecl = false;

            string prefix = null;
            if (GetElementForm(e) == XmlSchemaForm.Unqualified)
            {
                prefix = "";
                nsuri = "";
                if (!map.IsDefaultNamespaceEmpty(parent))
                {
                    needXmlnsDecl = true; // need to revert to empty namespace.
                }
            }
            else
            {
                prefix = map.GenerateElementPrefix(parent, e.QualifiedName.Namespace, out needXmlnsDecl);
            }

            XmlElement element = doc.CreateElement(prefix, e.QualifiedName.Name, nsuri);

            if (needXmlnsDecl)
            {
                if (string.IsNullOrEmpty(prefix))
                {
                    element.SetAttribute("xmlns", nsuri);
                }
                else
                {
                    element.SetAttribute("xmlns:" + prefix, nsuri);
                }
            }
            if (parent == null)
            {
                this.doc.AppendChild(element);
                this.root = element;
                map.Root = element;
                parent = element;
            }
            else
            {
                parent.AppendChild(element);
                parent = element;
            }

            if (!string.IsNullOrEmpty(e.FixedValue))
            {
                string val = e.FixedValue;
                val = val.Replace("$", "$$"); //replace all occurences of '$' with escaped value
                parent.InnerText = val;
            }
            else if (!string.IsNullOrEmpty(e.DefaultValue))
            {
                parent.InnerText = e.DefaultValue;
            }
            else if (e.ElementSchemaType is XmlSchemaSimpleType)
            {
                XmlSchemaSimpleType st = (XmlSchemaSimpleType)e.ElementSchemaType;
                GenerateSimpleType(e, e.QualifiedName, st.Datatype, parent);
            }
            else if (e.ElementSchemaType is XmlSchemaComplexType)
            {
                XmlSchemaComplexType ct = (XmlSchemaComplexType)e.ElementSchemaType;
                element.IsEmpty = ct.ContentType == XmlSchemaContentType.Empty;

                if (!IsAbstract(e, this.schemaSet))
                {
                    GenerateComplexType(e, ct, parent);
                }
                else
                { // Ct is abstract, need to generate instance elements with xsi:type
                    XmlSchemaComplexType dt = GetDerivedType(ct);
                    if (dt != null)
                    {
                        AddXsiTypeAttribute(parent, dt.QualifiedName);
                        GenerateComplexType(e, dt, parent);
                    }
                }
            }
        }

        void AddXsiTypeAttribute(XmlElement e, XmlQualifiedName qname)
        {

            string prefix = map.GenerateAttributePrefix(e, qname.Namespace);

            string name = qname.Name;
            if (!string.IsNullOrEmpty(prefix))
            {
                name = prefix + ":" + name;
            }

            string xsiUri = XmlStandardUris.XsiUri;
            map.GenerateAttributePrefix(e, xsiUri);

            e.SetAttribute("type", xsiUri, name);
        }

        void GenerateSimpleType(XmlSchemaObject owner, XmlQualifiedName name, XmlSchemaDatatype dt, XmlNode p)
        {
            SimpleTypeGenerator stgen = new SimpleTypeGenerator(name, dt);
            p.InnerText = stgen.GenerateLiteral();
        }

        void GenerateComplexType(XmlSchemaElement owner, XmlSchemaComplexType ct, XmlElement e)
        {

            if (complexTypesGuard.Contains(ct))
                return;

            complexTypesGuard[ct] = ct;

            if (ct.AttributeUses.Count > 0)
            {
                GenerateAttributes(ct.AttributeUses, e);
            }

            if (ct.ContentModel != null && ct.ContentModel is XmlSchemaSimpleContent)
            {
                // This can only be XmlSchemaSimpleContentExtension or 
                // XmlSchemaSimpleContentRestriction which can only add attributes
                // which we've already processed above.
                this.GenerateSimpleType(owner, owner.QualifiedName, ct.Datatype, e);
            }
            else
            {
                XmlQualifiedName ctname = ct.QualifiedName;
                if (ctname.Namespace == XmlStandardUris.XsdUri &&
                    ctname.Name == "anyType")
                {
                    this.GenerateSimpleType(owner, owner.QualifiedName, null, e);
                }
                else if (ct.ContentTypeParticle != null)
                {
                    GenerateParticle(ct.ContentTypeParticle, e);
                }
            }
            if (ct.IsMixed)
            {
                e.AppendChild(e.OwnerDocument.CreateTextNode("mixed"));
            }

            complexTypesGuard.Remove(ct);
        }

        void GenerateAttributes(XmlSchemaObjectTable table, XmlElement e)
        {
            foreach (DictionaryEntry de in table)
            {
                XmlQualifiedName name = (XmlQualifiedName)de.Key;
                XmlSchemaAttribute a = (XmlSchemaAttribute)de.Value;
                if (a.Use == XmlSchemaUse.Required)
                {
                    string prefix = map.GenerateAttributePrefix(e, name.Namespace);
                    XmlAttribute attr = e.OwnerDocument.CreateAttribute(prefix, name.Name, name.Namespace);

                    e.Attributes.Append(attr);
                    if (!a.RefName.IsEmpty)
                    {
                        a = this.schemaSet.GlobalAttributes[a.RefName] as XmlSchemaAttribute;
                    }
                    if (!string.IsNullOrEmpty(a.FixedValue))
                    {
                        attr.Value = a.FixedValue;
                    }
                    else
                    {
                        attr.Value = a.DefaultValue;
                    }
                }
            }
        }

        void GenerateParticle(XmlSchemaParticle particle, XmlElement e)
        {
            bool hint = false;
            if (particle.MinOccurs <= 0 && !hint)
                return; // don't expand particle unless it is required 

            decimal min = particle.MinOccurs;
            if (min <= 0) min = 1;
            if (min > 100) min = 100;

            for (decimal i = 0; i < min; i++)
            {
                if (particle is XmlSchemaSequence)
                {
                    XmlSchemaSequence seq = (XmlSchemaSequence)particle;
                    GenerateGroupBase(seq, e);
                }
                else if (particle is XmlSchemaChoice)
                {
                    XmlSchemaChoice sc = (XmlSchemaChoice)particle;
                    // Find a choice with vs:snippet hint, or that has minOccurs > 0.
                    XmlSchemaParticle choice = null;
                    foreach (XmlSchemaParticle p in sc.Items)
                    {
                        if (choice == null && p.MinOccurs > 0)
                        {
                            choice = p;
                        }
                    }
                    if (choice != null)
                    {
                        GenerateParticle(choice, e);
                    }
                }
                else if (particle is XmlSchemaAll)
                {
                    XmlSchemaAll all = (XmlSchemaAll)particle;
                    GenerateGroupBase(all, e);
                }
                else if (particle is XmlSchemaElement)
                {
                    XmlSchemaElement se = particle as XmlSchemaElement;
                    GenerateElement(se, e);
                }
                //else if (particle is XmlSchemaAny && particle.MinOccurs > 0) {
                // Don't pick a random element, the user will likely have to delete it.
                //}
            }
        }

        void GenerateGroupBase(XmlSchemaGroupBase gBase, XmlElement e)
        {
            foreach (XmlSchemaParticle p in gBase.Items)
            {
                GenerateParticle(p, e);
            }
        }

        private XmlSchemaComplexType GetDerivedType(XmlSchemaType baseType)
        { //To get derived type of an abstract type for xsi:type value in the instance
            foreach (XmlSchemaType type in schemaSet.GlobalTypes.Values)
            {
                XmlSchemaComplexType ct = type as XmlSchemaComplexType;
                if (ct != null && !ct.IsAbstract && XmlSchemaType.IsDerivedFrom(ct, baseType, XmlSchemaDerivationMethod.None))
                {
                    return ct;
                }
            }
            return null;
        }

        private void ValidationCallBack(object sender, ValidationEventArgs args)
        {
            if (args.Severity == XmlSeverityType.Error)
                this.compileErrors++;
        }
    }

    class SimpleTypeGenerator
    {
        XmlSchemaDatatype datatype;
        CompiledFacets facets;
        XmlQualifiedName name;

        public SimpleTypeGenerator(XmlQualifiedName name, XmlSchemaDatatype datatype)
        {
            this.name = name;
            if (datatype != null)
            {
                this.datatype = GetAtomicDatatype(new Hashtable(), datatype);
                object restriction = datatype.GetType().InvokeMember("Restriction", BindingFlags.GetProperty | BindingFlags.NonPublic | BindingFlags.Instance, null, datatype, null, CultureInfo.InvariantCulture);
                this.facets = new CompiledFacets(restriction);
            }
        }

        XmlSchemaDatatype GetAtomicDatatype(Hashtable visited, XmlSchemaDatatype dt)
        {
            if (dt == null) return dt;
            if (visited.Contains(dt)) // infinite recurrsion guard.
                return dt;          // not sure this is possible with the SOM, but better safe than sorry.
            visited[dt] = dt;

            if (dt.Variety == XmlSchemaDatatypeVariety.Union)
            {
                XmlSchemaSimpleType[] memberTypes = (XmlSchemaSimpleType[])dt.GetType().InvokeMember("types", BindingFlags.GetField | BindingFlags.NonPublic | BindingFlags.Instance, null, dt, null, CultureInfo.InvariantCulture);
                if (memberTypes != null && memberTypes.Length > 0)
                {
                    XmlSchemaSimpleType mt = memberTypes[0];
                    return GetAtomicDatatype(visited, mt.Datatype);
                }
            }
            else if (dt.Variety == XmlSchemaDatatypeVariety.List)
            {
                XmlSchemaDatatype itemType = (XmlSchemaDatatype)dt.GetType().InvokeMember("itemType", BindingFlags.GetField | BindingFlags.NonPublic | BindingFlags.Instance, null, dt, null, CultureInfo.InvariantCulture);
                if (itemType != null) return GetAtomicDatatype(visited, itemType);
            }
            return dt;
        }

        // strings matching the XmlTypeCode enumeration.
        static string[] XsdNames = new string[] {
            "None",
            "Item",
            "Node",
            "Document",
            "Element",
            "Attribute",
            "Namespace",
            "ProcessingInstruction",
            "Comment",
            "Text",
            "anyType",
            "UntypedAtomic",
            "string",
            "boolean",
            "decimal",
            "float",
            "double",
            "duration",
            "dateTime",
            "time",
            "date",
            "gYearMonth",
            "gYear",
            "gMonthDay",
            "gDay",
            "gMonth",
            "hexBinary",
            "base64Binary",
            "anyURI",
            "QName",
            "NOTATION",
            "normalizedString",
            "token",
            "language",
            "NMTOKEN",
            "Name",
            "NCName",
            "ID",
            "IDREF",
            "ENTITY",
            "integer",
            "nonPositiveInteger",
            "negativeInteger",
            "long",
            "int",
            "short",
            "byte",
            "nonNegativeInteger",
            "unsignedLong",
            "unsignedInt",
            "unsignedShort",
            "unsignedByte",
            "positiveInteger",
            "yearMonthDuration",
            "dayTimeDuration",

        };

        public string GenerateLiteral()
        {

            string value = null;

            if (this.facets != null && this.facets.HasEnumeration && this.facets.Enumeration.Count > 0)
            {
                object v = this.facets.Enumeration[0];
                value = v.ToString();
            }
            else
            {
                XmlTypeCode tc = datatype == null ? XmlTypeCode.AnyAtomicType : datatype.TypeCode;
                int i = (int)tc;
                if (i >= 0 && i < XsdNames.Length)
                {
                    value = XsdNames[i];
                }
                else
                {
                    value = tc.ToString();
                }
            }
            if (value != null)
            {
                string name = this.name.Name;
                if (string.IsNullOrEmpty(name)) name = "value";
                return value;
            }

            // I think providing the type name is more useful than trying to provide
            // a valid value in this case.  The user will most likely have to edit the 
            // value anyway and they will get intellisense to help them get a valid value.

            // The literal strings used below do not need to be localized because they are 
            // taken from the standard XSD specification.

            // We ignore the facets for strings because the user will have to edit the value anyway
            // and they get the validation errors as they type.           

            switch (this.datatype.TypeCode) {
                case XmlTypeCode.None:
                    // what to do with unions?
                    break;
                case XmlTypeCode.Item:
                    // what to do with lists?
                    break;
                case XmlTypeCode.AnyAtomicType:
                    value = "any";
                    break;
                case XmlTypeCode.Base64Binary:
                    value = "base64Binary";
                    break;
                case XmlTypeCode.Entity:
                    value = "ENTITY";
                    break;
                case XmlTypeCode.Id:
                    value = "ID";
                    break;
                case XmlTypeCode.Idref:
                    value = "IDREF";
                    break;
                case XmlTypeCode.NmToken:
                    value = "NMTOKEN";
                    break;
                case XmlTypeCode.Name:
                    value = "NAME";
                    break;
                case XmlTypeCode.NCName:
                    value = "NCNAME";
                    break;
                case XmlTypeCode.QName:
                    value = "QNAME";
                    break;
                case XmlTypeCode.NormalizedString:
                    value = "normalizedString";
                    break;
                case XmlTypeCode.Token:
                    value = "string";
                    break;
                case XmlTypeCode.HexBinary:
                    value = "hexBinary";
                    break;
                case XmlTypeCode.Notation:
                    value = "notation";
                    break;
                case XmlTypeCode.String:
                    value = "string";
                    break;
                case XmlTypeCode.Boolean:
                    value = "true";
                    break;
                case XmlTypeCode.Float:
                    value = GenerateNumeric<float>(float.MinValue, float.MaxValue, null);
                    break;
                case XmlTypeCode.Double:
                    value = GenerateNumeric<double>(double.MinValue, double.MaxValue, null);
                    break;
                case XmlTypeCode.AnyUri:
                    value = "anyURI";
                    break;
                case XmlTypeCode.Integer:
                    value = GenerateNumeric<long>(long.MinValue, long.MaxValue, null);
                    break;
                case XmlTypeCode.Int:
                    value = GenerateNumeric<int>(int.MinValue, int.MaxValue, null);
                    break;
                case XmlTypeCode.Decimal:
                    value = GenerateNumeric<decimal>(decimal.MinValue, decimal.MaxValue, null);
                    break;
                case XmlTypeCode.NonPositiveInteger:
                    value = GenerateNumeric<long>(long.MinValue, 0, null);
                    break;
                case XmlTypeCode.NegativeInteger:
                    value = GenerateNumeric<long>(long.MinValue, -1, null);
                    break;
                case XmlTypeCode.Long:
                    value = GenerateNumeric<long>(long.MinValue, long.MaxValue, null);
                    break;
                 case XmlTypeCode.Short:
                     value = GenerateNumeric<short>(short.MinValue, short.MaxValue, null);
                     break;
                case XmlTypeCode.Byte:
                    value = GenerateNumeric<sbyte>(sbyte.MinValue, sbyte.MaxValue, null);
                    break;
                case XmlTypeCode.NonNegativeInteger:
                    value = GenerateNumeric<long>(0, long.MaxValue, null);
                    break;
                case XmlTypeCode.UnsignedLong:
                    value = GenerateNumeric<ulong>(ulong.MinValue, ulong.MaxValue, null);
                    break;
                case XmlTypeCode.UnsignedInt:
                    value = GenerateNumeric<uint>(uint.MinValue, uint.MaxValue, null);
                    break;
                case XmlTypeCode.UnsignedShort:
                    value = GenerateNumeric<ushort>(ushort.MinValue, ushort.MaxValue, null);
                    break;
                case XmlTypeCode.UnsignedByte:
                    value = GenerateNumeric<byte>(byte.MinValue, byte.MaxValue, null);
                    break;
                case XmlTypeCode.PositiveInteger:
                    value = GenerateNumeric<long>(1, long.MaxValue, null);
                    break;
                case XmlTypeCode.Duration:
                    value = "P0Y0M0DT0H0M0S";
                    break;
                case XmlTypeCode.DateTime:
                    value = GenerateNumeric<DateTime>(DateTime.MinValue, DateTime.MaxValue, "yyyy-MM-ddTHH:mm:ss.fffffffzzzzzz");
                    break;
                case XmlTypeCode.Date:
                    value = GenerateNumeric<DateTime>(DateTime.MinValue, DateTime.MaxValue, "yyyy-MM-dd");
                    break;
                case XmlTypeCode.GYearMonth:
                    value = GenerateNumeric<DateTime>(DateTime.MinValue, DateTime.MaxValue, "yyyy-MM");
                    break;
                case XmlTypeCode.GYear:
                    value = GenerateNumeric<DateTime>(DateTime.MinValue, DateTime.MaxValue, "yyyy");
                    break;
                case XmlTypeCode.GMonthDay:
                    value = GenerateNumeric<DateTime>(DateTime.MinValue, DateTime.MaxValue, "MM-dd");
                    break;
                case XmlTypeCode.GDay:
                    value = GenerateNumeric<DateTime>(DateTime.MinValue, DateTime.MaxValue, "dd");
                    break;
                case XmlTypeCode.GMonth:
                    value = GenerateNumeric<DateTime>(DateTime.MinValue, DateTime.MaxValue, "MM");
                    break;
                case XmlTypeCode.Time:
                    value = GenerateNumeric<DateTime>(DateTime.MinValue, DateTime.MaxValue, "HH:mm:ss.fffffffzzzzzz");
                    break;
                case XmlTypeCode.Language:
                    value = CultureInfo.CurrentUICulture.Name;
                    break;
                default:
                    value = "any";
                    break;
            }
            return value;
        }

        string GetFacetTooltip()
        {
            string min = null;
            string join = null;
            string max = null;
            string mjoin = null;

            if (this.IsTextual)
            {
                if (this.facets.HasLength)
                {
                    return String.Format(StringResources.TooltipFacetLength, "=", this.facets.Length);
                }
                else
                {
                    if (this.facets.HasMinLength)
                    {
                        min = this.facets.MinLength.ToString(CultureInfo.InvariantCulture);
                    }
                    if (this.facets.HasMaxLength)
                    {
                        max = this.facets.MaxLength.ToString(CultureInfo.InvariantCulture);
                    }
                    string result = null;
                    if (min != null && max != null)
                    {
                        result = String.Format(StringResources.TooltipFacetLength, ">=", min) + " " +
                            String.Format(StringResources.TooltipFacetJoiner, "<=", max);
                    }
                    else if (min != null)
                    {
                        result = String.Format(StringResources.TooltipFacetLength, ">=", min);
                    }
                    else if (max != null)
                    {
                        result = String.Format(StringResources.TooltipFacetLength, "<=", max);
                    }
                    return result;
                }
            }

            if (this.IsNumeric)
            {

                if (this.facets.HasMinInclusive)
                {
                    min = this.facets.MinInclusive.ToString();
                    join = ">=";
                }
                else if (this.facets.HasMinExclusive)
                {
                    min = this.facets.MinExclusive.ToString();
                    join = ">";
                }
                if (this.facets.HasMaxInclusive)
                {
                    max = this.facets.MaxInclusive.ToString();
                    mjoin = "<=";
                }
                else if (this.facets.HasMaxExclusive)
                {
                    max = this.facets.MaxExclusive.ToString();
                    mjoin = "<";
                }
                string result = null;
                if (min != null && max != null)
                {
                    result = String.Format(StringResources.TooltipFacetValue, join, min) + " " +
                            String.Format(StringResources.TooltipFacetJoiner, mjoin, max);
                }
                else if (min != null)
                {
                    result = String.Format(StringResources.TooltipFacetValue, join, min);
                }
                else if (max != null)
                {
                    result = String.Format(StringResources.TooltipFacetValue, mjoin, max);
                }
                if (this.facets.HasTotalDigits)
                {
                    if (result != null) result += " ";
                    result = String.Format(StringResources.TooltipFacetTotalDigits, this.facets.TotalDigits.ToString(CultureInfo.InvariantCulture));
                }
                // todo: something clever with FractionDigits

                // ignore Whitespace facet since this is about how the value is processed
                // and not about what valid values the user can type in.
                return result;
            }

            return null;
        }

        bool IsNumeric
        {
            get
            {
                if (this.datatype == null) return false;
                switch (this.datatype.TypeCode)
                {
                    case XmlTypeCode.Boolean:
                    case XmlTypeCode.Float:
                    case XmlTypeCode.Double:
                    case XmlTypeCode.Integer:
                    case XmlTypeCode.Int:
                    case XmlTypeCode.Decimal:
                    case XmlTypeCode.NonPositiveInteger:
                    case XmlTypeCode.NegativeInteger:
                    case XmlTypeCode.Long:
                    case XmlTypeCode.Short:
                    case XmlTypeCode.Byte:
                    case XmlTypeCode.NonNegativeInteger:
                    case XmlTypeCode.UnsignedLong:
                    case XmlTypeCode.UnsignedInt:
                    case XmlTypeCode.UnsignedShort:
                    case XmlTypeCode.UnsignedByte:
                    case XmlTypeCode.PositiveInteger:
                    case XmlTypeCode.Duration:
                    case XmlTypeCode.DateTime:
                    case XmlTypeCode.Date:
                    case XmlTypeCode.GYearMonth:
                    case XmlTypeCode.GYear:
                    case XmlTypeCode.GMonthDay:
                    case XmlTypeCode.GDay:
                    case XmlTypeCode.GMonth:
                    case XmlTypeCode.Time:
                        return true;
                }
                return false;
            }
        }

        bool IsTextual
        {
            get
            {
                if (this.datatype == null) return false;
                switch (this.datatype.TypeCode)
                {
                    case XmlTypeCode.Base64Binary:
                    case XmlTypeCode.Entity:
                    case XmlTypeCode.Id:
                    case XmlTypeCode.Idref:
                    case XmlTypeCode.NmToken:
                    case XmlTypeCode.Name:
                    case XmlTypeCode.NCName:
                    case XmlTypeCode.QName:
                    case XmlTypeCode.NormalizedString:
                    case XmlTypeCode.Token:
                    case XmlTypeCode.HexBinary:
                    case XmlTypeCode.Notation:
                    case XmlTypeCode.String:
                    case XmlTypeCode.AnyUri:
                    case XmlTypeCode.Language:
                        return true;
                }
                return false;
            }
        }

        object PinValue(object facetValue, object v, int delta) {
            // This method is necessary because C# generics cannot do "+" operations on generic arguments.
            try {
                object result = facetValue;
                if (v is ulong) {
                    ulong x = Convert.ToUInt64(facetValue);
                    int y = x.CompareTo(v);
                    if (Math.Sign(y) == Math.Sign(delta)) x += (ulong)delta;                    
                    result = x;
                } else if (v is long) {
                    long x = Convert.ToInt64(facetValue);
                    int y = x.CompareTo(v);
                    if (Math.Sign(y) == Math.Sign(delta)) x += (long)delta;
                    result = x;
                } else if (v is uint) {
                    uint x = Convert.ToUInt32(facetValue);
                    int y = x.CompareTo(v);
                    if (Math.Sign(y) == Math.Sign(delta)) x += (uint)delta;
                    result = x;
                } else if (v is int) {
                    int x = Convert.ToInt32(facetValue);
                    int y = x.CompareTo(v);
                    if (Math.Sign(y) == Math.Sign(delta)) x += (int)delta;
                    result = x;
                } else if (v is ushort) {
                    ushort x = Convert.ToUInt16(facetValue);
                    int y = x.CompareTo(v);
                    if (Math.Sign(y) == Math.Sign(delta)) x += (ushort)delta;
                    result = x;
                } else if (v is short) {
                    short x = Convert.ToInt16(facetValue);
                    int y = x.CompareTo(v);
                    if (Math.Sign(y) == Math.Sign(delta)) x += (short)delta;
                    result = x;
                } else if (v is byte) {
                    byte x = Convert.ToByte(facetValue);
                    int y = x.CompareTo(v);
                    if (Math.Sign(y) == Math.Sign(delta)) x += (byte)delta;
                    result = x;
                } else if (v is sbyte) {
                    sbyte x = Convert.ToSByte(facetValue);
                    int y = x.CompareTo(v);
                    if (Math.Sign(y) == Math.Sign(delta)) x += (sbyte)delta;
                    result = x;
                } else if (v is float) {
                    float x = Convert.ToSingle(facetValue);
                    if (delta > 0) x = (float)Math.Ceiling(x);
                    else if (delta < 0) x = (float)Math.Floor(x);
                    int y = x.CompareTo(v);
                    if (Math.Sign(y) == Math.Sign(delta)) x += (float)delta;
                    result = x;
                } else if (v is double) {
                    double x = Convert.ToDouble(facetValue);
                    if (delta > 0) x = Math.Ceiling(x);
                    else if (delta < 0) x = Math.Floor(x);
                    int y = x.CompareTo(v);
                    if (Math.Sign(y) == Math.Sign(delta)) x += (double)delta;
                    result = x;
                } else if (v is decimal) {
                    decimal x = Convert.ToDecimal(facetValue); 
                    if (delta > 0) x = (decimal)Math.Ceiling(x);
                    else if (delta < 0) x = (decimal)Math.Floor(x);
                    int y = x.CompareTo(v);
                    if (Math.Sign(y) == Math.Sign(delta)) x += (decimal)delta;
                    result = x;
                } else if (v is DateTime){
                    DateTime x = Convert.ToDateTime(facetValue);
                    int y = x.CompareTo(v);
                    if (Math.Sign(y) == Math.Sign(delta)) x = x.AddDays(delta);
                    result = x;                    
                } else {
                    Debug.Assert(false, "Unexpected generic type: " + v.GetType().FullName);
                }
                return result;
            } catch (ArgumentException) {
                return facetValue;
            }
        }

        object GetSameTypeValue(object v, object defaultValue) {
            // This method is necessary because IComparable on primitive types requires
            // the argument to be the exact same type.
            try {
                object result = v;
                if (v is ulong) {
                    result = Convert.ToUInt64(defaultValue);
                } else if (v is long) {
                    result = Convert.ToInt64(defaultValue);
                } else if (v is uint) {
                    result = Convert.ToUInt32(defaultValue);
                } else if (v is int) {
                    result = Convert.ToInt32(defaultValue);
                } else if (v is ushort) {
                    result = Convert.ToUInt16(defaultValue);
                } else if (v is short) {
                    result = Convert.ToInt16(defaultValue);
                } else if (v is byte) {
                    result = Convert.ToByte(defaultValue);
                } else if (v is sbyte) {
                    result = Convert.ToSByte(defaultValue);
                } else if (v is float) {
                    result = Convert.ToSingle(defaultValue);
                } else if (v is double) {
                    result = Convert.ToDouble(defaultValue);
                } else if (v is decimal) {
                    result = Convert.ToDecimal(defaultValue);
                } else {
                    Debug.Assert(false, "Unexpected type: " + v.GetType().FullName);
                }
                return result;
            } catch (ArgumentException) {
                return v;
            }
        }

        string GenerateNumeric<T>(T min, T max, string format) {

            if (this.facets.HasMinInclusive) {
                min = (T)PinValue(this.facets.MinInclusive, min, 0);
            } else if (this.facets.HasMinExclusive) {
                min = (T)PinValue(this.facets.MinExclusive, min, 1);
            }
            if (this.facets.HasMaxInclusive) {
                max = (T)PinValue(this.facets.MaxInclusive, max, 0);
            } else if (this.facets.HasMaxExclusive) {
                max = (T)PinValue(this.facets.MaxExclusive, max, -1);
            }
            // todo: something clever with TotalDigits and FractionDigits
            if (min is DateTime) {
                DateTime dt = DateTime.Now;
                DateTime dtmin = (DateTime)(object)min;
                DateTime dtmax = (DateTime)(object)max;
                if (dt < dtmin) dt = dtmin;
                if (dt > dtmax) dt = dtmax;
                return dt.ToString(format);
            } else {
                object value = GetSameTypeValue(min, 0);
                IComparable minc = (IComparable)min;
                if (minc.CompareTo(value) > 0) value = min;
                IComparable maxc = (IComparable)max;
                if (maxc.CompareTo(value) < 0) value = max;
                return value.ToString();
            }
        }
    }

    // This class maintains a list of prefixes that map to a given namespace.
    class NamespacePrefixGenerator
    {
        IXmlNamespaceResolver mgr; // outside scope in which this snippet will be inserted.
        int nextPrefix;
        XmlElement root;
        string defaultNamespace;
        Dictionary<string, string> generatedPrefixes = new Dictionary<string, string>();

        public NamespacePrefixGenerator(IXmlNamespaceResolver mgr)
        {
            this.mgr = mgr;
        }

        public XmlElement Root
        {
            get { return this.root; }
            set { this.root = value; }
        }

        public bool IsDefaultNamespaceEmpty(XmlElement parent)
        {
            string ns = null;
            if (parent != null)
            {
                // see if xmlns='' scope has been entered.
                ns = parent.GetNamespaceOfPrefix(string.Empty);
                if (string.IsNullOrEmpty(ns)) return true;
            }
            // see if outer scope has defined anything for the empty prefix.
            ns = mgr.LookupNamespace(string.Empty);
            return string.IsNullOrEmpty(ns);
        }


        /// <param name="parent">Parent element scope</param>
        /// <param name="ns">namspace for which we need to find a prefix</param>
        /// <param name="redefinedDefaultNS">if true then calling function should spit out a local xmlns:prefix=ns</param>
        /// <returns></returns>
        public string GenerateElementPrefix(XmlElement parent, string ns, out bool needsXmlNs)
        {
            needsXmlNs = false;

            // See if prefix is already defined by the parent element.
            string prefix = null;
            if (parent != null)
            {
                if (parent.NamespaceURI == ns)
                    return parent.Prefix; // in this case should favor the parent prefix.
                prefix = parent.GetPrefixOfNamespace(ns);
                if (prefix != null) return prefix;
            }

            // See if prefix is already defined by outer scope.
            prefix = this.mgr.LookupPrefix(ns);
            if (string.IsNullOrEmpty(ns) && string.IsNullOrEmpty(prefix))
                return string.Empty;

            if (prefix != null)
            {
                // but make sure parent hasn't redefined it.
                if (parent != null)
                {
                    string previous = parent.GetNamespaceOfPrefix(prefix);
                    if (previous != null && previous != ns)
                    {
                        // then this prefix has been overridden for some reason, so we can't use it.
                        prefix = null;
                    }
                }
                if (prefix != null)
                    return prefix;
            }

            if (string.IsNullOrEmpty(ns))
            {
                // then we cannot have a prefix.
                needsXmlNs = !IsDefaultNamespaceEmpty(parent);
                return string.Empty;
            }

            // Then we need to invent a new prefix.
            if (!this.generatedPrefixes.TryGetValue(ns, out prefix))
            {
                prefix = GenerateUniquePrefix(ns, true);
            }
            return prefix;
        }

        private string GenerateUniquePrefix(string ns, bool canBeEmpty)
        {
            string prefix = null;
            if (ns == XmlStandardUris.XsiUri)
            {
                prefix = "xsi";
            }
            else if (prefix == null)
            {
                // See if we can take over the default namespace.
                if (canBeEmpty && (defaultNamespace == null || defaultNamespace == ns))
                {
                    defaultNamespace = ns;
                    return string.Empty;
                }
                else
                {
                    prefix = GetNextPrefix();
                }
            }
            while (this.mgr.LookupNamespace(prefix) != null)
            {
                prefix = GetNextPrefix(); // skip existing prefixes
            }

            this.generatedPrefixes[ns] = prefix;

            // record this namespace on the root element of the snippet to maximize reuse of it.
            if (this.root != null)
            {
                this.root.SetAttribute("xmlns:" + prefix, ns);
            }
            return prefix;
        }

        public string GenerateAttributePrefix(XmlElement parent, string ns)
        {
            // Empty namespace for attributes means an empty prefix.
            if (string.IsNullOrEmpty(ns)) return string.Empty;

            // But non-empty namespace requires a non-empty prefix.
            string prefix = parent.GetPrefixOfNamespace(ns);
            if (!string.IsNullOrEmpty(prefix)) return prefix;

            if (!this.generatedPrefixes.TryGetValue(ns, out prefix))
            {
                prefix = GenerateUniquePrefix(ns, false);
                parent.SetAttribute("xmlns:" + prefix, ns);
            }
            return prefix;
        }

        string GetNextPrefix()
        {
            // generates 'a'-'z','aa'-'zz','aaa'-'zzz', etc
            int i = nextPrefix;
            StringBuilder sb = new StringBuilder();
            while (i >= 26)
            {
                int letter = (nextPrefix % 26);
                sb.Append((char)('a' + letter));
                i = (nextPrefix / 26) - 1;
            }
            sb.Append((char)('a' + i));
            nextPrefix++;
            return sb.ToString();
        }

    }

    #region CompiledFacets

    internal enum XmlSchemaWhiteSpace
    {
        Preserve,
        Replace,
        Collapse,
    }

    internal enum RestrictionFlags
    {
        Length = 0x0001,
        MinLength = 0x0002,
        MaxLength = 0x0004,
        Pattern = 0x0008,
        Enumeration = 0x0010,
        WhiteSpace = 0x0020,
        MaxInclusive = 0x0040,
        MaxExclusive = 0x0080,
        MinInclusive = 0x0100,
        MinExclusive = 0x0200,
        TotalDigits = 0x0400,
        FractionDigits = 0x0800,
    }

    internal class CompiledFacets
    {

        static FieldInfo lengthInfo;
        static FieldInfo minLengthInfo;
        static FieldInfo maxLengthInfo;
        static FieldInfo patternsInfo;
        static FieldInfo enumerationInfo;
        static FieldInfo whitespaceInfo;

        static FieldInfo maxInclusiveInfo;
        static FieldInfo maxExclusiveInfo;
        static FieldInfo minInclusiveInfo;
        static FieldInfo minExclusiveInfo;
        static FieldInfo totalDigitsInfo;
        static FieldInfo fractionDigitsInfo;

        static FieldInfo restrictionFlagsInfo;
        static FieldInfo restrictionFixedFlagsInfo;

        public static Type XsdSimpleValueType;
        public static Type XmlSchemaDatatypeType;

        object compiledRestriction;

        static CompiledFacets()
        {
            Assembly systemXmlAsm = typeof(XmlSchema).Assembly;

            Type RestrictionFacetsType = systemXmlAsm.GetType("System.Xml.Schema.RestrictionFacets", true);
            XsdSimpleValueType = systemXmlAsm.GetType("System.Xml.Schema.XsdSimpleValue", true);
            XmlSchemaDatatypeType = typeof(XmlSchemaDatatype);

            lengthInfo = RestrictionFacetsType.GetField("Length", BindingFlags.Instance | BindingFlags.NonPublic);
            minLengthInfo = RestrictionFacetsType.GetField("MinLength", BindingFlags.Instance | BindingFlags.NonPublic);
            maxLengthInfo = RestrictionFacetsType.GetField("MaxLength", BindingFlags.Instance | BindingFlags.NonPublic);
            patternsInfo = RestrictionFacetsType.GetField("Patterns", BindingFlags.Instance | BindingFlags.NonPublic);
            enumerationInfo = RestrictionFacetsType.GetField("Enumeration", BindingFlags.Instance | BindingFlags.NonPublic);
            whitespaceInfo = RestrictionFacetsType.GetField("WhiteSpace", BindingFlags.Instance | BindingFlags.NonPublic);

            maxInclusiveInfo = RestrictionFacetsType.GetField("MaxInclusive", BindingFlags.Instance | BindingFlags.NonPublic);
            maxExclusiveInfo = RestrictionFacetsType.GetField("MaxExclusive", BindingFlags.Instance | BindingFlags.NonPublic);
            minInclusiveInfo = RestrictionFacetsType.GetField("MinInclusive", BindingFlags.Instance | BindingFlags.NonPublic);
            minExclusiveInfo = RestrictionFacetsType.GetField("MinExclusive", BindingFlags.Instance | BindingFlags.NonPublic);

            totalDigitsInfo = RestrictionFacetsType.GetField("TotalDigits", BindingFlags.Instance | BindingFlags.NonPublic);
            fractionDigitsInfo = RestrictionFacetsType.GetField("FractionDigits", BindingFlags.Instance | BindingFlags.NonPublic);

            restrictionFlagsInfo = RestrictionFacetsType.GetField("Flags", BindingFlags.Instance | BindingFlags.NonPublic);
            restrictionFixedFlagsInfo = RestrictionFacetsType.GetField("FixedFlags", BindingFlags.Instance | BindingFlags.NonPublic);
        }

        public CompiledFacets(object restriction)
        {
            this.compiledRestriction = restriction;
        }

        public int Length
        {
            get
            {
                if (compiledRestriction == null)
                {
                    return 0;
                }
                return (int)lengthInfo.GetValue(compiledRestriction);
            }
        }

        public bool HasLength
        {
            get { return ((this.Flags & RestrictionFlags.Length) != 0); }
        }

        public int MinLength
        {
            get
            {
                if (compiledRestriction == null)
                {
                    return 0;
                }
                return (int)minLengthInfo.GetValue(compiledRestriction);
            }
        }

        public bool HasMinLength
        {
            get { return ((this.Flags & RestrictionFlags.MinLength) != 0); }
        }

        public int MaxLength
        {
            get
            {
                if (compiledRestriction == null)
                {
                    return 0;
                }
                return (int)maxLengthInfo.GetValue(compiledRestriction);
            }
        }

        public bool HasMaxLength
        {
            get { return ((this.Flags & RestrictionFlags.MaxLength) != 0); }
        }

        public ArrayList Patterns
        {
            get
            {
                if (compiledRestriction == null)
                {
                    return null;
                }
                return (ArrayList)patternsInfo.GetValue(compiledRestriction);
            }
        }

        public bool HasPatterns
        {
            get { return ((this.Flags & RestrictionFlags.Pattern) != 0); }
        }

        public ArrayList Enumeration
        {
            get
            {
                if (compiledRestriction == null)
                {
                    return null;
                }
                return (ArrayList)enumerationInfo.GetValue(compiledRestriction);
            }
        }

        public bool HasEnumeration
        {
            get { return ((this.Flags & RestrictionFlags.Enumeration) != 0); }
        }

        public XmlSchemaWhiteSpace WhiteSpace
        {
            get
            {
                if (compiledRestriction == null)
                {
                    return XmlSchemaWhiteSpace.Preserve;
                }
                return (XmlSchemaWhiteSpace)whitespaceInfo.GetValue(compiledRestriction);
            }
        }

        public bool HasWhiteSpace
        {
            get { return ((this.Flags & RestrictionFlags.WhiteSpace) != 0); }
        }

        public object MaxInclusive
        {
            get
            {
                if (compiledRestriction == null)
                {
                    return null;
                }
                return maxInclusiveInfo.GetValue(compiledRestriction);
            }
        }

        public bool HasMaxInclusive
        {
            get { return ((this.Flags & RestrictionFlags.MaxInclusive) != 0); }
        }

        public object MaxExclusive
        {
            get
            {
                if (compiledRestriction == null)
                {
                    return null;
                }
                return maxExclusiveInfo.GetValue(compiledRestriction);
            }
        }

        public bool HasMaxExclusive
        {
            get { return ((this.Flags & RestrictionFlags.MaxExclusive) != 0); }
        }

        public object MinInclusive
        {
            get
            {
                if (compiledRestriction == null)
                {
                    return null;
                }
                return minInclusiveInfo.GetValue(compiledRestriction);
            }
        }

        public bool HasMinInclusive
        {
            get { return ((this.Flags & RestrictionFlags.MinInclusive) != 0); }
        }

        public object MinExclusive
        {
            get
            {
                if (compiledRestriction == null)
                {
                    return null;
                }
                return minExclusiveInfo.GetValue(compiledRestriction);
            }
        }

        public bool HasMinExclusive
        {
            get { return ((this.Flags & RestrictionFlags.MinExclusive) != 0); }
        }

        public int TotalDigits
        {
            get
            {
                if (compiledRestriction == null)
                {
                    return 0;
                }
                return (int)totalDigitsInfo.GetValue(compiledRestriction);
            }
        }

        public bool HasTotalDigits
        {
            get { return ((this.Flags & RestrictionFlags.TotalDigits) != 0); }
        }

        public int FractionDigits
        {
            get
            {
                if (compiledRestriction == null)
                {
                    return 0;
                }
                return (int)fractionDigitsInfo.GetValue(compiledRestriction);
            }
        }

        public bool HasFractionDigits
        {
            get { return ((this.Flags & RestrictionFlags.FractionDigits) != 0); }
        }

        RestrictionFlags Flags
        {
            get
            {
                if (compiledRestriction == null)
                {
                    return 0;
                }
                return (RestrictionFlags)restrictionFlagsInfo.GetValue(compiledRestriction);
            }
        }

        //RestrictionFlags FixedFlags {
        //    get {
        //        if (compiledRestriction == null) {
        //            return 0;
        //        }
        //        return (RestrictionFlags)restrictionFixedFlagsInfo.GetValue(compiledRestriction);
        //    }
        //}
    }

    #endregion
}
