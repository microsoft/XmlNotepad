using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;

namespace XmlNotepad.ViewModels;

internal sealed class XmlTreeRows : ObservableCollection<XmlNodeItem>
{
    private readonly List<XmlNodeItem> _rows = [];
    private readonly Stack<XmlNodeItem> _pending = new();

    internal void Update(IReadOnlyList<XmlNodeItem> roots)
    {
        _rows.Clear();
        PushReversed(roots);
        while (_pending.TryPop(out var item))
        {
            _rows.Add(item);
            if (item.IsExpanded)
            {
                PushReversed(item.Children);
            }
        }

        var start = 0;
        while (start < Count && start < _rows.Count && ReferenceEquals(this[start], _rows[start]))
        {
            start++;
        }

        var oldEnd = Count;
        var newEnd = _rows.Count;
        while (oldEnd > start && newEnd > start && ReferenceEquals(this[oldEnd - 1], _rows[newEnd - 1]))
        {
            oldEnd--;
            newEnd--;
        }

        if (oldEnd - start + newEnd - start > 32)
        {
            Items.Clear();
            foreach (var row in _rows)
            {
                Items.Add(row);
            }
            OnPropertyChanged(new PropertyChangedEventArgs(nameof(Count)));
            OnPropertyChanged(new PropertyChangedEventArgs("Item[]"));
            OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
        }
        else
        {
            for (var index = oldEnd - 1; index >= start; index--)
            {
                RemoveAt(index);
            }
            for (var index = start; index < newEnd; index++)
            {
                Insert(index, _rows[index]);
            }
        }
        _rows.Clear();
    }

    private void PushReversed(IReadOnlyList<XmlNodeItem> rows)
    {
        for (var index = rows.Count - 1; index >= 0; index--)
        {
            _pending.Push(rows[index]);
        }
    }
}
