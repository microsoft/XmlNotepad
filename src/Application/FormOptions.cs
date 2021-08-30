using System;
using System.Drawing;
using System.Collections;
using System.ComponentModel;
using System.Windows.Forms;
using System.Collections.Generic;
using SR = XmlNotepad.StringResources;

namespace XmlNotepad
{
	/// <summary>
	/// Summary description for FormOptions.
	/// </summary>
	public partial class FormOptions : System.Windows.Forms.Form
    {
        private Settings settings;
        UserSettings userSettings;

        private System.Windows.Forms.Button buttonOK;
        private System.Windows.Forms.Button buttonCancel;
        private PropertyGrid propertyGrid1;
        private Button buttonReset;
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

        private void OnButtonOKClick(object sender, System.EventArgs e) {
            this.userSettings.Apply();
            this.Close();
        }

        private void OnButtonResetClick(object sender, EventArgs e) {
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
    public class UserSettings
    {
        readonly Settings settings;
        Font font;
        readonly string fontName;
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
        string textEditor;

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
            this.textEditor = (string)this.settings["TextEditor"];
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
            this.settings["TextEditor"] = this.textEditor;

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
            newLineChars = Utilities.Escape("\r\n");
            language = "";
            this.maximumLineLength = 10000;
            this.maximumValueLength = short.MaxValue;
            ignoreDTD = false;
            this.allowAnalytics = false;
        }

        [SRCategory("ThemeCategory")]
        [LocDisplayName("Theme")]
        [SRDescription("ThemeDescription")]
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

        [SRCategory("ColorCategory")]
        [LocDisplayName("ElementColor")]
        [SRDescription("ElementColorDescription")]
        public Color ElementColor {
            get {
                return this.elementColor;
            }
            set {
                this.elementColor = value;
            }
        }

        [SRCategory("ColorCategory")]
        [LocDisplayName("AttributeColor")]
        [SRDescription("AttributeColorDescription")]
        public Color AttributeColor {
            get {
                return this.attributeColor;
            }
            set {
                this.attributeColor = value;
            }
        }

        [SRCategory("ColorCategory")]
        [LocDisplayName("CommentColor")]
        [SRDescription("CommentColorDescription")]
        public Color CommentColor {
            get {
                return this.commentColor;
            }
            set {
                this.commentColor = value;
            }
        }
        [SRCategory("ColorCategory")]
        [LocDisplayName("PiColor")]
        [SRDescription("PiColorDescription")]
        public Color PiColor {
            get {
                return this.piColor;
            }
            set {
                this.piColor = value;
            }
        }
        [SRCategory("ColorCategory")]
        [LocDisplayName("TextColor")]
        [SRDescription("TextColorDescription")]
        public Color TextColor {
            get {
                return this.textColor;
            }
            set {
                this.textColor = value;
            }
        }
        [SRCategory("ColorCategory")]
        [LocDisplayName("CDataColor")]
        [SRDescription("CDataColorDescription")]
        public Color CDataColor {
            get {
                return this.cdataColor;
            }
            set {
                this.cdataColor = value;
            }
        }
        [SRCategory("ColorCategory")]
        [LocDisplayName("BackgroundColor")]
        [SRDescription("BackgroundColorDescription")]
        public Color BackgroundColor {
            get {
                return this.backgroundColor;
            }
            set {
                this.backgroundColor = value;
            }
        }

        [SRCategory("ColorCategory")]
        [LocDisplayName("ContainerBackgroundColor")]
        [SRDescription("ContainerBackgroundColorDescription")]
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

        [SRCategory("ColorCategory")]
        [LocDisplayName("EditorBackgroundColor")]
        [SRDescription("EditorBackgroundColorDescription")]
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


        [SRCategory("FontCategory")]
        [LocDisplayName("FontPropertyName")]
        [SRDescription("FontDescription")]
        public Font Font {
            get {
                return this.font;
            }
            set {
                this.font = value;
            }
        }

        [SRCategory("LanguageCategory")]
        [LocDisplayName("LanguagePropertyName")]
        [SRDescription("LanguageDescription")]
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

        [SRCategory("AnalyticsCategory")]
        [LocDisplayName("AllowAnalytics")]
        [SRDescription("AllowAnalyticsDescription")]
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

        [SRCategory("UpdateCategory")]
        [LocDisplayName("EnableUpdate")]
        [SRDescription("EnableUpdateDescription")]
        public bool EnableUpdate {
            get {
                return this.enableUpdate;
            }
            set {
                this.enableUpdate = value;
            }
        }

        [SRCategory("UpdateCategory")]
        [LocDisplayName("UpdateLocation")]
        [SRDescription("UpdateLocationDescription")]
        public string UpdateLocation {
            get {
                return this.updateLocation;
            }
            set {
                this.updateLocation = value;
            }
        }

        [SRCategory("FormatCategory")]
        [LocDisplayName("AutoFormatOnSave")]
        [SRDescription("AutoFormatOnSaveDescription")]
        public bool AutoFormatOnSave {
            get {
                return this.autoFormatOnSave;
            }
            set {
                this.autoFormatOnSave = value;
            }
        }
        [SRCategory("FormatCategory")]
        [LocDisplayName("IndentLevel")]
        [SRDescription("IndentLevelDescription")]
        public int IndentLevel {
            get {
                return this.indentLevel;
            }
            set {
                this.indentLevel = value;
            }
        }
        [SRCategory("FormatCategory")]
        [LocDisplayName("IndentChar")]
        [SRDescription("IndentCharDescription")]
        public IndentChar IndentChar {
            get {
                return this.indentChar;
            }
            set {
                this.indentChar = value;
            }
        }

        [SRCategory("FormatCategory")]
        [LocDisplayName("NewLineChars")]
        [SRDescription("NewLineCharsDescription")]
        public string NewLineChars {
            get {
                return this.newLineChars;
            }
            set {
                this.newLineChars = value;
            }
        }

        [SRCategory("FormatCategory")]
        [LocDisplayName("NoByteOrderMark")]
        [SRDescription("NoByteOrderMarkDescription")]
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

        [SRCategory("LongLineCategory")]
        [LocDisplayName("MaximumLineLengthProperty")]
        [SRDescription("MaximumLineLengthDescription")]
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

        [SRCategory("LongLineCategory")]
        [LocDisplayName("MaximumValueLengthProperty")]
        [SRDescription("MaximumValueLengthDescription")]
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

        [SRCategory("LongLineCategory")]
        [LocDisplayName("AutoFormatLongLinesProperty")]
        [SRDescription("AutoFormatLongLinesDescription")]
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

        [SRCategory("Validation")]
        [LocDisplayName("IgnoreDTDProperty")]
        [SRDescription("IgnoreDTDDescription")]
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


        [SRCategory("XmlDiff")]
        [LocDisplayName("XmlDiffIgnoreChildOrderProperty")]
        [SRDescription("XmlDiffIgnoreChildOrderDescription")]
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
        [SRCategory("XmlDiff")]
        [LocDisplayName("XmlDiffIgnoreCommentsProperty")]
        [SRDescription("XmlDiffIgnoreCommentsDescription")]
        public bool XmlDiffIgnoreComments
        {
            get { return this.xmlDiffIgnoreComments; }
            set { this.xmlDiffIgnoreComments = value; }
        }
        [SRCategory("XmlDiff")]
        [LocDisplayName("XmlDiffIgnorePIProperty")]
        [SRDescription("XmlDiffIgnorePIDescription")]
        public bool XmlDiffIgnorePI
        {
            get { return this.xmlDiffIgnorePI; }
            set { this.xmlDiffIgnorePI = value; }
        }
        [SRCategory("XmlDiff")]
        [LocDisplayName("XmlDiffIgnoreWhitespaceProperty")]
        [SRDescription("XmlDiffIgnoreWhitespaceDescription")]
        public bool XmlDiffIgnoreWhitespace
        {
            get { return this.xmlDiffIgnoreWhitespace; }
            set { this.xmlDiffIgnoreWhitespace = value; }
        }
        [SRCategory("XmlDiff")]
        [LocDisplayName("XmlDiffIgnoreNamespacesProperty")]
        [SRDescription("XmlDiffIgnoreNamespacesDescription")]
        public bool XmlDiffIgnoreNamespaces
        {
            get { return this.xmlDiffIgnoreNamespaces; }
            set { this.xmlDiffIgnoreNamespaces = value; }
        }
        [SRCategory("XmlDiff")]
        [LocDisplayName("XmlDiffIgnorePrefixesProperty")]
        [SRDescription("XmlDiffIgnorePrefixesDescription")]
        public bool XmlDiffIgnorePrefixes
        {
            get { return this.xmlDiffIgnorePrefixes; }
            set { this.xmlDiffIgnorePrefixes = value; }
        }
        [SRCategory("XmlDiff")]
        [LocDisplayName("XmlDiffIgnoreXmlDeclProperty")]
        [SRDescription("XmlDiffIgnoreXmlDeclDescription")]
        public bool XmlDiffIgnoreXmlDecl
        {
            get { return this.xmlDiffIgnoreXmlDecl; }
            set { this.xmlDiffIgnoreXmlDecl = value; }
        }
        [SRCategory("XmlDiff")]
        [LocDisplayName("XmlDiffIgnoreDtdProperty")]
        [SRDescription("XmlDiffIgnoreDtdDescription")]
        public bool XmlDiffIgnoreDtd
        {
            get { return this.xmlDiffIgnoreDtd; }
            set { this.xmlDiffIgnoreDtd = value; }
        }

        [SRCategory("EditingCategory")]
        [LocDisplayName("TextEditorProperty")]
        [SRDescription("TextEditorDescription")]
        public string Editor
        {
            get
            {
                return this.textEditor;
            }
            set
            {
                if (this.textEditor != value)
                {
                    this.textEditor = value;
                }
            }
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
