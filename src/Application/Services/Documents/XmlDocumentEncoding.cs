using System.Text;
using System.Xml;

namespace XmlNotepad.Services;

internal static class XmlDocumentEncoding
{
    internal static Encoding? ReadPrefix(Stream stream)
    {
        if (!stream.CanSeek)
        {
            return null;
        }

        var position = stream.Position;
        Span<byte> prefix = stackalloc byte[4];
        var read = stream.ReadAtLeast(prefix, prefix.Length, false);
        stream.Position = position;
        return prefix[..read] switch
        {
            [0x00, 0x00, 0xfe, 0xff] => new UTF32Encoding(bigEndian: true, byteOrderMark: true),
            [0xff, 0xfe, 0x00, 0x00] => Encoding.UTF32,
            [0xef, 0xbb, 0xbf, ..] => Encoding.UTF8,
            [0xfe, 0xff, ..] => Encoding.BigEndianUnicode,
            [0xff, 0xfe, ..] => Encoding.Unicode,
            [0x00, 0x00, 0x00, 0x3c] => new UTF32Encoding(bigEndian: true, byteOrderMark: false),
            [0x3c, 0x00, 0x00, 0x00] => new UTF32Encoding(bigEndian: false, byteOrderMark: false),
            [0x00, 0x3c, 0x00, _] => new UnicodeEncoding(bigEndian: true, byteOrderMark: false),
            [0x3c, 0x00, _, 0x00] => new UnicodeEncoding(bigEndian: false, byteOrderMark: false),
            _ => new UTF8Encoding(encoderShouldEmitUTF8Identifier: false)
        };
    }

    internal static Encoding ForSave(XmlDocument document, Encoding? sourceEncoding)
    {
        var name = (document.FirstChild as XmlDeclaration)?.Encoding;
        if (string.IsNullOrWhiteSpace(name))
        {
            return sourceEncoding ?? Encoding.UTF8;
        }

        try
        {
            var declared = Encoding.GetEncoding(name);
            return sourceEncoding is not null &&
                (sourceEncoding.CodePage == declared.CodePage ||
                 sourceEncoding.CodePage == 1201 && name.Equals("utf-16", StringComparison.OrdinalIgnoreCase) ||
                 sourceEncoding.CodePage == 12001 && name.Equals("utf-32", StringComparison.OrdinalIgnoreCase))
                ? sourceEncoding : declared;
        }
        catch (ArgumentException exception)
        {
            throw new XmlException($"'{name}' is not a supported XML encoding.", exception);
        }
    }
}
