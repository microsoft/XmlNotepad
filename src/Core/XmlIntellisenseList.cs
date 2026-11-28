using System.Collections.Generic;

namespace XmlNotepad
{
    public sealed class XmlIntellisenseList
    {
        private readonly HashSet<string> _unique = new HashSet<string>();
        private readonly List<(string Name, string Tooltip)> _items = new List<(string Name, string Tooltip)>();

        internal void Add(string name, string tip)
        {
            if (!string.IsNullOrEmpty(name) && _unique.Add(name))
            {
                _items.Add((name, string.IsNullOrEmpty(tip) ? null : tip));
            }
        }

        internal void Sort() => _items.Sort((left, right) => string.Compare(left.Name, right.Name));

        public int Count => _items.Count;
        public string GetName(int index) => _items[index].Name;
        public string GetTooltip(int index) => _items[index].Tooltip;
    }
}
