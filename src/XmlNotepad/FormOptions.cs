using System;
using System.Drawing;
using System.Collections;
using System.ComponentModel;
using System.Windows.Forms;
using System.Drawing.Drawing2D;
using System.Collections.Generic;

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
            if (hp != null && Utilities.DynamicHelpEnabled)
            {
                hp.HelpNamespace = Utilities.OptionsHelp;
            }

            // now let the user resize it.
            this.AutoSize = false; 
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            HelpProvider hp = this.Site.GetService(typeof(HelpProvider)) as HelpProvider;
            if (hp != null && Utilities.DynamicHelpEnabled)
            {
                hp.HelpNamespace = Utilities.DefaultHelp;
            }

            base.OnClosing(e);
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

                    string[] hiddenProperties = new string[0];
                    if ((string)this.settings["AnalyticsClientId"] == "disabled")
                    {
                        hiddenProperties = new string[] { "AllowAnalytics" };
                    }

                    MemberFilter filter = new MemberFilter(this.userSettings, hiddenProperties);
                    this.propertyGrid1.SelectedObject = filter;
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

    public enum ColorTheme
    {
        Light,
        Dark
    }

    // This class keeps s a local snapshot of the settings until the user clicks the Ok button,
    // then the Apply() method is called to propagate the new settings to the underlying Settings object.
    // It also provides localizable strings for the property grid.
    public class UserSettings
    {
        Settings settings;
        Font font;
        string fontName;
        ColorTheme theme;
        Hashtable lightColors;
        Hashtable darkColors;
        Color elementColor;
        Color commentColor;
        Color attributeColor;
        Color piColor;
        Color textColor;
        Color cdataColor;
        Color backgroundColor;
        Color containerBackgroundColor;
        Color editorBackgroundColor;
        string updateLocation;
        bool enableUpdate;
        bool noByteOrderMark;
        bool autoFormatOnSave;
        int indentLevel;
        IndentChar indentChar;
        string newLineChars;
        string language;
        int maximumLineLength;
        int maximumValueLength;
        bool autoFormatLongLines;
        bool ignoreDTD;
        bool xmlDiffIgnoreChildOrder;
        bool xmlDiffIgnoreComments;
        bool xmlDiffIgnorePI;
        bool xmlDiffIgnoreWhitespace;
        bool xmlDiffIgnoreNamespaces;
        bool xmlDiffIgnorePrefixes;
        bool xmlDiffIgnoreXmlDecl;
        bool xmlDiffIgnoreDtd;
        bool allowAnalytics;

        public static string DefaultUpdateLocation = "https://lovettsoftwarestorage.blob.core.windows.net/downloads/XmlNotepad/Updates.xml";

        public UserSettings(Settings s) {            
            this.settings = s;

            this.font = (Font)this.settings["Font"];
            this.fontName = font.Name + " " + font.SizeInPoints + " " + font.Style.ToString();
            this.theme = (ColorTheme)this.settings["Theme"];
            lightColors = (Hashtable)this.settings["LightColors"];
            darkColors = (Hashtable)this.settings["DarkColors"];
            LoadColors();
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

            this.xmlDiffIgnoreChildOrder = (bool)this.settings["XmlDiffIgnoreChildOrder"];
            this.xmlDiffIgnoreComments = (bool)this.settings["XmlDiffIgnoreComments"];
            this.xmlDiffIgnorePI = (bool)this.settings["XmlDiffIgnorePI"];
            this.xmlDiffIgnoreWhitespace = (bool)this.settings["XmlDiffIgnoreWhitespace"];
            this.xmlDiffIgnoreNamespaces = (bool)this.settings["XmlDiffIgnoreNamespaces"];
            this.xmlDiffIgnorePrefixes = (bool)this.settings["XmlDiffIgnorePrefixes"];
            this.xmlDiffIgnoreXmlDecl = (bool)this.settings["XmlDiffIgnoreXmlDecl"];
            this.xmlDiffIgnoreDtd = (bool)this.settings["XmlDiffIgnoreDtd"];
            this.allowAnalytics = (bool)this.settings["AllowAnalytics"];
        }

        private void LoadColors()
        {
            Hashtable colors = this.theme == ColorTheme.Light ? lightColors : darkColors;
            elementColor = (Color)colors["Element"];
            commentColor = (Color)colors["Comment"];
            attributeColor = (Color)colors["Attribute"];
            piColor = (Color)colors["PI"];
            textColor = (Color)colors["Text"];
            cdataColor = (Color)colors["CDATA"];
            backgroundColor = (Color)colors["Background"];
            containerBackgroundColor = (Color)colors["ContainerBackground"];
            editorBackgroundColor = (Color)colors["EditorBackground"];
        }

        internal static Hashtable GetDefaultColors(ColorTheme theme)
        {
            if (theme == ColorTheme.Light)
            {
                System.Collections.Hashtable light = new System.Collections.Hashtable();
                light["Element"] = Color.FromArgb(0, 64, 128);
                light["Attribute"] = Color.Maroon;
                light["Text"] = Color.Black;
                light["Comment"] = Color.Green;
                light["PI"] = Color.Purple;
                light["CDATA"] = Color.Gray;
                light["Background"] = Color.White;
                light["ContainerBackground"] = Color.AliceBlue;
                light["EditorBackground"] = Color.LightSteelBlue;
                return light;
            }
            else
            {
                System.Collections.Hashtable dark = new System.Collections.Hashtable();
                dark["Element"] = Color.FromArgb(0x35, 0x7D, 0xCE);
                dark["Attribute"] = Color.FromArgb(0x92, 0xCA, 0xF3);
                dark["Text"] = Color.FromArgb(0x94, 0xB7, 0xC8);
                dark["Comment"] = Color.FromArgb(0x45, 0x62, 0x23);
                dark["PI"] = Color.FromArgb(0xAC, 0x91, 0x6A);
                dark["CDATA"] = Color.FromArgb(0xC2, 0xCB, 0x85);
                dark["Background"] = Color.FromArgb(0x1e, 0x1e, 0x1e);
                dark["ContainerBackground"] = Color.FromArgb(0x25, 0x25, 0x26);
                dark["EditorBackground"] = Color.FromArgb(24, 24, 44);
                return dark;
            }
        }

        internal static void AddDefaultColors(Settings settings, string name, ColorTheme theme)
        {
            Hashtable table = (Hashtable)settings[name];
            if (table == null)
            {
                table = new Hashtable();
                settings[name] = table;
            }

            Hashtable defaults = GetDefaultColors(theme);
            // Merge any undefined colors.
            foreach (string key in defaults.Keys)
            {
                if (!table.ContainsKey(key))
                {
                    table.Add(key, defaults[key]);
                }
            }
        }

        internal void SaveColors()
        {
            Hashtable colors = this.theme == ColorTheme.Light ? this.lightColors : this.darkColors;
            colors["Element"] = this.elementColor;
            colors["Comment"] = this.commentColor;
            colors["CDATA"] = this.cdataColor;
            colors["Attribute"] = this.attributeColor;
            colors["PI"] = this.piColor;
            colors["Text"] = this.textColor;
            colors["Background"] = this.backgroundColor;
            colors["ContainerBackground"] = this.containerBackgroundColor;
            colors["EditorBackground"] = this.editorBackgroundColor;
        }

        public static string Escape(string nl) {
            return nl.Replace("\r", "\\r").Replace("\n", "\\n");
        }
        public static string Unescape(string nl) {
            return nl.Replace("\\r", "\r").Replace("\\n", "\n");
        }

        public void Apply() {
            this.settings["Font"] = this.font;

            this.settings["Theme"] = this.theme;
            
            SaveColors();
            this.settings["LightColors"] = this.lightColors;
            this.settings["DarkColors"] = this.darkColors;

            this.settings["UpdateEnabled"] = this.enableUpdate;
            this.settings["UpdateLocation"] = this.updateLocation;

            this.settings["AutoFormatOnSave"] = autoFormatOnSave;
            this.settings["IndentLevel"] = indentLevel;
            this.settings["IndentChar"] = indentChar;
            this.settings["NewLineChars"] = newLineChars;
            this.settings["NoByteOrderMark"] = noByteOrderMark;

            this.settings["Language"] = ("" + this.language).Trim();
            this.settings["MaximumLineLength"] = this.maximumLineLength;
            this.settings["MaximumValueLength"] = this.maximumValueLength;
            this.settings["AutoFormatLongLines"] = this.autoFormatLongLines;
            this.settings["IgnoreDTD"] = this.ignoreDTD;

            this.settings["XmlDiffIgnoreChildOrder"] = this.xmlDiffIgnoreChildOrder;
            this.settings["XmlDiffIgnoreComments"] = this.xmlDiffIgnoreComments;
            this.settings["XmlDiffIgnorePI"] = this.xmlDiffIgnorePI;
            this.settings["XmlDiffIgnoreWhitespace"] = this.xmlDiffIgnoreWhitespace;
            this.settings["XmlDiffIgnoreNamespaces"] = this.xmlDiffIgnoreNamespaces;
            this.settings["XmlDiffIgnorePrefixes"] = this.xmlDiffIgnorePrefixes;
            this.settings["XmlDiffIgnoreXmlDecl"] = this.xmlDiffIgnoreXmlDecl;
            this.settings["XmlDiffIgnoreDtd"] = this.xmlDiffIgnoreDtd;

            this.settings["AllowAnalytics"] = this.allowAnalytics;

            this.settings.OnChanged("Colors");

        }

        public void Reset() {
            this.font = new Font("Courier New", 10, FontStyle.Regular);

            this.theme = ColorTheme.Light;
            this.lightColors = GetDefaultColors(ColorTheme.Light);
            this.darkColors = GetDefaultColors(ColorTheme.Dark);
            this.LoadColors();
            updateLocation = DefaultUpdateLocation;
            enableUpdate = true;
            autoFormatOnSave = true;
            noByteOrderMark = false;
            indentLevel = 2;
            indentChar = IndentChar.Space;
            newLineChars = Escape("\r\n");
            language = "";
            this.maximumLineLength = 10000;
            this.maximumValueLength = short.MaxValue;
            ignoreDTD = false;
            this.allowAnalytics = false;
        }

        [SRCategoryAttribute("ThemeCategory")]
        [LocDisplayName("Theme")]
        [SRDescriptionAttribute("ThemeDescription")]
        public ColorTheme Theme
        {
            get
            {
                return this.theme;
            }
            set
            {
                if (this.theme != value)
                {
                    SaveColors();
                    this.theme = value;
                    LoadColors();
                }
            }
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

        [SRCategoryAttribute("ColorCategory")]
        [LocDisplayName("ContainerBackgroundColor")]
        [SRDescriptionAttribute("ContainerBackgroundColorDescription")]
        public Color ContainerBackgroundColor
        {
            get
            {
                return this.containerBackgroundColor;
            }
            set
            {
                this.containerBackgroundColor = value;
            }
        }

        [SRCategoryAttribute("ColorCategory")]
        [LocDisplayName("EditorBackgroundColor")]
        [SRDescriptionAttribute("EditorBackgroundColorDescription")]
        public Color EditorBackgroundColor
        {
            get
            {
                return this.editorBackgroundColor;
            }
            set
            {
                this.editorBackgroundColor = value;
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

        [SRCategoryAttribute("AnalyticsCategory")]
        [LocDisplayName("AllowAnalytics")]
        [SRDescriptionAttribute("AllowAnalyticsDescription")]
        public bool AllowAnalytics
        {
            get
            {
                return this.allowAnalytics;
            }
            set
            {
                this.allowAnalytics = value;
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
        [LocDisplayName("MaximumValueLengthProperty")]
        [SRDescriptionAttribute("MaximumValueLengthDescription")]
        public int MaximumValueLength
        {
            get
            {
                return this.maximumValueLength;
            }
            set
            {
                this.maximumValueLength = value;
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


        [SRCategoryAttribute("XmlDiff")]
        [LocDisplayName("XmlDiffIgnoreChildOrderProperty")]
        [SRDescriptionAttribute("XmlDiffIgnoreChildOrderDescription")]
        public bool XmlDiffIgnoreChildOrder
        {
            get
            {
                return this.xmlDiffIgnoreChildOrder;
            }
            set
            {
                this.xmlDiffIgnoreChildOrder = value;
            }
        }
        [SRCategoryAttribute("XmlDiff")]
        [LocDisplayName("XmlDiffIgnoreCommentsProperty")]
        [SRDescriptionAttribute("XmlDiffIgnoreCommentsDescription")]
        public bool XmlDiffIgnoreComments
        {
            get { return this.xmlDiffIgnoreComments; }
            set { this.xmlDiffIgnoreComments = value; }
        }
        [SRCategoryAttribute("XmlDiff")]
        [LocDisplayName("XmlDiffIgnorePIProperty")]
        [SRDescriptionAttribute("XmlDiffIgnorePIDescription")]
        public bool XmlDiffIgnorePI
        {
            get { return this.xmlDiffIgnorePI; }
            set { this.xmlDiffIgnorePI = value; }
        }
        [SRCategoryAttribute("XmlDiff")]
        [LocDisplayName("XmlDiffIgnoreWhitespaceProperty")]
        [SRDescriptionAttribute("XmlDiffIgnoreWhitespaceDescription")]
        public bool XmlDiffIgnoreWhitespace
        {
            get { return this.xmlDiffIgnoreWhitespace; }
            set { this.xmlDiffIgnoreWhitespace = value; }
        }
        [SRCategoryAttribute("XmlDiff")]
        [LocDisplayName("XmlDiffIgnoreNamespacesProperty")]
        [SRDescriptionAttribute("XmlDiffIgnoreNamespacesDescription")]
        public bool XmlDiffIgnoreNamespaces
        {
            get { return this.xmlDiffIgnoreNamespaces; }
            set { this.xmlDiffIgnoreNamespaces = value; }
        }
        [SRCategoryAttribute("XmlDiff")]
        [LocDisplayName("XmlDiffIgnorePrefixesProperty")]
        [SRDescriptionAttribute("XmlDiffIgnorePrefixesDescription")]
        public bool XmlDiffIgnorePrefixes
        {
            get { return this.xmlDiffIgnorePrefixes; }
            set { this.xmlDiffIgnorePrefixes = value; }
        }
        [SRCategoryAttribute("XmlDiff")]
        [LocDisplayName("XmlDiffIgnoreXmlDeclProperty")]
        [SRDescriptionAttribute("XmlDiffIgnoreXmlDeclDescription")]
        public bool XmlDiffIgnoreXmlDecl
        {
            get { return this.xmlDiffIgnoreXmlDecl; }
            set { this.xmlDiffIgnoreXmlDecl = value; }
        }
        [SRCategoryAttribute("XmlDiff")]
        [LocDisplayName("XmlDiffIgnoreDtdProperty")]
        [SRDescriptionAttribute("XmlDiffIgnoreDtdDescription")]
        public bool XmlDiffIgnoreDtd
        {
            get { return this.xmlDiffIgnoreDtd; }
            set { this.xmlDiffIgnoreDtd = value; }
        }

    }

    public sealed class MemberFilter : ICustomTypeDescriptor
    {
        private readonly HashSet<string> hidden = new HashSet<string>();
        private readonly object component;

        public MemberFilter(object component, params string[] memberNamesToHide)
        {
            this.component = component;
            hidden = new HashSet<string>(memberNamesToHide);
        }

        AttributeCollection ICustomTypeDescriptor.GetAttributes()
        {
            return TypeDescriptor.GetAttributes(component);
        }

        string ICustomTypeDescriptor.GetClassName()
        {
            return TypeDescriptor.GetClassName(component);
        }

        string ICustomTypeDescriptor.GetComponentName()
        {
            return TypeDescriptor.GetComponentName(component);
        }

        TypeConverter ICustomTypeDescriptor.GetConverter()
        {
            return TypeDescriptor.GetConverter(component);
        }

        EventDescriptor ICustomTypeDescriptor.GetDefaultEvent()
        {
            EventDescriptor result = TypeDescriptor.GetDefaultEvent(component);
            return (result == null || hidden.Contains(result.Name)) ? null : result;

        }

        PropertyDescriptor ICustomTypeDescriptor.GetDefaultProperty()
        {
            PropertyDescriptor result = TypeDescriptor.GetDefaultProperty(component);
            return (result == null || hidden.Contains(result.Name)) ? null : result;
        }

        object ICustomTypeDescriptor.GetEditor(Type editorBaseType)
        {
            return TypeDescriptor.GetEditor(component, editorBaseType);
        }

        EventDescriptorCollection
        ICustomTypeDescriptor.GetEvents(Attribute[] attributes)
        {
            return GetEvents(); // don't filter on attribute; we're calling the shots...
}
        EventDescriptorCollection ICustomTypeDescriptor.GetEvents()
        {
            return GetEvents();
        }
        private EventDescriptorCollection GetEvents()
        {
            EventDescriptorCollection master = TypeDescriptor.GetEvents(component);
            var list = new List<EventDescriptor>(master.Count);
            foreach (EventDescriptor evt in master)
            {
                if (!hidden.Contains(evt.Name)) 
                    list.Add(evt);
            }
            return new EventDescriptorCollection(list.ToArray());
        }

        PropertyDescriptorCollection ICustomTypeDescriptor.GetProperties(Attribute[] attributes)
        {
            return GetProperties(); // don't filter on attribute; we're calling the shots...
        }

        PropertyDescriptorCollection ICustomTypeDescriptor.GetProperties()
        {
            return GetProperties();
        }
        private PropertyDescriptorCollection GetProperties()
        {
            PropertyDescriptorCollection master = TypeDescriptor.GetProperties(component);
            var list = new List<PropertyDescriptor>(master.Count);
            foreach (PropertyDescriptor prop in master)
            {
                if (!hidden.Contains(prop.Name)) 
                    list.Add(prop);
            }
            return new PropertyDescriptorCollection(list.ToArray());
        }

        public object GetPropertyOwner(PropertyDescriptor pd)
        {
            return component;
        }
    }
}
