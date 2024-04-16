using System;
using System.Windows.Forms;

namespace XmlNotepad
{
    public partial class FormTransformProgress : Form
    {
        public FormTransformProgress()
        {
            InitializeComponent();
        }

        public void SetProgress(int min, int max, int value)
        {
            if (max == 0)
            {
                this.progressBar1.Style = ProgressBarStyle.Marquee;
            }
            else
            {
                this.progressBar1.Style = ProgressBarStyle.Continuous;
                this.progressBar1.Minimum = min;
                this.progressBar1.Maximum = max;
                this.progressBar1.Value = value;
            }
        }


        private void buttonCancel_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.Cancel;
            this.Hide();
        }
    }
}
