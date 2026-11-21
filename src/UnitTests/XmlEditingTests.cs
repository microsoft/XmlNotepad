using System.Xml;
using System.Xml.Schema;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace XmlNotepad.UnitTests;

[TestClass]
public sealed class XmlEditingTests
{
    [TestMethod]
    public void EditingValuePreservesExistingLineEndings()
    {
        const string original = "one\ntwo\rthree\r\nfour";
        const string displayed = "one\rtwo\rthree\rfour";

        var edited = XmlValueLineEndings.ApplyEditorChange(original, displayed, "one\rTWO\rthree\rfour");

        Assert.AreEqual("one\nTWO\rthree\r\nfour", edited);
    }

    [TestMethod]
    public void NestedNamespaceBindingsOverrideAncestorBindings()
    {
        var document = new XmlDocument();
        document.LoadXml("<root xmlns='urn:root' xmlns:p='urn:outer'><branch xmlns='' xmlns:p='urn:inner'><leaf/></branch></root>");

        var scope = XmlHelpers.GetNamespaceScope(document.DocumentElement!["branch"]!["leaf"]!);

        Assert.AreEqual(string.Empty, scope.DefaultNamespace);
        Assert.AreEqual("urn:inner", scope.LookupNamespace("p"));
    }

    [TestMethod]
    public void GeneratesAnXmlDocumentFromASchema()
    {
        const string schema = """
            <xs:schema xmlns:xs="http://www.w3.org/2001/XMLSchema">
              <xs:element name="item" type="xs:string" fixed="sample"/>
            </xs:schema>
            """;
        using var reader = XmlReader.Create(new StringReader(schema));
        var schemas = new XmlSchemaSet();
        schemas.Add(null, reader);
        var generator = new XsdInstanceGenerator(schemas, new XmlNamespaceManager(new NameTable()));

        var document = generator.Generate(new XmlQualifiedName("item"));

        Assert.AreEqual("<item>sample</item>", document?.DocumentElement?.OuterXml);
    }
}
