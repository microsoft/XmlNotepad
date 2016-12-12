using Microsoft.Xml;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace XmlNotepad
{
    public partial class FormCsvImport : Form
    {
        string fileName;
        static string[] delimiterNames = new string[] { "Comma (,)", "Tab", "Space", "Semicolon (;)", "Colon (:)", "Vertical bar (|)", "Slash (/)" };
        static char[] delimiters = new char[] { ',', '\t', ' ', ';', ':', '|', '/' };

        public FormCsvImport()
        {
            InitializeComponent();
            
            // remove the test label we use just for design mode.
            flowLayoutPanel1.Controls.Remove(labelTest);

            foreach (var delim in delimiterNames)
            {
                comboBoxDelimiters.Items.Add(delim);
            }
            comboBoxDelimiters.SelectedIndex = 0;
            ShowStatus("");
            comboBoxDelimiters.TextChanged += ComboBoxDelimiters_TextChanged;
        }

        private void ShowStatus(string msg)
        {
            labelStatus.Text = msg;
        }

        public string FileName
        {
            get { return fileName; }
            set { fileName = value; SniffHeaders(); }
        }

        public char Deliminter { get; set; }

        public bool FirstRowIsHeader { get; set; }

        void SniffHeaders()
        {
            flowLayoutPanel1.Controls.Clear();

            // sniff the file, see if we can figure out the delimiter
            if (string.IsNullOrEmpty(this.fileName) || !File.Exists(this.fileName))
            {
                return;
            }

            string userText = null;
            if (comboBoxDelimiters.SelectedIndex >= 0)
            {
                if (comboBoxDelimiters.Text != delimiterNames[comboBoxDelimiters.SelectedIndex])
                {
                    // user is typing in something new
                    userText = comboBoxDelimiters.Text;
                }
            }
            else
            {
                // user is typing in something new
                userText = comboBoxDelimiters.Text;

            }
            if (userText != null && string.IsNullOrEmpty(userText))
            {
                return;
            }
            else if (userText == null && comboBoxDelimiters.SelectedIndex < 0)
            {
                return;
            }

            ShowStatus("");

            using (StreamReader reader = new StreamReader(this.fileName))
            {
                CsvReader csvReader = new CsvReader(reader, 8192);
                if (userText == null)
                {
                    Deliminter = delimiters[comboBoxDelimiters.SelectedIndex];
                }
                else 
                {
                    Deliminter = userText[0];
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
                        flowLayoutPanel1.Controls.Add(label);
                    }
                }
                else
                {
                    ShowStatus("Now rows found in that file");
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
            FirstRowIsHeader = checkBoxHeadings.Checked;
        }

        private void comboBoxDelimiters_SelectedIndexChanged(object sender, EventArgs e)
        {
            SniffHeaders();
        }
        private void ComboBoxDelimiters_TextChanged(object sender, EventArgs e)
        {
            SniffHeaders();
        }

    }
}
