using Microsoft.Xml;
using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace XmlNotepad
{
    public partial class FormCsvImport : Form
    {
        private string _fileName;
        private static string[] _delimiterNames = new string[] { "Comma (,)", "Tab", "Space", "Semicolon (;)", "Colon (:)", "Vertical bar (|)", "Slash (/)" };
        private static char[] _delimiters = new char[] { ',', '\t', ' ', ';', ':', '|', '/' };

        public FormCsvImport()
        {
            InitializeComponent();

            // Remove the test label we use only for design mode.
            this.flowLayoutPanel1.Controls.Remove(this.labelTest);

            foreach (var delim in FormCsvImport._delimiterNames)
            {
                this.comboBoxDelimiters.Items.Add(delim);
            }
            this.comboBoxDelimiters.SelectedIndex = 0;
            ShowStatus("");
            this.comboBoxDelimiters.TextChanged += ComboBoxDelimiters_TextChanged;
        }

        private void ShowStatus(string msg)
        {
            this.labelStatus.Text = msg;
        }

        public string FileName
        {
            get { return this._fileName; }
            set { this._fileName = value; SniffHeaders(); }
        }

        public char Deliminter { get; set; }

        public bool FirstRowIsHeader { get; set; }

        void SniffHeaders()
        {
            flowLayoutPanel1.Controls.Clear();

            // Sniff the file, see if we can figure out the delimiter
            if (string.IsNullOrEmpty(this._fileName) || !File.Exists(this._fileName))
            {
                return;
            }

            string userText = null;
            if (this.comboBoxDelimiters.SelectedIndex >= 0)
            {
                if (this.comboBoxDelimiters.Text != FormCsvImport._delimiterNames[this.comboBoxDelimiters.SelectedIndex])
                {
                    // User is typing in something new
                    userText = this.comboBoxDelimiters.Text;
                }
            }
            else
            {
                // User is typing in something new
                userText = this.comboBoxDelimiters.Text;

            }
            if (userText != null && string.IsNullOrEmpty(userText))
            {
                return;
            }
            else if (userText == null && this.comboBoxDelimiters.SelectedIndex < 0)
            {
                return;
            }

            ShowStatus("");

            using (StreamReader reader = new StreamReader(this._fileName))
            {
                CsvReader csvReader = new CsvReader(reader, 8192);
                if (userText == null)
                {
                    this.Deliminter = FormCsvImport._delimiters[this.comboBoxDelimiters.SelectedIndex];
                }
                else
                {
                    this.Deliminter = userText[0];
                }

                csvReader.Delimiter = Deliminter;

                if (csvReader.Read())
                {
                    for (int i = 0, n = csvReader.FieldCount; i < n; i++)
                    {
                        Label label = new Label();
                        label.Text = csvReader[i];
                        label.Margin = new Padding(5);
                        label.Padding = new Padding(5);
                        label.BackColor = Color.Firebrick;
                        label.ForeColor = Color.White;
                        label.AutoSize = true;
                        label.TextAlign = ContentAlignment.MiddleLeft;
                        this.flowLayoutPanel1.Controls.Add(label);
                    }
                }
                else
                {
                    ShowStatus("No rows found in that file.");
                }
            }
        }


        private void buttonOK_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.OK;
        }

        private void buttonCancel_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.Cancel;
        }

        private void checkBoxHeadings_CheckedChanged(object sender, EventArgs e)
        {
            this.FirstRowIsHeader = this.checkBoxHeadings.Checked;
        }

        private void comboBoxDelimiters_SelectedIndexChanged(object sender, EventArgs e)
        {
            this.SniffHeaders();
        }
        private void ComboBoxDelimiters_TextChanged(object sender, EventArgs e)
        {
            this.SniffHeaders();
        }

    }
}
