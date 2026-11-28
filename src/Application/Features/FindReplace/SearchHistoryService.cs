namespace XmlNotepad.Services;

public sealed class SearchHistoryService(AppSettingsService settingsService)
{
    private const int MaximumHistoryItems = 100;

    public IReadOnlyList<string> FindItems => GetItems("RecentFindStrings");

    public IReadOnlyList<string> ReplaceItems => GetItems("RecentReplaceStrings");

    public void AddFind(string find)
    {
        if (Add("RecentFindStrings", find))
        {
            settingsService.Save();
        }
    }

    public void AddTerms(string find, string replace)
    {
        var changed = Add("RecentFindStrings", find);
        changed |= Add("RecentReplaceStrings", replace);
        if (changed)
        {
            settingsService.Save();
        }
    }

    private IReadOnlyList<string> GetItems(string key) =>
        settingsService.Settings[key] as string[] ?? [];

    private bool Add(string key, string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        var items = GetItems(key).ToList();
        var existingIndex = items.IndexOf(value);
        if (existingIndex >= 0 && existingIndex == items.Count - 1)
        {
            return false;
        }
        if (existingIndex >= 0)
        {
            items.RemoveAt(existingIndex);
        }
        items.Add(value);
        if (items.Count > MaximumHistoryItems)
        {
            items.RemoveRange(0, items.Count - MaximumHistoryItems);
        }

        settingsService.Settings[key] = items.ToArray();
        return true;
    }
}
