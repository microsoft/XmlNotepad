using XmlNotepad;

namespace XmlNotepad.Services;

public sealed class FindReplaceRequestEventArgs(string expression, string replacement, FindFlags flags, SearchFilter filter, bool isPreview) : EventArgs
{
    public string Expression { get; } = expression;
    public string Replacement { get; } = replacement;
    public FindFlags Flags { get; } = flags;
    public SearchFilter Filter { get; } = filter;
    public bool IsPreview { get; } = isPreview;
}
