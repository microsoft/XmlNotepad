using System;
using System.Text;
using System.Drawing;
using System.Windows.Forms;

namespace XmlNotepad {
    public interface IEditableView {
        bool BeginEdit(string value);
        bool EndEdit(bool cancel);
        bool IsEditing { get; }
        void SelectText(int index, int length);
        bool ReplaceText(int index, int length, string replacement);
        int SelectionStart { get; }
        int SelectionLength { get; }
        Rectangle EditorBounds { get; }
        void BubbleKeyDown(KeyEventArgs e);
    }
}
