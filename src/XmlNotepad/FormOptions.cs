using System;
using System.Drawing;
using System.Collections;
using System.ComponentModel;
using System.Windows.Forms;
using System.Drawing.Drawing2D;

namespace XmlNotepad
{
	/// <summary>
	/// Summary description for FormOptions.
	/// </summary>
	public class FormOptions : System.Windows.Forms.Form
	{
        private Settings settings;
        UserSettings userSettings;
        
        private System.Windows.Forms.Button buttonOK;
        private System.Windows.Forms.Button buttonCancel;
        private System.Windows.Forms.FontDialog fontDialog1;
        private System.Windows.Forms.ColorDialog colorDialog1;
        private PropertyGrid propertyGrid1;
        private Button buttonReset;

		/// <summary>
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.Container components = null;

		public FormOptions()
		{
			//
			// Required for Windows Form Designer support
			//
			InitializeComponent();
		}

        protected override void OnLoad(EventArgs e) {
            base.OnLoad(e);
        
            HelpProvider hp = this.Site.GetService(typeof(HelpProvider)) as HelpProvider;
            if (hp != null) {
                hp.SetHelpKeyword(this, "Options");
                hp.SetHelpNavigator(this, HelpNavigator.KeywordIndex);
            }

            // now let the user resize it.
            this.AutoSize = false; 
        }

        protected override bool ProcessDialogKey(Keys keyData) {
            if (keyData == Keys.Escape) {
                this.Close();
                return true;
            } else {
                return base.ProcessDialogKey(keyData);
            }
        }

		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		protected override void Dispose( bool disposing )
		{
			if( disposing )
			{
				if(components != null)
				{
					components.Dispose();
				}
			}
			base.Dispose( disposing );
		}

		#region Windows Form Designer generated code
		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(FormOptions));
            this.buttonOK = new System.Windows.Forms.Button();
            this.buttonCancel = new System.Windows.Forms.Button();
            this.fontDialog1 = new System.Windows.Forms.FontDialog();
            this.colorDialog1 = new System.Windows.Forms.ColorDialog();
            this.propertyGrid1 = new System.Windows.Forms.PropertyGrid();
            this.buttonReset = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // buttonOK
            // 
            resources.ApplyResources(this.buttonOK, "buttonOK");
            this.buttonOK.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.buttonOK.Name = "buttonOK";
            this.buttonOK.Click += new System.EventHandler(this.buttonOK_Click);
            // 
            // buttonCancel
            // 
            resources.ApplyResources(this.buttonCancel, "buttonCancel");
            this.buttonCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.buttonCancel.Name = "buttonCancel";
            // 
            // propertyGrid1
            // 
            resources.ApplyResources(this.propertyGrid1, "propertyGrid1");
            this.propertyGrid1.Name = "propertyGrid1";
            // 
            // buttonReset
            // 
            resources.ApplyResources(this.buttonReset, "buttonReset");
            this.buttonReset.Name = "buttonReset";
            this.buttonReset.UseVisualStyleBackColor = true;
            this.buttonReset.Click += new System.EventHandler(this.buttonReset_Click);
            // 
            // FormOptions
            // 
            this.AcceptButton = this.buttonOK;
            resources.ApplyResources(this, "$this");
            this.BackColor = System.Drawing.Color.White;
            this.CancelButton = this.buttonCancel;
            this.Controls.Add(this.buttonReset);
            this.Controls.Add(this.propertyGrid1);
            this.Controls.Add(this.buttonCancel);
            this.Controls.Add(this.buttonOK);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.SizableToolWindow;
            this.HelpButton = true;
            this.Name = "FormOptions";
            this.ShowIcon = false;
            this.ShowInTaskbar = false;
            this.ResumeLayout(false);

        }
		#endregion        

        public override ISite Site {
            get {
                return base.Site;
            }
            set {
                base.Site = value;
                if (value != null) {
                    this.settings = value.GetService(typeof(Settings)) as Settings;
                    this.userSettings = value.GetService(typeof(UserSettings)) as UserSettings;
                    this.propertyGrid1.SelectedObject = this.userSettings;
                }
            }
        }

        private void buttonOK_Click(object sender, System.EventArgs e) {
            this.userSettings.Apply();
            this.Close();
        }

        private void buttonReset_Click(object sender, EventArgs e) {
            if (DialogResult.OK == MessageBox.Show(this, SR.ResetOptionsPrompt, SR.ResetOptionsPromptCaption, MessageBoxButtons.OKCancel, MessageBoxIcon.Exclamation)) {
                this.userSettings.Reset();
                this.propertyGrid1.SelectedObject = null;
                this.propertyGrid1.SelectedObject = userSettings;
            }
        }

	}

    // This class keeps s a local snapshot of the settings until the user clicks the Ok button,
    // then the Apply() method is called to propagate the new settings to the underlying Settings object.
    // It also provides localizable strings for the property grid.
    public class UserSettings {
        Settings settings;
        Font font;
        string fontName;
        Color elementColor;
        Color commentColor;
        Color attributeColor;
        Color piColor;
        Color textColor;
        Color cdataColor;
        Color backgroundColor;
        string updateLocation;
        bool enableUpdate;
        bool noByteOrderMark;
        bool autoFormatOnSave;
        int indentLevel;
        IndentChar indentChar;
        string newLineChars;
        string language;
        int maximumLineLength;
        bool autoFormatLongLines;
        bool ignoreDTD;

        public UserSettings(Settings s) {            
            this.settings = s;

            this.font = (Font)this.settings["Font"];
            this.fontName = font.Name + " " + font.SizeInPoints + " " + font.Style.ToString();
            Hashtable colors = (Hashtable)this.settings["Colors"];
            elementColor = (Color)colors["Element"];
            commentColor = (Color)colors["Comment"];
            attributeColor = (Color)colors["Attribute"];
            piColor = (Color)colors["PI"];
            textColor = (Color)colors["Text"];
            cdataColor = (Color)colors["CDATA"];
            backgroundColor = (Color)colors["Background"];
            updateLocation = (string)this.settings["UpdateLocation"];
            enableUpdate = (bool)this.settings["UpdateEnabled"];
            autoFormatOnSave = (bool)this.settings["AutoFormatOnSave"];
            noByteOrderMark = (bool)this.settings["NoByteOrderMark"];
            indentLevel = (int)this.settings["IndentLevel"];
            indentChar = (IndentChar)this.settings["IndentChar"];
            newLineChars = (string)this.settings["NewLineChars"];
            language = (string)this.settings["Language"];
            maximumLineLength = (int)this.settings["MaximumLineLength"];
            autoFormatLongLines = (bool)this.settings["AutoFormatLongLines"];
            ignoreDTD = (bool)this.settings["IgnoreDTD"];
        }

        public static string Escape(string nl) {
            return nl.Replace("\r", "\\r").Replace("\n", "\\n");
        }
        public static string Unescape(string nl) {
            return nl.Replace("\\r", "\r").Replace("\\n", "\n");
        }

        public void Apply() {
            this.settings["Font"] = this.font;

            Hashtable colors = (Hashtable)this.settings["Colors"];
            colors["Element"] = this.elementColor;
            colors["Comment"] = this.commentColor;
            colors["CDATA"] = this.cdataColor;
            colors["Attribute"] = this.attributeColor;
            colors["PI"] = this.piColor;
            colors["Text"] = this.textColor;
            colors["Background"] = this.backgroundColor;
            
            this.settings["UpdateEnabled"] = this.enableUpdate;
            this.settings["UpdateLocation"] = this.updateLocation;

            this.settings["AutoFormatOnSave"] = autoFormatOnSave;
            this.settings["IndentLevel"] = indentLevel;
            this.settings["IndentChar"] = indentChar;
            this.settings["NewLineChars"] = newLineChars;
            this.settings["NoByteOrderMark"] = noByteOrderMark;

            this.settings["Language"] = ("" + this.language).Trim();
            this.settings["MaximumLineLength"] = this.maximumLineLength;
            this.settings["AutoFormatLongLines"] = this.autoFormatLongLines;
            this.settings["IgnoreDTD"] = this.ignoreDTD;

            this.settings.OnChanged("Colors");

        }

        public void Reset() {
            this.font = new Font("Courier New", 10, FontStyle.Regular);
            elementColor = Color.FromArgb(0, 64, 128); 
            commentColor = Color.Green;
            attributeColor = Color.Maroon;
            piColor = Color.Purple;
            textColor = Color.Black;
            cdataColor = Color.Gray;
            backgroundColor = Color.White;
            updateLocation = "http://www.lovettsoftware.com/downloads/xmlnotepad/Updates.xml";
            enableUpdate = true;
            autoFormatOnSave = true;
            noByteOrderMark = false;
            indentLevel = 2;
            indentChar = IndentChar.Space;
            newLineChars = Escape("\r\n");
            language = "";
            this.maximumLineLength = 10000;
            ignoreDTD = false;
        }

        [SRCategoryAttribute("ColorCategory")]
        [LocDisplayName("ElementColor")]
        [SRDescriptionAttribute("ElementColorDescription")]        
        public Color ElementColor {
            get {
                return this.elementColor;
            }
            set {
                this.elementColor = value;
            }
        }

        [SRCategoryAttribute("ColorCategory")]
        [LocDisplayName("AttributeColor")]
        [SRDescriptionAttribute("AttributeColorDescription")]
        public Color AttributeColor {
            get {
                return this.attributeColor;
            }
            set {
                this.attributeColor = value;
            }
        }

        [SRCategoryAttribute("ColorCategory")]
        [LocDisplayName("CommentColor")]
        [SRDescriptionAttribute("CommentColorDescription")]
        public Color CommentColor {
            get {
                return this.commentColor;
            }
            set {
                this.commentColor = value;
            }
        }
        [SRCategoryAttribute("ColorCategory")]
        [LocDisplayName("PiColor")]
        [SRDescriptionAttribute("PiColorDescription")]
        public Color PiColor {
            get {
                return this.piColor;
            }
            set {
                this.piColor = value;
            }
        }
        [SRCategoryAttribute("ColorCategory")]
        [LocDisplayName("TextColor")]
        [SRDescriptionAttribute("TextColorDescription")]
        public Color TextColor {
            get {
                return this.textColor;
            }
            set {
                this.textColor = value;
            }
        }
        [SRCategoryAttribute("ColorCategory")]
        [LocDisplayName("CDataColor")]
        [SRDescriptionAttribute("CDataColorDescription")]
        public Color CDataColor {
            get {
                return this.cdataColor;
            }
            set {
                this.cdataColor = value;
            }
        }
        [SRCategoryAttribute("ColorCategory")]
        [LocDisplayName("BackgroundColor")]
        [SRDescriptionAttribute("BackgroundColorDescription")]
        public Color BackgroundColor {
            get {
                return this.backgroundColor;
            }
            set {
                this.backgroundColor = value;
            }
        }

        [SRCategoryAttribute("FontCategory")]
        [LocDisplayName("FontPropertyName")]
        [SRDescriptionAttribute("FontDescription")]
        public Font Font {
            get {
                return this.font;
            }
            set {
                this.font = value;
            }
        }

        [SRCategoryAttribute("LanguageCategory")]
        [LocDisplayName("LanguagePropertyName")]
        [SRDescriptionAttribute("LanguageDescription")]
        public string Language
        {
            get
            {
                return this.language;
            }
            set
            {
                this.language = value;
            }
        }

        [SRCategoryAttribute("UpdateCategory")]
        [LocDisplayName("EnableUpdate")]
        [SRDescriptionAttribute("EnableUpdateDescription")]
        public bool EnableUpdate {
            get {
                return this.enableUpdate;
            }
            set {
                this.enableUpdate = value;
            }
        }
        [SRCategoryAttribute("UpdateCategory")]
        [LocDisplayName("UpdateLocation")]
        [SRDescriptionAttribute("UpdateLocationDescription")]
        public string UpdateLocation {
            get {
                return this.updateLocation;
            }
            set {
                this.updateLocation = value;
            }
        }

        [SRCategoryAttribute("FormatCategory")]
        [LocDisplayName("AutoFormatOnSave")]
        [SRDescriptionAttribute("AutoFormatOnSaveDescription")]
        public bool AutoFormatOnSave {
            get {
                return this.autoFormatOnSave;
            }
            set {
                this.autoFormatOnSave = value;
            }
        }
        [SRCategoryAttribute("FormatCategory")]
        [LocDisplayName("IndentLevel")]
        [SRDescriptionAttribute("IndentLevelDescription")]
        public int IndentLevel {
            get {
                return this.indentLevel;
            }
            set {
                this.indentLevel = value;
            }
        }
        [SRCategoryAttribute("FormatCategory")]
        [LocDisplayName("IndentChar")]
        [SRDescriptionAttribute("IndentCharDescription")]
        public IndentChar IndentChar {
            get {
                return this.indentChar;
            }
            set {
                this.indentChar = value;
            }
        }

        [SRCategoryAttribute("FormatCategory")]
        [LocDisplayName("NewLineChars")]
        [SRDescriptionAttribute("NewLineCharsDescription")]
        public string NewLineChars {
            get {
                return this.newLineChars;
            }
            set {
                this.newLineChars = value;
            }
        }

        [SRCategoryAttribute("FormatCategory")]
        [LocDisplayName("NoByteOrderMark")]
        [SRDescriptionAttribute("NoByteOrderMarkDescription")]
        public bool NoByteOrderMark
        {
            get
            {
                return this.noByteOrderMark;
            }
            set
            {
                this.noByteOrderMark = value;
            }
        }

        [SRCategoryAttribute("LongLineCategory")]
        [LocDisplayName("MaximumLineLengthProperty")]
        [SRDescriptionAttribute("MaximumLineLengthDescription")]
        public int MaximumLineLength
        {
            get
            {
                return this.maximumLineLength;
            }
            set
            {
                this.maximumLineLength = value;
            }
        }

        [SRCategoryAttribute("LongLineCategory")]
        [LocDisplayName("AutoFormatLongLinesProperty")]
        [SRDescriptionAttribute("AutoFormatLongLinesDescription")]
        public bool AutoFormatLongLines
        {
            get
            {
                return this.autoFormatLongLines;
            }
            set
            {
                this.autoFormatLongLines = value;
            }
        }

        [SRCategoryAttribute("Validation")]
        [LocDisplayName("IgnoreDTDProperty")]
        [SRDescriptionAttribute("IgnoreDTDDescription")]
        public bool IgnoreDTD
        {
            get
            {
                return this.ignoreDTD;
            }
            set
            {
                this.ignoreDTD = value;
            }
        }
    }
}
