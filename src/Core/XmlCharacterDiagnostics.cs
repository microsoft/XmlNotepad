using System.Collections.Generic;
using System.Globalization;
using System.Xml;

namespace XmlNotepad
{
    public static class XmlCharacterDiagnostics
    {
        public static IEnumerable<string> GetErrors(string value)
        {
            for (int index = 0; index < value.Length; index++)
            {
                char character = value[index];
                if (XmlConvert.IsXmlChar(character))
                {
                    continue;
                }

                if (char.IsHighSurrogate(character) && index + 1 < value.Length)
                {
                    char next = value[index + 1];
                    if (char.IsLowSurrogate(next))
                    {
                        index++;
                        continue;
                    }

                    yield return string.Format(CultureInfo.CurrentCulture, Strings.IllegalSurrogatePair,
                        (int)character, (int)next, index);
                    if (char.IsHighSurrogate(next) && (index + 2 == value.Length || !char.IsLowSurrogate(value[index + 2])))
                    {
                        index++;
                    }
                }
                else
                {
                    yield return string.Format(CultureInfo.CurrentCulture, Strings.InvalidCharacter, (int)character, index);
                }
            }
        }
    }
}
