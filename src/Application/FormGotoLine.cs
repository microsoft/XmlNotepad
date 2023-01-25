using System.Windows.Forms;

namespace XmlNotepad
{
    public partial class FormGotoLine : Form
    {
        int maxLine = 0;
        string promptPattern;

        public FormGotoLine()
        {
            InitializeComponent();
            promptPattern = labelLinePrompt.Text;
            textBoxLine.PreviewKeyDown += TextBox1_PreviewKeyDown;
            textBoxColumn.PreviewKeyDown += TextBox1_PreviewKeyDown;
        }

        private void TextBox1_PreviewKeyDown(object sender, PreviewKeyDownEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                if (this.LineNumber >= 0)
                {
                    this.DialogResult = DialogResult.OK;
                }
            }
            else if (e.KeyCode == Keys.Escape)
            {
                this.DialogResult = DialogResult.Cancel;
            }
        }

        public int LineNumber
        {
            get
            {
                string text = textBoxLine.Text;
                if (int.TryParse(text, out int value))
                {
                    return value;
                }
                return -1;
            }
            set
            {
                textBoxLine.Text = value.ToString();
            }
        }

        public int Column
        {
            get
            {
                string text = textBoxColumn.Text;
                if (int.TryParse(text, out int value))
                {
                    return value;
                }
                return -1;
            }
            set
            {
                textBoxColumn.Text = value.ToString();
            }
        }

        public int MaxLineNumber
        {
            get
            {
                return this.maxLine;
            }
            set
            {
                this.maxLine = value;
                this.labelLinePrompt.Text = string.Format(promptPattern, value);
            }
        }

        private void textBox1_TextChanged(object sender, System.EventArgs e)
        {
            button1.Enabled = (this.LineNumber != -1);
        }
    }
}
