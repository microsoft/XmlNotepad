using System.Text;
using System.Xml;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace XmlNotepad.UnitTests;

[TestClass]
public sealed class UpdateManifestTests
{
    private static readonly Uri FeedUri = new("https://example.invalid/releases/Updates.xml");

    [TestMethod]
    public void ReadsTheLatestValidVersionAndResolvesTheInstallerLink()
    {
        const string xml = """
            <updates>
              <application><installer>install</installer></application>
              <version number="2.9.0.25"/>
              <version number="invalid"/>
              <version number="3.0.0.0"/>
            </updates>
            """;
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(xml));

        var manifest = UpdateManifest.Read(stream, FeedUri);

        Assert.AreEqual(new Version(3, 0, 0, 0), manifest.LatestVersion);
        Assert.AreEqual(new Uri("https://example.invalid/releases/install"), manifest.Installer);
    }

    [TestMethod]
    public void RejectsDocumentTypes()
    {
        using var stream = new MemoryStream("<!DOCTYPE updates><updates><version number='3.0.0.0'/></updates>"u8.ToArray());

        Assert.ThrowsExactly<XmlException>(() => UpdateManifest.Read(stream, FeedUri));
    }
}
