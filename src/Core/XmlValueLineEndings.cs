using System;

namespace XmlNotepad
{
    public static class XmlValueLineEndings
    {
        public static bool HasLineLongerThan(string value, int maximumLength)
        {
            if (maximumLength <= 0 || value.Length <= maximumLength)
            {
                return false;
            }

            var length = 0;
            foreach (var character in value)
            {
                if (character == '\r' || character == '\n')
                {
                    length = 0;
                }
                else if (++length > maximumLength)
                {
                    return true;
                }
            }
            return false;
        }

        public static int GetEditorOffset(string value, int offset)
        {
            offset = Math.Max(0, Math.Min(offset, value.Length));
            var editorOffset = offset;
            for (var index = 1; index < offset; index++)
            {
                if (value[index - 1] == '\r' && value[index] == '\n')
                {
                    editorOffset--;
                }
            }
            return editorOffset;
        }

        public static string ApplyEditorChange(string value, string previousText, string editedText)
        {
            if (string.Equals(previousText, editedText, StringComparison.Ordinal))
            {
                return value;
            }

            var sharedLength = Math.Min(previousText.Length, editedText.Length);
            var prefix = 0;
            while (prefix < sharedLength && previousText[prefix] == editedText[prefix])
            {
                prefix++;
            }

            var suffix = 0;
            while (suffix < sharedLength - prefix && previousText[previousText.Length - suffix - 1] == editedText[editedText.Length - suffix - 1])
            {
                suffix++;
            }

            var start = GetXmlOffset(value, prefix);
            var end = GetXmlOffset(value, previousText.Length - suffix);
            var inserted = editedText.Substring(prefix, editedText.Length - prefix - suffix);
            if (inserted.IndexOf('\r') >= 0)
            {
                inserted = inserted.Replace("\r\n", "\n").Replace('\r', '\n');
            }

            return value.Substring(0, start) + inserted + value.Substring(end);
        }

        private static int GetXmlOffset(string value, int editorOffset)
        {
            var index = 0;
            while (editorOffset > 0 && index < value.Length)
            {
                if (value[index] == '\r' && index + 1 < value.Length && value[index + 1] == '\n')
                {
                    index++;
                }
                index++;
                editorOffset--;
            }
            return index;
        }
    }
}
