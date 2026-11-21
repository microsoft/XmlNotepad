using System.Xml;
using Microsoft.Xml;

namespace XmlNotepad.Services;

internal static class CsvImport
{
    public static string[] ReadFirstRow(string text, char delimiter)
    {
        using var input = new StringReader(text);
        var reader = new CsvReader(input, 4096) { Delimiter = delimiter };
        if (!reader.Read())
        {
            return [];
        }

        var fields = new string[reader.FieldCount];
        for (int index = 0; index < fields.Length; index++)
        {
            fields[index] = reader[index];
        }

        return fields;
    }

    public static XmlDocument Convert(Uri location, string text, char delimiter, bool firstRowIsHeader)
    {
        using var input = new StringReader(text);
        var document = new XmlDocument();
        using var reader = new XmlCsvReader(input, location, document.NameTable)
        {
            Delimiter = delimiter,
            FirstRowHasColumnNames = firstRowIsHeader
        };
        document.Load(reader);
        return document;
    }
}
