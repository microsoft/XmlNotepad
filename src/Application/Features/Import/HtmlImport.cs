using System.Xml;
using Sgml;

namespace XmlNotepad.Services;

internal static class HtmlImport
{
    public static XmlDocument Load(string path)
    {
        using var input = new StreamReader(path);
        return Load(input);
    }

    internal static XmlDocument Load(TextReader input)
    {
        using var reader = new SgmlReader
        {
            InputStream = input,
            DocType = "HTML",
            CaseFolding = CaseFolding.ToLower,
            WhitespaceHandling = WhitespaceHandling.Significant,
            Resolver = new EmbeddedHtmlDtdResolver()
        };

        var document = new XmlDocument { PreserveWhitespace = true };
        try
        {
            document.Load(reader);
        }
        catch (Exception exception) when (exception is SgmlParseException or InvalidOperationException)
        {
            throw new XmlException(exception.Message, exception);
        }

        if (document.DocumentElement is null)
        {
            throw new XmlException("The file does not contain an HTML document.");
        }

        return document;
    }

    private sealed class EmbeddedHtmlDtdResolver : IEntityResolver
    {
        public IEntityContent GetContent(Uri uri)
        {
            if (uri.IsAbsoluteUri || !uri.OriginalString.Equals("Html.dtd", StringComparison.OrdinalIgnoreCase))
            {
                throw new XmlException("External entities are unavailable during HTML import.");
            }

            var assembly = typeof(SgmlReader).Assembly;
            var resource = assembly.GetManifestResourceNames().FirstOrDefault(
                name => name.Equals("Html.dtd", StringComparison.OrdinalIgnoreCase)
                    || name.EndsWith(".Html.dtd", StringComparison.OrdinalIgnoreCase));
            return resource is null
                ? throw new XmlException("The built-in HTML DTD could not be found.")
                : new EmbeddedResourceEntityContent(assembly, resource);
        }
    }
}
