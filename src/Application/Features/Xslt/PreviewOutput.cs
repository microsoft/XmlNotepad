namespace XmlNotepad.Services;

public sealed record PreviewOutput(
    string Content,
    byte[] OutputBytes,
    string FileExtension,
    string SuggestedFileName);
