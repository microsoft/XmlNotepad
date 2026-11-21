namespace XmlNotepad.Services;

[Flags]
public enum FindFlags
{
    Normal = 0,
    Regex = 1,
    XPath = 2,
    MatchCase = 4,
    WholeWord = 8,
    Backwards = 16,
    StartsWith = 32
}

public enum SearchFilter
{
    Everything,
    Names,
    Text,
    Comments
}
