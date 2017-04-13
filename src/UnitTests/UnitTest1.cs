using System;
using System.Text;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Windows.Forms;
using System.IO;
using System.Xml;
using System.Drawing;
using System.Diagnostics;
using System.Reflection;
using XmlNotepad;
using System.Text.RegularExpressions;

// Here's a handy reference on SendKeys:
// http://msdn2.microsoft.com/en-us/library/system.windows.forms.sendkeys.aspx

namespace UnitTests {
    
    [TestClass]
    public class UnitTest1 : TestBase {
        string TestDir;
                
        public UnitTest1() {
            Uri baseUri = new Uri(this.GetType().Assembly.Location);
            Uri resolved = new Uri(baseUri, "..\\..\\..\\");
            TestDir = resolved.LocalPath;
            // Test that we can process updates and show available updates button.
            // Have to fix the location field to show the right thing.
            XmlDocument doc = new XmlDocument();
            doc.Load(TestDir + @"UnitTests\TestUpdates.xml");
            XmlElement e = (XmlElement)doc.SelectSingleNode("updates/application/location");
            string target = TestDir + @"XmlNotepad\bin\Debug\Updates.xml";
            e.InnerText = target;
            doc.Save(target);
        }

        //[ClassInitialize()]
        //public static void MyClassInitialize(TestContext testContext) {
        //}
        
        //[ClassCleanup()]
        //public static void MyClassCleanup() {            
        //}
        
        [TestInitialize()]        
        public void MyTestInitialize() {
        }
        
        [TestCleanup()]
        public void MyTestCleanup() 
        {
            if (this.window != null) {
                this.window.Dispose();
            }
        }


        Window LaunchNotepad() {
            this.window  = LaunchNotepad(null);
            this.window.InvokeMenuItem("newToolStripMenuItem");
            Sleep(1000);
            return window;
        }

        Window LaunchNotepad(string filename) {
            this.window = LaunchApp(Directory.GetCurrentDirectory() + @"\..\..\..\drop\XmlNotepad.exe", filename, "FormMain");
            return window;
        }


        AutomationWrapper XmlTreeView {
            get {
                AutomationWrapper xtv = this.window.FindDescendant("xmlTreeView1");
                return xtv;
            }
        }

        AutomationWrapper TreeView {
            get {
                AutomationWrapper tv = this.window.FindDescendant("TreeView");
                return tv;
            }
        }


        AutomationWrapper NodeTextView {
            get {
                AutomationWrapper ntv = this.window.FindDescendant("NodeTextView");
                return ntv;
            }
        }

        AutomationWrapper NodeTextViewCompletionSet {
            get {
                AutomationWrapper cset = this.window.FindPopup("CompletionSet");               
                if (!cset.IsVisible) {
                    throw new Exception("CompletionSet is not visible");
                }
                return cset;
            }
        }
        
        [TestMethod]
        public void TestUndoRedo() {
            Trace.WriteLine("TestUndoRedo==========================================================");
            // Since this is the first test, we have to make sure we don't load some other user settings.
            string testFile = TestDir + "UnitTests\\test1.xml";
            var w = this.LaunchNotepad(testFile);

            // test that we can cancel editing when we click New
            Sleep(500); 
            w.SendKeystrokes("^IRoot{ENTER}");
            Sleep(100); 
            w.InvokeMenuItem("newToolStripMenuItem");
            Sleep(500);

            w.SetWindowSize(800, 600);

            Stack<bool> hasChildren = new Stack<bool>();
            List<NodeInfo> nodes = new List<NodeInfo>();
            string dir = Directory.GetCurrentDirectory();
            XmlReader reader = XmlReader.Create(testFile);
            bool openElement = true;
            int commands = 0;
            bool readyForText = false;
            IntPtr hwnd = w.Handle;

            using (reader) {
                while (reader.Read()) {
                    if (reader.NodeType == XmlNodeType.Whitespace || 
                        reader.NodeType == XmlNodeType.SignificantWhitespace ||
                        reader.NodeType == XmlNodeType.XmlDeclaration)
                        continue;

                    Trace.WriteLine(string.Format("Adding node type {0} with name {1} and value {2}",
                        reader.NodeType.ToString(), reader.Name, reader.Value));
                    
                    nodes.Add(new NodeInfo(reader));
                    bool children = false;
                    Trace.WriteLine(reader.NodeType + " " + reader.Name + "["+reader.Value+"]");
                    switch (reader.NodeType) {
                        case XmlNodeType.Element:
                            commands++;
                            w.InvokeMenuItem(openElement ? "elementChildToolStripMenuItem" :
                                "elementAfterToolStripMenuItem");
                            openElement = true;
                            bool isEmpty = reader.IsEmptyElement;
                            if (!isEmpty) {
                                hasChildren.Push(children);
                                children = false;
                            } else {
                                openElement = false;
                            }
                            string name = reader.Name;
                            w.SendKeystrokes(name);
                            Sleep(20);
                            w.SendKeystrokes("{ENTER}");

                            readyForText = true;
                            bool firstAttribute = true;
                            while (reader.MoveToNextAttribute()){
                                w.InvokeMenuItem(firstAttribute ? "attributeChildToolStripMenuItem" :
                                    "attributeAfterToolStripMenuItem");
                                firstAttribute = false;
                                readyForText = false;
                                openElement = false;
                                children = true;
                                w.SendKeystrokes(reader.Name + "{TAB}");
                                commands++;
                                w.SendKeystrokes(reader.Value);
                                Sleep(20);
                                w.SendKeystrokes("{ENTER}");
                                commands++;
                                Sleep(30);
                                w.SendKeystrokes("{LEFT}");
                            }
                            if (isEmpty) {
                                readyForText = false;
                                Sleep(50);
                                if (firstAttribute) w.SendKeystrokes("{ESC}"); // cancel value editing.
                                Sleep(30);
                                w.SendKeystrokes("{LEFT}");
                                Sleep(50);
                            }
                            break;
                        case XmlNodeType.Comment:
                            children = true;
                            w.InvokeMenuItem(openElement ? "commentChildToolStripMenuItem" : "commentAfterToolStripMenuItem");
                            commands++;
                            w.SendKeystrokes(reader.Value);
                            Sleep(20);
                            w.SendKeystrokes("{ENTER}");
                            commands++;
                            Sleep(30);
                            w.SendKeystrokes("{LEFT}");
                            openElement = false;
                            break;
                        case XmlNodeType.CDATA:
                            children = true;
                            w.InvokeMenuItem(openElement ? "cdataChildToolStripMenuItem" : "cdataAfterToolStripMenuItem");
                            commands++;
                            w.SendKeystrokes(reader.Value);
                            Sleep(20);
                            w.SendKeystrokes("{ENTER}");
                            commands++;
                            Sleep(30);
                            w.SendKeystrokes("{LEFT}");
                            openElement = false;
                            break;
                        case XmlNodeType.Text:
                            Sleep(30);
                            if (openElement) {
                                commands++;
                                if (!readyForText)
                                    w.SendKeystrokes("{TAB}{ENTER}");
                            } else {
                                children = true;
                                commands++;
                                w.InvokeMenuItem("textAfterToolStripMenuItem");
                                openElement = false;
                            }
                            w.SendKeystrokes(reader.Value);
                            Sleep(20);
                            w.SendKeystrokes("{ENTER}");
                            readyForText = false;
                            commands++;
                            Sleep(30);
                            w.SendKeystrokes("{LEFT}");
                            break;
                        case XmlNodeType.ProcessingInstruction:
                            children = true;
                            w.InvokeMenuItem(openElement ? "PIChildToolStripMenuItem" : "PIAfterToolStripMenuItem");
                            commands++;
                            w.SendKeystrokes(reader.Name + "{TAB}");
                            w.SendKeystrokes(reader.Value);
                            Sleep(20);
                            w.SendKeystrokes("{ENTER}");
                            commands++;
                            Sleep(30);
                            w.SendKeystrokes("{LEFT}");
                            openElement = false;
                            break;
                        case XmlNodeType.EndElement:
                            Sleep(50);
                            string keys = "";
                            if (readyForText) {
                                readyForText = false;
                                w.SendKeystrokes("{ESC}");
                                Sleep(200);
                                keys = "{LEFT}";
                            }
                            if (children)
                            {
                                keys = "{LEFT}";
                            }
                            children = hasChildren.Pop();
                            if (!openElement)
                            {
                                keys = "{LEFT}";
                            }
                            openElement = false;
                            w.SendKeystrokes(keys);
                            Sleep(200);
                            break;
                    }
                }
            }

            this.SaveAndCompare("out.xml", "test9.xml");

            // Test undo-redo
            UndoRedo(commands);

            this.SaveAndCompare("out.xml", "test9.xml");
        }

        [TestMethod]
        public void TestEditCombinations() {
            Trace.WriteLine("TestEditCombinations==========================================================");
            // Test all the combinations of insert before, after, child stuff!
            string testFile = TestDir + "UnitTests\\test1.xml";
            var w = this.LaunchNotepad(testFile);
            w.InvokeMenuItem("newToolStripMenuItem");
            Sleep(500);

            // each node type at root level
            string[] nodeTypes = new string[]{ "comment", "PI", "element", "attribute", "text", "cdata" };
            bool[] validInRoot = new bool[]{ true, true, true, false, false, false };
            bool[] requiresName = new bool[]{ false, true, true, true, false, false };
            string[] clips = new string[] { "<!--{1}-->", "<?{0} {1}?>", "<{0}>{1}</{0}>", "{0}=\"{1}\"", "{1}", "<![CDATA[{1}]]>" };
            nodeIndex = 0;

            for (int i = 0; i < nodeTypes.Length; i++){
                string type = nodeTypes[i];
                if (validInRoot[i]){
                    InsertNode(type, "Child", requiresName[i], clips[i]);
                    Undo();
                    Undo();
                }
            }

            w.InvokeMenuItem("commentChildToolStripMenuItem");

            for (int i = 0; i < nodeTypes.Length; i++) {
                string type = nodeTypes[i];
                if (validInRoot[i]) {
                    InsertNode(type, "After", requiresName[i], clips[i]);                                       
                    if (type != "element") {
                        InsertNode(type, "Before", requiresName[i], clips[i]);                    
                    }
                }
            }
            w.SendKeystrokes("^Ielement");
                
            // test all combinations of child elements under root element
            for (int i = 0; i < nodeTypes.Length; i++) {
                string type = nodeTypes[i];
                InsertNode(type, "Child", requiresName[i], clips[i]);
                InsertNode(type, "After", requiresName[i], clips[i]);
                InsertNode(type, "Before", requiresName[i], clips[i]);
                w.SendKeystrokes("{LEFT}{LEFT}"); // go back up to element.
            }
            this.SaveAndCompare("out.xml", "test7.xml");

        }

        int nodeIndex = 0;
        private void InsertNode(string type, string mode, bool requiresName, string clip) {
            string command = type + mode + "ToolStripMenuItem";
            Trace.WriteLine(command);
            this.window.InvokeMenuItem(command);
            string name = type + nodeIndex.ToString();
            if (requiresName) {
                this.window.SendKeystrokes(name);
                this.window.SendKeystrokes("{TAB}");
            }
            string value = mode;
            this.window.SendKeystrokes(value + "{ENTER}");
            clip = string.Format(clip, name, value);
            this.window.InvokeMenuItem("toolStripButtonCopy");
            CheckClipboard(clip);
            Clipboard.SetText("error");
            UndoRedo(2);
            this.window.InvokeMenuItem("toolStripButtonCopy");
            CheckClipboard(clip);

            nodeIndex++;            
        }

        /// <summary>
        /// Gets the value of whatever is selected by putting it in edit mode.
        /// This works on the tree view and the node text view depending on which
        /// has the focus.
        /// </summary>
        void CheckNodeValue(string expected) {
            CheckNodeValue(expected, StringComparison.Ordinal);
        }

        /// <summary>
        /// Gets the value of whatever is selected by putting it in edit mode.
        /// This works on the tree view and the node text view depending on which
        /// has the focus.
        /// </summary>
        void CheckNodeValue(string expected, StringComparison comparison)
        {
            // must not be a leaf node then...
            Sleep(100);
            SendKeys.SendWait("{ENTER}");
            Sleep(300);
            SendKeys.SendWait("^c");
            CheckClipboard(expected, comparison);
            Sleep(300);
            SendKeys.SendWait("{ENTER}");
            Sleep(300);
        }

        internal void CheckOuterXml(string expected) {
            this.window.SendKeystrokes("^c");
            CheckClipboard(expected);
        }

        [TestMethod]
        public void TestIntellisense() {
            Trace.WriteLine("TestIntellisense==========================================================");
            var w = LaunchNotepad();

            string outFile = TestDir + "UnitTests\\out.xml";

            Trace.WriteLine("Add <Basket>");
            w.InvokeMenuItem("elementChildToolStripMenuItem");
            w.SendKeystrokes("Basket{ENTER}");

            Save("out.xml");

            Trace.WriteLine("Add xmlns:xsi attribute");
            w.InvokeMenuItem("attributeChildToolStripMenuItem");
            w.SendKeystrokes("xmlns:xsi{TAB}");
            w.SendKeystrokes("http://www.w3.org/2001/XMLSchema-instance");

            Trace.WriteLine("Add xsi:noNamespaceSchemaLocation attribute");
            w.InvokeMenuItem("attributeAfterToolStripMenuItem");
            w.SendKeystrokes("xsi:no{TAB}");

            Trace.WriteLine("code coverage on Checker ReportError for non-existant schema");
            w.SendKeystrokes("foo.xsd{ENTER}");
            Sleep(500);
            Trace.WriteLine("Point to xsd2.xsd");
            w.SendKeystrokes("{ENTER}test2.xsd{ENTER}");

            Trace.WriteLine("Get intellisense tooltip");
            AutomationWrapper xtv = this.XmlTreeView;
            Sleep(2000); // wait for tooltip!
            Rectangle treeBounds = xtv.Bounds;
            Mouse.MouseMoveTo(Control.MousePosition, new Point(treeBounds.Left + 20, treeBounds.Top + 10), 5);
            Sleep(2000); // wait for tooltip!

            Trace.WriteLine("Add language='en-au'");
            w.InvokeMenuItem("attributeAfterToolStripMenuItem");
            w.SendKeystrokes("l{TAB}en-a{ENTER}");

            Trace.WriteLine("Add sold='true'");
            w.InvokeMenuItem("attributeAfterToolStripMenuItem");
            w.SendKeystrokes("s{TAB}t{ENTER}");

            Trace.WriteLine("Test validation error!");
            w.InvokeMenuItem("attributeAfterToolStripMenuItem");
            w.SendKeystrokes("tick{TAB}12345{ENTER}");
            Sleep(500); //just so I can see it

            Trace.WriteLine("Test we can rename attributes!");
            w.SendKeystrokes("+{TAB}{ENTER}");
            w.SendKeystrokes("t{ENTER}");

            Trace.WriteLine("make sure we can undo redo edit attribute name."); 
            UndoRedo(); 
            w.SendKeystrokes("{TAB}{ENTER}11:18:38:P{ENTER}");

            Trace.WriteLine("undo redo of edit attribute");
            UndoRedo();

            Trace.WriteLine("Add <color>Thistle</color>");
            w.InvokeMenuItem("elementAfterToolStripMenuItem");
            w.SendKeystrokes("c{TAB}ste{ENTER}");

            Trace.WriteLine("Check intellisense dropdown navigation.");
            CheckNodeValue("SteelBlue");

            Trace.WriteLine("Change Steeleblue to Yellow");
            w.SendKeystrokes("{ENTER}");

            w.SendKeystrokes("{PGDN}{END}{UP}{ENTER}");
            CheckNodeValue("Yellow");
            w.SendKeystrokes("{ENTER}{PGUP}{HOME}{DOWN}{DOWN}{ENTER}");
            Sleep(200);
            CheckNodeValue("Aqua");

            Trace.WriteLine("Click Color Picker button.");
            Sleep(500);
            w.SendKeystrokes("{ENTER}"); // activate drop down.
            Rectangle bounds = GetXmlBuilderBounds();
            Window popup = ClickXmlBuilder();            
            popup.SendKeystrokes("{DOWN}{LEFT} {ENTER}");

            Trace.WriteLine("test MouseDown on NodeTextView editor");
            Mouse.MouseClick(new Point(bounds.Left + 20, bounds.Top - 10), MouseButtons.Left);
            Sleep(500);
            w.SendKeystrokes("{DOWN}{ENTER}");

            Trace.WriteLine("Add <date>2005-12-25</date>");
            w.InvokeMenuItem("elementAfterToolStripMenuItem");
            w.SendKeystrokes("d{TAB}12/25/2005{ENTER}");

            Trace.WriteLine("Add <dateTime>2006-11-11T07:30:00</dateTime> ");
            w.InvokeMenuItem("elementAfterToolStripMenuItem");
            w.SendKeystrokes("d{TAB}11/11/2006{RIGHT}07:30:00:A{ENTER}");

            Trace.WriteLine("Add <photo>test1.xml</photo> ");
            w.InvokeMenuItem("elementAfterToolStripMenuItem");
            w.SendKeystrokes("p{TAB}basket.jpg");

            Trace.WriteLine("Test UriBuilder");
            popup = ClickXmlBuilder();
            popup.SendKeystrokes(TestDir + "UnitTests\\" + "test1.xml");
            popup.DismissPopUp("{ENTER}");

            w.SendKeystrokes("{ENTER}");
            CheckNodeValue("test1.xml", StringComparison.OrdinalIgnoreCase);
            // make test1.xml lower case (file system might make it upper case).
            w.SendKeystrokes("{ENTER}test1.xml{ENTER}");

            Trace.WriteLine("Add <fruit>banana</fruit> ");
            w.InvokeMenuItem("elementAfterToolStripMenuItem");
            w.SendKeystrokes("fr{TAB}b{ENTER}");

            Trace.WriteLine("Add <font>Arial, 8.25</font> ");
            w.InvokeMenuItem("elementAfterToolStripMenuItem");
            w.SendKeystrokes("fo{TAB}ar{ENTER}");
            Sleep(500);//just so I can see it

            Trace.WriteLine("Add Test FontBuilder");
            w.SendKeystrokes("{ENTER}");
            popup = ClickXmlBuilder();
            popup.DismissPopUp("{ENTER}{ENTER}");

            Trace.WriteLine("Add <vegetable>cucumber</vegetable> ");
            w.InvokeMenuItem("elementAfterToolStripMenuItem");
            w.SendKeystrokes("v{TAB}cu{ENTER}");

            Trace.WriteLine("Add <berry>huckleberry</berry> ");
            w.InvokeMenuItem("elementAfterToolStripMenuItem");
            w.SendKeystrokes("b{TAB}hu{ENTER}");

            Trace.WriteLine("Test edit of PI name");
            w.InvokeMenuItem("PIAfterToolStripMenuItem");
            Sleep(200);
            w.SendKeystrokes("test{ENTER}{ESC}{LEFT}");
            Sleep(200);
            w.SendKeystrokes("{ENTER}pi{ENTER}");
            Sleep(200);
            UndoRedo();

            Trace.WriteLine("Test validation error and elementBefore command!");
            w.InvokeMenuItem("elementBeforeToolStripMenuItem");
            w.SendKeystrokes("woops{ENTER}");
            Sleep(500);//just so I can see it

            Trace.WriteLine("Move to Basket element"); 
            w.SendKeystrokes("{ESC}");            
            Sleep(500);
            w.SendKeystrokes("{LEFT}{LEFT}");
            Sleep(1000);
            Trace.WriteLine("Navigate to next error");
            NavigateNextError();
            CheckNodeName("woops");
            Trace.WriteLine("Move to Basket element"); 
            w.SendKeystrokes("{LEFT}"); 

            Trace.WriteLine("Navigate error with mouse double click");
            NavigateErrorWithMouse();

            Trace.WriteLine("We are now back on the 'woops' element.");
            CheckNodeName("woops");            

            Trace.WriteLine("undo redo of elementBeforeToolStripMenuItem.");
            UndoRedo();

            Trace.WriteLine("Add <weight>1.234</weight> ");
            w.SendKeystrokes("{TAB}{ENTER}1.234{ENTER}");

            Trace.WriteLine("Test we can fix it by renaming element");
            w.SendKeystrokes("+{TAB}{ENTER}w{TAB}");

            Trace.WriteLine("undo redo of edit element name");
            UndoRedo();

            this.SaveAndCompare("out.xml", "test2.xml");
        }

        private void NavigateErrorWithMouse() {
            AutomationWrapper grid = this.window.FindDescendant("DataGridView");
            AutomationWrapper row = grid.FirstChild;
            row = row.NextSibling;
            Point pt = row.Bounds.Center();
            // Double click it
            Mouse.MouseDoubleClick(pt, MouseButtons.Left);
        }

        private void NavigateNextError() {
            this.window.InvokeMenuItem("nextErrorToolStripMenuItem");
        }

        private void Undo(int count) {
            while (count-- > 0) {
                Undo();
            }
        }

        private void Redo(int count) {
            while (count-- > 0) {
                Redo();
            }
        }

        private void Undo() {
            this.window.InvokeMenuItem("undoToolStripMenuItem");
        }
        private void Redo() {
            this.window.InvokeMenuItem("redoToolStripMenuItem");
        }
        private void UndoRedo(int level) {
            for (int i = 0; i < level; i++) {
                Undo();
            }
            for (int i = 0; i < level; i++) {
                Redo();
            }
        }
        private void UndoRedo() {
            Undo();
            Redo();
        }

        Rectangle GetXmlBuilderBounds()
        {
            return NodeTextViewCompletionSet.Bounds; 
        }

        Window ClickXmlBuilder() {
            // Find the intellisense button and click on it
            Rectangle bounds = NodeTextViewCompletionSet.Bounds;
            Sleep(1000);
            Mouse.MouseClick(new Point(bounds.Left + 15, bounds.Top + 10), MouseButtons.Left);
            return this.window.WaitForPopup(NodeTextViewCompletionSet.Hwnd);
        }


        [TestMethod]
        public void TestCompare() {
            Trace.WriteLine("TestCompare==========================================================");
            string testFile = TestDir + "UnitTests\\test4.xml";
            var w = LaunchNotepad(testFile);

            // something the same
            w.InvokeAsyncMenuItem("compareXMLFilesToolStripMenuItem");
            Window openDialog = w.WaitForPopup();
            openDialog.SendKeystrokes(TestDir + "UnitTests\\test4.xml{ENTER}");
            Window msgBox = w.WaitForPopup();
            string text = msgBox.GetWindowText();
            Assert.AreEqual<string>(text, "Files Identical");
            msgBox.SendKeystrokes("{ENTER}");

            // Now something different
            w.InvokeAsyncMenuItem("compareXMLFilesToolStripMenuItem");

            openDialog = w.WaitForPopup();            
            openDialog.SendKeystrokes(TestDir + "UnitTests\\test5.xml{ENTER}");
            Window browser = w.WaitForPopup(openDialog.Handle);
            text = browser.GetWindowText();
            browser.DismissPopUp("%{F4}");

            Undo();
        }        

        [TestMethod]
        public void TestClipboard() {
            Trace.WriteLine("TestClipboard==========================================================");

            string testFile = TestDir + "UnitTests\\test1.xml";
            var w = LaunchNotepad(testFile);

            Trace.WriteLine("Incremental find 'Emp'");
            w.InvokeMenuItem("incrementalSearchToolStripMenuItem");
            System.Windows.Forms.SendKeys.SendWait("Emp");
            //w.SendKeystrokes("Emp");

            Trace.WriteLine("cut");
            w.SendKeystrokes("^x");
            
            string expected = "<Employee xmlns=\"http://www.hr.org\" id=\"46613\" title=\"Architect\"><Name First=\"Chris\" Last=\"Lovett\" /><Street>One Microsoft Way</Street><City>Redmond</City><Zip>98052</Zip><Country><Name>U.S.A.</Name></Country><Office></Office></Employee>";
            CheckClipboard(expected);

            Trace.WriteLine("paste something different");
            string expected2 = "<Employee xmlns=\"http://www.hr.org\" id=\"46613\" title=\"Architect\"><Name>Test</Name><?pi test?></Employee>";
            w.SendKeystrokes("{LEFT}");
            Clipboard.SetText(expected2);
            w.SendKeystrokes("^v");
            Sleep(500);
            CheckOuterXml(expected2);

            Trace.WriteLine("test undo and redo of cut/paste commands");
            UndoRedo(2);
            Undo();
            Undo();

            Sleep(500);
            CheckOuterXml(expected);

            Trace.WriteLine("test delete key");
            w.InvokeMenuItem("deleteToolStripMenuItem");
            CheckOuterXml("<!--last comment-->");
            UndoRedo();
            Undo();

            Trace.WriteLine("test copy paste via menus");
            w.SendKeystrokes("{END}"); // now on #comment
            Clipboard.SetText("error");
            w.InvokeMenuItem("copyToolStripMenuItem");
            CheckClipboard("<!--last comment-->");
            Clipboard.SetText("error");
            w.InvokeMenuItem("cutToolStripMenuItem");
            CheckClipboard("<!--last comment-->");
            w.InvokeMenuItem("pasteToolStripMenuItem");
            Undo();
            Undo();

            Trace.WriteLine("Test repeat");
            w.InvokeMenuItem("repeatToolStripMenuItem");
            Sleep(1000);
            w.SendKeystrokes("new comment{ENTER}");
            UndoRedo(2); // test redo of duplicate!
            Undo();
            Undo();

            Trace.WriteLine("test cut/copy/paste/delete in NodeTextView");
            this.NodeTextView.SetFocus();
            w.SendKeystrokes("{DEL}");
            Sleep(200);
            w.SendKeystrokes("^z");
            Sleep(200);
            CheckNodeValue("last comment");
            CheckOuterXml("<!--last comment-->");
            w.SendKeystrokes("^x");
            CheckClipboard("<!--last comment-->");
            Undo();
            Clipboard.SetText("<!--last comment-->");
            w.SendKeystrokes("^v");
            CheckOuterXml("<!--last comment-->");
            Undo();

            this.NodeTextView.SetFocus();
            Sleep(1000);
            Trace.WriteLine("type to find in 'foo' in node text view");
            w.SendKeystrokes("^Ifoo");
            CheckNodeValue("foo");

            this.TreeView.SetFocus();
            Trace.WriteLine("DuplicateNode");
            w.SendKeystrokes("^IEmp");
            CheckNodeValue("Employee");
            w.InvokeMenuItem("duplicateToolStripMenuItem");

            Trace.WriteLine("undo/redo of duplicate");
            UndoRedo();
            Undo();
            CheckNodeValue("Employee");

            Trace.WriteLine("Test namespace aware copy/paste");
            string xml = "<x:item xmlns:x='uri:1'>Some text</x:item>";
            Clipboard.SetText(xml);
            w.InvokeMenuItem("pasteToolStripMenuItem");
            Sleep(500);

            Trace.WriteLine("Test namespace normalization on paste.");
            w.InvokeMenuItem("pasteToolStripMenuItem");

            Trace.WriteLine("Test namespace prefix auto-generation.");
            Sleep(500); 
            w.SendKeystrokes("{DOWN}"); // reset type-to-find
            w.SendKeystrokes("^Iid");
            w.SendKeystrokes("{ENTER}");
            w.SendKeystrokes("{HOME}y:{ENTER}");
            Sleep(200); 
            w.SendKeystrokes("{DOWN}"); // reset type-to-find
            w.SendKeystrokes("^Iitem");
            w.SendKeystrokes("{ENTER}{HOME}z:{ENTER}");
            Sleep(200);
            
            // test save to same file.
            this.SaveAndCompare("out.xml", "test6.xml");

        }

        void WipeFile(string fname) {
            if (File.Exists(fname)) {
                File.SetAttributes(fname, File.GetAttributes(fname) & ~FileAttributes.ReadOnly);
                File.Delete(fname);
            }
        }


        [TestMethod]
        public void TestOptionsDialog() {
            Trace.WriteLine("TestOptionsDialog==========================================================");
            
            // Save original settings.
            string originalSettings = Environment.GetEnvironmentVariable("USERPROFILE") + "\\Local Settings\\Application Data\\Microsoft\\Xml Notepad\\XmlNotepad.settings";
            string backupSettings = Path.GetTempPath() + "XmlNotepad.settings";
            File.Copy(originalSettings, backupSettings, true);

            var w = LaunchNotepad();

            // Options dialog
            Trace.WriteLine("Options dialog...");
            Window options = w.OpenDialog("optionsToolStripMenuItem", "FormOptions");

            // Find the PropertyGrid control.
            AutomationWrapper acc = options.FindDescendant("propertyGrid1");

            AutomationWrapper table = acc.FindChild("Properties Window");

            Trace.WriteLine("Font");
            AutomationWrapper font = table.FindChild("Font"); // this is the group heading
            Rectangle r = font.Bounds;
            // bring up the font dialog.
            Mouse.MouseClick(new Point(r.Right - 10, r.Top + 6), MouseButtons.Left);
            Sleep(1000);
            Mouse.MouseClick(new Point(r.Right - 10, r.Top + 6), MouseButtons.Left);
            Window popup = options.WaitForPopup();
            popup.DismissPopUp("{ENTER}");

            string[] names = new string[] { "Element", "Attribute", "Text",
                    "Background", "Comment", "PI", "CDATA" };

            string[] values = new string[] { "Aqua", "128, 64, 64", "64, 0, 0",
                  "64, 0, 128", "Lime", "128, 0, 64", "0, 64, 64"};

            
            for (int i = 0, n = names.Length; i<n ;i++) {
                string name = names[i];

                Trace.WriteLine("Click " + name);

                AutomationWrapper child = table.FindChild(name);
                r = child.Bounds;
                Mouse.MouseClick(new Point(r.Left + 10, r.Top + 6), MouseButtons.Left);
                Sleep(100);
                popup.SendKeystrokes("{TAB}" + values[i] + "{ENTER}");

                Sleep(333); // so we can see it!
            }

            popup.SendKeystrokes("%O");            
            bool passed = true;

            // Close the app.
            w.Dispose();
            Sleep(1000); // give it time to write out the new settings.

            // verify persisted colors.
            if (File.Exists(originalSettings)) {
                XmlDocument doc = new XmlDocument();
                doc.Load(originalSettings);
                for (int i = 0, n = names.Length; i<n; i++){
                    string ename = names[i];
                    XmlNode node = doc.SelectSingleNode("Settings/Colors/" + ename);
                    if (node != null) {
                        string expected = values[i];
                        string actual = node.InnerText;
                        if (expected != actual) {
                            Trace.WriteLine(string.Format("Color '{0}' has unexpected value '{1}'", ename, actual));
                            passed = false;
                        }
                    }
                }
            }

            // restore the original settings.
            File.Copy(backupSettings, originalSettings, true);
            DeleteFile(backupSettings);

            if (!passed) {
                throw new ApplicationException("Unexpected colors found in XmlNotepad.settings file.");
            }
            
        }

        [TestMethod]
        public void TestDialogs() {

            // ensure we get this warning dialog.
            ResetFormatLongLines();

            // ensure we get a horizontal scroll bar on the supply.xml file.
            SetTreeViewWidth(290);

            Trace.WriteLine("TestDialogs==========================================================");
            var w = LaunchNotepad();

            // About...
            Trace.WriteLine("About...");
            w.InvokeAsyncMenuItem("aboutXMLNotepadToolStripMenuItem");
            Window popup = w.WaitForPopup();
            popup.DismissPopUp("{ENTER}");

            // hide/show status bar
            Trace.WriteLine("hide/show status bar...");
            w.InvokeAsyncMenuItem("statusBarToolStripMenuItem");
            Sleep(500);
            w.InvokeAsyncMenuItem("statusBarToolStripMenuItem");
            Sleep(500);

            // open bad file.            
            Trace.WriteLine("open bad file");
            w.InvokeAsyncMenuItem("openToolStripMenuItem");
            popup = w.WaitForPopup();
            popup.SendKeystrokes(TestDir + "UnitTests\\bad.xml{ENTER}");
            popup = w.WaitForPopup();
            popup.SendKeystrokes("%Y");
            popup = w.WaitForPopup();
            popup.DismissPopUp("%{F4}");

            // Test OpenFileDialog
            Trace.WriteLine("OpenFileDialog");
            w.InvokeAsyncMenuItem("openToolStripMenuItem");
            popup = w.WaitForPopup();
            popup.SendKeystrokes(TestDir + "UnitTests\\supply.xml");
            popup.DismissPopUp("{ENTER}");

            Trace.WriteLine("Test long line wrap message.");
            w.InvokeAsyncMenuItem("findToolStripMenuItem");
            popup = w.WaitForPopup();
            FindDialog fd = new FindDialog(popup);
            fd.UseRegex = false;
            fd.UseXPath = false;
            fd.UseWholeWord = false;
            fd.FindString = "FinalDeliverable";
            popup.SendKeystrokes("{ENTER}");
            popup.DismissPopUp("{ESC}");

            Trace.WriteLine("Test horizontal scroll bar");
            AutomationWrapper hscroll = XmlTreeView.FindChild("HScrollBar");
            Rectangle sbBounds = hscroll.Bounds;
            Sleep(1000);
            Mouse.MouseClick(new Point(sbBounds.Left + 5, sbBounds.Top + 5), MouseButtons.Left);
            Sleep(500);

            this.TreeView.SetFocus();
            w.SendKeystrokes("{TAB}{ENTER}");
            popup = w.WaitForPopup();
            popup.DismissPopUp("{ENTER}");
            w.SendKeystrokes("{ENTER}");
                        
            Trace.WriteLine("View source");
            w.InvokeAsyncMenuItem("sourceToolStripMenuItem");
            popup = w.WaitForPopup(); // file has changed, do you want to save it?
            popup.SendKeystrokes("%N");
            popup = w.WaitForPopup(); // wait for Notepad.exe.
            popup.DismissPopUp("%{F4}");
            
            // Show help
            Trace.WriteLine("Show help...");
            Trace.WriteLine(Directory.GetCurrentDirectory());
            Trace.WriteLine(Application.StartupPath);
            w.SendKeystrokes("{F1}");
            popup = w.WaitForPopup();
            popup.DismissPopUp("%{F4}");
            
            // Test reload - discard changes
            Trace.WriteLine("Reload- discard changes");
            w.InvokeAsyncMenuItem("reloadToolStripMenuItem");
            popup = w.WaitForPopup();
            popup.DismissPopUp("{ENTER}");

            // Save As...
            Trace.WriteLine("Save As..."); 
            string outFile = TestDir + "UnitTests\\out.xml";
            WipeFile(outFile);
            w.InvokeAsyncMenuItem("saveAsToolStripMenuItem");
            popup = w.WaitForPopup();
            popup.SendKeystrokes("out.xml");
            popup.DismissPopUp("{ENTER}");

            // Check save read only
            Trace.WriteLine("Check save read only.");
            File.SetAttributes(outFile, File.GetAttributes(outFile) | FileAttributes.ReadOnly);
            w.InvokeAsyncMenuItem("saveToolStripMenuItem");
            popup = w.WaitForPopup();
            popup.DismissPopUp("%Y");
            Sleep(2000); // let file system settle down...
            
            // Test "reload" message box.
            Trace.WriteLine("File has changed on disk, do you want to reload?");
            File.SetLastWriteTime(outFile, DateTime.Now);
            Sleep(2000); // now takes 2 seconds for this to show up.

            popup = w.WaitForPopup();
            popup.DismissPopUp("%Y"); // reload!
            
            // Window/NewWindow!
            Trace.WriteLine("Window/NewWindow");
            w.InvokeAsyncMenuItem("newWindowToolStripMenuItem");
            popup = w.WaitForPopup();
            popup.DismissPopUp("%{F4}"); // close second window!
            
            if (!Window.GetForegroundWindowText().StartsWith("XML Notepad")) {
                w.Activate(); // alt-f4 sometimes sends focus to another window (namely, the VS process running this test!)
                Sleep(500);
            }
            Sleep(1000);
           
            // Test SaveIfDirty
            Trace.WriteLine("make simple edit");
            FocusTreeView();
            w.SendKeystrokes("{END}{DELETE}");// make simple edit
           
            Trace.WriteLine("Test error dialog when user tries to enter element with no name");
            w.InvokeMenuItem("repeatToolStripMenuItem");
            w.SendKeystrokes("{ENTER}");
            popup = w.WaitForPopup();
            popup.DismissPopUp("%N");
            
            Trace.WriteLine("Test error dialog when user tries to enter name with spaces");
            w.SendKeystrokes("     {ENTER}");
            popup = w.WaitForPopup();
            Trace.WriteLine("This time accept the empty name");
            popup.DismissPopUp("%Y");

            Trace.WriteLine("Test error dialog when user enter an invalid name");
            w.SendKeystrokes("{ENTER}{+}{+}{+}{ENTER}");
            popup = w.WaitForPopup();
            popup.DismissPopUp("{ENTER}");
            w.SendKeystrokes("{ESC}");
            Undo();
            
            // Save changes on exit?
            Trace.WriteLine("Save changes on exit - cancel");
            w.InvokeAsyncMenuItem("exitToolStripMenuItem");
            popup = w.WaitForPopup();
            popup.DismissPopUp("{ESC}"); // make sure we can cancel exit!
            Sleep(1000);

            // Save changes on 'new'?
            Trace.WriteLine("Save changes on 'new' - cancel");
            w.InvokeAsyncMenuItem("newToolStripMenuItem");
            popup = w.WaitForPopup();
            popup.DismissPopUp("{ESC}"); // make sure we can cancel 'new'!
            Sleep(1000);

            Trace.WriteLine("Save changes on 'exit' - yes!");
            CheckNodeName("Header");
            w.InvokeAsyncMenuItem("exitToolStripMenuItem");
            popup = w.WaitForPopup();

            // save the changes!
            popup.SendKeystrokes("%Y");

        }

        [TestMethod]
        public void TestSchemaDialog() {
            Trace.WriteLine("TestSchemaDialog==========================================================");
            var w = LaunchNotepad();

            Sleep(1000);
            Trace.WriteLine("Open Schema Dialog");
            Window schemaDialog = w.OpenDialog("schemasToolStripMenuItem", "FormSchemas");
            schemaDialog.InvokeMenuItem("clearToolStripMenuItem");

            Trace.WriteLine("Add schema via file dialog");
            var button = schemaDialog.FindDescendant("Browse Row 0");
            button.Invoke(); // bring up file open dialog
            Sleep(1000);

            Window fileDialog = schemaDialog.WaitForPopup();
            OpenFileDialog fd = new OpenFileDialog(fileDialog);
            string schema = TestDir + "UnitTests\\emp.xsd";
            fd.FileName = schema;
            fd.DismissPopUp("{ENTER}");

            schemaDialog.SendKeystrokes("^{HOME}+ "); // select first row
            Sleep(300); // just so we can watch it happen
            schemaDialog.SendKeystrokes("^c"); // copy
            string text = Clipboard.GetText();
            if (!text.ToLowerInvariant().Contains("emp.xsd")) {
                throw new ApplicationException("Did not find 'test2.xsd' on the clipboard!");
            }
            Trace.WriteLine("Close schema dialog");
            schemaDialog.DismissPopUp("%O"); // hot key for OK button.

            Trace.WriteLine("Close Microsoft XML Notepad and reload it to ensure schema cache was persisted");
            CloseApp();
            w = LaunchNotepad();

            Sleep(1000);
            schemaDialog = w.OpenDialog("schemasToolStripMenuItem", "FormSchemas");
            Sleep(500);
            w.SendKeystrokes("^{HOME}+ "); // select first row

            Trace.WriteLine("Cut");
            schemaDialog.SendKeystrokes("^x"); // cut
            text = Clipboard.GetText();
            if (!text.ToLowerInvariant().Contains("emp.xsd")) {
                throw new ApplicationException("Did not find 'test2.xsd' on the clipboard!");
            }
            Sleep(300);
            Trace.WriteLine("Paste");
            schemaDialog.SendKeystrokes("^v"); // paste
            Sleep(300);

            Trace.WriteLine("Edit of filename cell.");
            schemaDialog.SendKeystrokes("^{HOME}{RIGHT}{RIGHT}" + schema + "{ENTER}");
            Trace.WriteLine("Undo");
            schemaDialog.SendKeystrokes("^z"); // undo
            Sleep(300);
            Trace.WriteLine("Redo");
            schemaDialog.SendKeystrokes("^y"); // redo            
            Sleep(300);
            Trace.WriteLine("Delete");
            schemaDialog.SendKeystrokes("^{HOME}+ {DELETE}"); // delete first row
            Sleep(300);
            Trace.WriteLine("Undo");
            schemaDialog.SendKeystrokes("^z"); // undo
            Sleep(300);
            Trace.WriteLine("Redo");
            schemaDialog.SendKeystrokes("^y"); // redo
            Sleep(300);
            Trace.WriteLine("Undo");
            schemaDialog.SendKeystrokes("^z"); // undo
            Sleep(300);

            Trace.WriteLine("Make sure we commit with some rows to update schema cache!");
            schemaDialog.DismissPopUp("%O"); // hot key for OK button.

            Trace.WriteLine("Now add a duplicate target namespcace.");
            schemaDialog = w.OpenDialog("schemasToolStripMenuItem", "FormSchemas");
            Sleep(500);

            Trace.WriteLine("Add emp2.xsd via paste");
            schema = TestDir + "UnitTests\\emp2.xsd";
            schemaDialog.SendKeystrokes("{DOWN}{RIGHT}{RIGHT}^ "); // select first row
            Clipboard.SetText(schema);
            schemaDialog.SendKeystrokes("^v");
            schemaDialog.SendKeystrokes("^c"); // copy
            text = Clipboard.GetText();
            if (!text.ToLowerInvariant().Contains("emp2.xsd")) {
                throw new ApplicationException("Did not find 'test2.xsd' on the clipboard!");
            }

            Trace.WriteLine("Add duplicate schema via file dialog ");
            Sleep(1000);
            schemaDialog.InvokeAsyncMenuItem("addSchemasToolStripMenuItem");
            
            fileDialog = schemaDialog.WaitForPopup();
            fd = new OpenFileDialog(fileDialog);
            schema = TestDir + "UnitTests\\emp.xsd";
            fd.FileName = schema;
            fd.DismissPopUp("{ENTER}");
            
            Sleep(300); // just so we can watch it happen
            schemaDialog.SendKeystrokes("^c"); // copy first row
            text = Clipboard.GetText();
            if (!text.ToLowerInvariant().Contains("emp.xsd")) {
                throw new ApplicationException("Did not find 'emp.xsd' on the clipboard!");
            }
            Trace.WriteLine("Make sure we commit with the duplicate tns.");
            schemaDialog.SendKeystrokes("%O"); // hot key for OK button.

            Trace.WriteLine("Clear the schemas.");
            schemaDialog = w.OpenDialog("schemasToolStripMenuItem", "FormSchemas");
            schemaDialog.InvokeMenuItem("clearToolStripMenuItem");
            // Make sure we commit with the duplicate tns.
            schemaDialog.SendKeystrokes("%O"); // hot key for OK button.

        }
        public FindDialog OpenFindDialog()
        {
            this.window.InvokeAsyncMenuItem("findToolStripMenuItem");
            Window fd = this.window.WaitForPopup();
            return new FindDialog(fd);
        }

        public FindDialog OpenReplaceDialog()
        {
            this.window.InvokeAsyncMenuItem("replaceToolStripMenuItem");
            Window fd = this.window.WaitForPopup();
            return new FindDialog(fd);
        }

        [TestMethod]
        public void TestXPathFind() {
            Trace.WriteLine("TestXPathFind==========================================================");
            // Give view source something to show.
            string testFile = TestDir + "UnitTests\\test1.xml";
            var w = LaunchNotepad(testFile);
                       
            Sleep(1000);

            Trace.WriteLine("test path of 'pi' node");
            w.SendKeystrokes("^Ipi");

            FindDialog fd = OpenFindDialog();
            fd.UseXPath = true;

            AssertNormalizedEqual(fd.FindString, "/processing-instruction('pi')"); // test pi
            fd.Window.DismissPopUp("{ESC}");

            Trace.WriteLine("test path of comment");
            this.TreeView.SetFocus();
            w.SendKeystrokes("{DOWN}^I#");
            fd = OpenFindDialog();
            AssertNormalizedEqual(fd.FindString, "/Root/comment()[1]");
            fd.Window.DismissPopUp("{ESC}");

            Trace.WriteLine("test path of cdata");
            this.TreeView.SetFocus();
            w.SendKeystrokes("{ESC}{DOWN}");
            fd = OpenFindDialog();
            AssertNormalizedEqual(fd.FindString, "/Root/text()");
            fd.Window.DismissPopUp("{ESC}");

            Trace.WriteLine("test path of text node");
            this.TreeView.SetFocus();
            w.SendKeystrokes("{DOWN}{RIGHT}{DOWN}");
            fd = OpenFindDialog();
            AssertNormalizedEqual(fd.FindString, "/Root/item/text()");
            fd.Window.DismissPopUp("{ESC}");

            Trace.WriteLine("test path of node with namespace");
            this.TreeView.SetFocus();
            w.SendKeystrokes("{ESC}{DOWN}^IEmp");
            fd = OpenFindDialog();
            AssertNormalizedEqual(fd.FindString, "/Root/a:Employee"); // test element with namespaces!
            
            Trace.WriteLine("test edit path and find node.");
            fd.Window.SendKeystrokes("/Root{ENTER}");
            fd.Window.DismissPopUp("{ESC}");

            Trace.WriteLine("test 'id' attribute path generation.");
            this.TreeView.SetFocus();
            w.SendKeystrokes("{ESC}{DOWN}");
            fd = OpenFindDialog();
            AssertNormalizedEqual(fd.FindString, "/Root/@id");
            fd.Window.DismissPopUp("{ESC}");

            Trace.WriteLine("Find on an xmlns attributue!");
            this.TreeView.SetFocus();
            w.SendKeystrokes("^IEmp{RIGHT}{DOWN}");
            fd = OpenFindDialog();
            AssertNormalizedEqual(fd.FindString, "/Root/a:Employee/namespace::*[local-name()='']");
            fd.Window.DismissPopUp("{ESC}");

            // XmlDocument lazily creates namespace nodes causing this test to "modify" the document!
            Save("out.xml");
        }

        [TestMethod]
        public void TestXsltOutput() {
            Trace.WriteLine("TestXsltOutput==========================================================");

            var w = LaunchNotepad();

            Trace.WriteLine("Click in the combo box location field");
            AutomationWrapper comboBoxLocation = w.FindDescendant("comboBoxLocation");
            Rectangle bounds = comboBoxLocation.Bounds;
            Mouse.MouseClick(bounds.Center(), MouseButtons.Left);

            Trace.WriteLine("Load RSS from http");
            w.SendKeystrokes("{END}+{HOME}http://www.bing.com/news?format=RSS{ENTER}");

            Trace.WriteLine("Wait for rss to be loaded");
            WaitForText("<?xml version=\"1.0\" encoding=\"utf-8\"?>");

            //w.SendWait("{DOWN}");
            //this.CheckOuterXml("<?xml-stylesheet type='text/xsl' href='rsspretty.xsl' version='1.0'?>");

            Trace.WriteLine("Show XSLT");
            AutomationWrapper tab = w.FindDescendant("tabPageHtmlView");
            bounds = tab.Bounds;
            Trace.WriteLine("Select tabPageHtmlView ");
            Mouse.MouseClick(new Point(bounds.Left + 20 + 70, bounds.Top - 15), MouseButtons.Left);
            Sleep(1000);

            Trace.WriteLine("Enter custom XSL with script code.");
            this.EnterXslFilename(TestDir + "UnitTests\\rss.xsl");
            Window popup = w.WaitForPopup();
            string title = Window.GetForegroundWindowText();
            if (title != "Untrusted Script Code") {
                throw new ApplicationException("Expecting script security dialog");
            }
            Sleep(1000);
            popup.DismissPopUp("%Y");

            Trace.WriteLine("Make sure it executed");
            CopyHtml();

            this.CheckClipboard(new Regex(@"Found [\d]* RSS items. The script executed successfully."));

            Trace.WriteLine("Try xslt with error");
            this.EnterXslFilename(TestDir + "UnitTests\\bad.xsl");
            Sleep(2000);            
            CopyHtml();
            this.CheckClipboard(@"Error Transforming XML 
Prefix 'user' is not defined. ");

            Trace.WriteLine("Back to tree view");
            tab = w.FindDescendant("tabPageTreeView");
            Mouse.MouseClick(new Point(bounds.Left + 20, bounds.Top - 15), MouseButtons.Left);

            Sleep(1000);
            Save("out.xml");
        }

        void WaitForText(string value) {            
            int retries = 20;
            string clip = null;
            while (retries-- > 0) {
                this.window.SendKeystrokes("^c");
                clip = Clipboard.GetText();
                Trace.WriteLine("clip=" + clip);
                if (clip == value)
                    return;
                Sleep(2000);
            }
            throw new Exception("Not finding expected text '" + value + "', instead we got '" + clip + "'");
        }

        void EnterXslFilename(string filename) {
            AutomationWrapper s = this.window.FindDescendant("SourceFileName");
            Rectangle bounds = s.Bounds;
            Mouse.MouseClick(bounds.Center(), MouseButtons.Left);
            Sleep(500);
            this.window.SendKeystrokes("{END}+{HOME}" + filename + "{ENTER}");            
        }

        string CopyHtml() {
            AutomationWrapper xsltViewer = this.window.FindDescendant("xsltViewer");
            Rectangle bounds = xsltViewer.Bounds;
            // click in HTML view
            Mouse.MouseClick(bounds.Center(), MouseButtons.Left);

            // select all the text
            Sleep(1000);
            this.window.SendKeystrokes("^a");
            
            Sleep(1000);
            this.window.SendKeystrokes("^c");
            return Clipboard.GetText();
        }


        [TestMethod]
        public void TestFindReplace() {

            ResetFindOptions();

            Trace.WriteLine("TestFindReplace==========================================================");
            // Give view source something to show.
            string testFile = TestDir + "UnitTests\\test1.xml";
            var w = LaunchNotepad(testFile);

            Trace.WriteLine("Test auto-move of Find Window to reveal what was found");
            Rectangle treeBounds = this.XmlTreeView.Bounds;
            
            var findDialog = OpenFindDialog();
            findDialog.ClearFindCheckBoxes();

            Rectangle findBounds = findDialog.Window.GetScreenBounds();
            Point treeCenter = treeBounds.Center();
            Point findCenter = findBounds.Center();
            Point start = new Point(findBounds.Left + (findBounds.Width / 2), findBounds.Top + 15);
            Point end = new Point(start.X + treeCenter.X - findCenter.X, 
                                  start.Y + treeCenter.Y - findCenter.Y);
            Mouse.MouseClick(start, MouseButtons.Left);
            Mouse.MouseDragTo(start, end, 5, MouseButtons.Left);
            Mouse.MouseUp(end, MouseButtons.Left);

            // Refocus the combo box...
            Sleep(500);
            findDialog.FocusFindString();
            
            Sleep(500);
            findDialog.Window.SendKeystrokes("Some{ENTER}");
            Sleep(500);

            findDialog.Window.DismissPopUp("{ESC}");
            w.SendKeystrokes("^c{ESC}");
            CheckClipboard("Some");
            Sleep(200);
            w.SendKeystrokes("^{HOME}");
            
            Trace.WriteLine("Test find error dialog");
            findDialog = OpenFindDialog();
            findDialog.Window.SendKeystrokes("will not find{ENTER}");
            Window popup = w.ExpectingPopup("Find Error");
            popup.DismissPopUp("{ENTER}");

            Sleep(200);
            Trace.WriteLine("test we can find the 'this' text.");
            findDialog.Window.SendKeystrokes("this{ENTER}");
            Sleep(200);
            findDialog.Window.DismissPopUp("{ESC}");
            Sleep(200);
            w.SendKeystrokes("{ESC}");
            Sleep(200);
            w.SendKeystrokes("^c");
            CheckClipboard("<!-- This tests all element types -->");
            
            Trace.WriteLine("repeat find with shortcut");
            w.SendKeystrokes("{F3}{ESC}");
            Sleep(200);
            w.SendKeystrokes("^c");
            CheckClipboard(@"
    The XML markup in this version is Copyright © 1999 Jon Bosak.
    This work may freely be distributed on condition that it not be
    modified or altered in any way.
    ");

            Trace.WriteLine("Test illegal regular expressions.");
            findDialog = OpenFindDialog();
            findDialog.Window.DismissPopUp("\\%e{ENTER}");
            popup = findDialog.Window.ExpectingPopup("Find Error");
            popup.DismissPopUp("{ENTER}");
            Sleep(500);

            Trace.WriteLine("Find 'Microsoft' using regular expressions.");
            findDialog.Window.SendKeystrokes("M[{^} ]*t");
            findDialog.Window.DismissPopUp("{ENTER}{ESC}");
            Sleep(500);
            w.SendKeystrokes("{ESC}");
            Sleep(500);
            w.SendKeystrokes("^c");
            CheckClipboard("One Microsoft Way");

            Trace.WriteLine("test we can find 'last' in a comment only.");
            w.SendKeystrokes("{HOME}");
            findDialog = OpenFindDialog();
            findDialog.Window.SendKeystrokes("last%e{TAB}{TAB}c");
            findDialog.Window.DismissPopUp("{ENTER}{ESC}");
            Sleep(500);
            w.SendKeystrokes("{ESC}");
            Sleep(500);
            w.SendKeystrokes("^c");
            CheckClipboard("<!--last comment-->");

            w.SendKeystrokes("{HOME}");
            findDialog = OpenReplaceDialog();

            Trace.WriteLine("Toggle dialog using ctrl+f & ctrl+h");
            findDialog.Window.SendKeystrokes("^f");            
            Sleep(300); // so I can see it...
            findDialog.Window.SendKeystrokes("^h");            
            Sleep(300);

            Trace.WriteLine("test we can replace 'This' using case sensitive.");
            findDialog.Window.SendKeystrokes("This{TAB}xxx%m%w{TAB}{TAB}{TAB}e%a");
            string expected = @"
    The XML markup in this version is Copyright © 1999 Jon Bosak.
    xxx work may freely be distributed on condition that it not be
    modified or altered in any way.
    ";
            Sleep(200);
            findDialog.Window.DismissPopUp("{ESC}");
            Sleep(200);
            this.NodeTextView.SetFocus();
            w.SendKeystrokes("^c"); 
            CheckClipboard(expected);

            Trace.WriteLine("Failed replace, via replace button");
            w.SendKeystrokes("{HOME}");
            findDialog = OpenReplaceDialog();
            findDialog.Window.SendKeystrokes("will not find%r");
            popup = findDialog.Window.ExpectingPopup("Find Error");
            popup.DismissPopUp("{ENTER}");
            findDialog.Window.DismissPopUp("{ESC}");

            Trace.WriteLine("Check compound undo.");
            Undo();
            CheckOuterXml("<!-- This tests all element types -->");

            Sleep(1000);
            Save("out.xml");

            w.Dispose();
            Sleep(2000);
            ResetFindOptions();
        }

        void ResetFindOptions() {
            string path = Environment.GetEnvironmentVariable("USERPROFILE") + "\\Local Settings\\Application Data\\Microsoft\\Xml Notepad\\XmlNotepad.settings";
            if (!File.Exists(path)) {
                return;
            }
            XmlDocument doc = new XmlDocument();
            doc.Load(path);

            RemoveNode(doc, "//SearchWholeWord");
            RemoveNode(doc, "//SearchRegex");
            RemoveNode(doc, "//SearchMatchCase");
            RemoveNode(doc, "//FindMode");
            doc.Save(path);
        }

        void ResetFormatLongLines()
        {
            string path = Environment.GetEnvironmentVariable("USERPROFILE") + "\\Local Settings\\Application Data\\Microsoft\\Xml Notepad\\XmlNotepad.settings";
            if (!File.Exists(path))
            {
                return;
            }
            XmlDocument doc = new XmlDocument();
            doc.Load(path);

            RemoveNode(doc, "//AutoFormatLongLines");
            doc.Save(path);
        }

        void SetTreeViewWidth(int width)
        {
            XmlDocument doc = new XmlDocument();
            string path = Environment.GetEnvironmentVariable("USERPROFILE") + "\\Local Settings\\Application Data\\Microsoft\\Xml Notepad\\XmlNotepad.settings";
            if (!File.Exists(path))
            {
                XmlElement e = doc.CreateElement("Settings");
                doc.AppendChild(e);
            }
            else
            {
                doc.Load(path);
            }

            XmlNode node = doc.SelectSingleNode("//TreeViewSize");
            if (node == null)
            {
                node = doc.CreateElement("TreeViewSize");
                node.AppendChild(doc.CreateTextNode(width.ToString()));
                doc.DocumentElement.AppendChild(node);
            }
            else
            {
                XmlNode child = node.FirstChild;
                child.Value = width.ToString();
            }
            doc.Save(path);
        }

        void ClearSchemaCache() {
            string path = Environment.GetEnvironmentVariable("USERPROFILE") + "\\Local Settings\\Application Data\\Microsoft\\Xml Notepad\\XmlNotepad.settings";
            if (!File.Exists(path)) {
                return;
            }
            XmlDocument doc = new XmlDocument();
            doc.Load(path);

            RemoveNodes(doc, "//Schema");
            doc.Save(path);
        }

        void RemoveNode(XmlDocument doc, string xpath) {
            XmlNode node = doc.SelectSingleNode(xpath);
            if (node != null) node.ParentNode.RemoveChild(node);
        }

        void RemoveNodes(XmlDocument doc, string xpath) {
            List<XmlNode> toRemove = new List<XmlNode>();
            foreach (XmlNode node in doc.SelectNodes(xpath)) {
                toRemove.Add(node);
            }
            foreach (XmlNode node in toRemove)
            {
                node.ParentNode.RemoveChild(node);
            }

        }

        [TestMethod]
        public void TestToolbarAndContextMenus() {
            Trace.WriteLine("TestToolbarAndContextMenus==========================================================");

            string testFile = TestDir + "UnitTests\\test1.xml";
            var w = LaunchNotepad(testFile);

            Trace.WriteLine("Test toopstrip 'new' button");
            w.InvokeAsyncMenuItem("toolStripButtonNew");

            Trace.WriteLine("test recent files menu");
            w.SendKeystrokes("%f");
            Sleep(500);
            w.SendKeystrokes("f");
            Sleep(500);
            w.SendKeystrokes("{ENTER}");

            Sleep(1000);
            Trace.WriteLine("Test toolstrip button open");
            w.InvokeAsyncMenuItem("toolStripButtonOpen");
            Window openDialog = w.WaitForPopup();
            openDialog.DismissPopUp(testFile + "{ENTER}");
            Sleep(500);
            w.SendKeystrokes("^IRoot");

            Trace.WriteLine("Bring up context menu");
            w.SendKeystrokes("^ ");
            Sleep(500);
            w.SendKeystrokes("{UP}{ENTER}"); // collapse
            // Bring up context menu
            w.SendKeystrokes("^ ");
            Sleep(500);
            w.SendKeystrokes("{UP}{UP}{ENTER}"); // expand

            Trace.WriteLine("Test toolstrip copy, cut, undo, delete");
            w.SendKeystrokes("{UP}");
            Clipboard.SetText("error");
            w.InvokeMenuItem("toolStripButtonCopy");
            CheckClipboard("<?pi at root level?>");
            Clipboard.SetText("error");
            w.InvokeMenuItem("toolStripButtonCut");
            CheckClipboard("<?pi at root level?>");
            w.InvokeMenuItem("toolStripButtonUndo");
            w.InvokeMenuItem("toolStripButtonDelete");
            w.SendKeystrokes("{UP}");

            Trace.WriteLine("test toolstrip paste, undo, redo");
            Clipboard.SetText("<?pi at root level?>");
            w.InvokeMenuItem("toolStripButtonPaste");
            w.InvokeMenuItem("toolStripButtonUndo");
            w.InvokeMenuItem("toolStripButtonRedo");
            CheckNodeValue("pi");

            Trace.WriteLine("Test nudge commands");
            Sleep(1000);
            w.InvokeMenuItem("toolStripButtonNudgeDown");
            w.InvokeMenuItem("toolStripButtonNudgeRight");
            w.InvokeMenuItem("toolStripButtonNudgeUp");
            w.InvokeMenuItem("toolStripButtonNudgeLeft");

            Sleep(1000);
            Trace.WriteLine("context menu item - insert comment before");
            //bugbug: context menu items are not accessible?
            //w.InvokeMenuItem("ctxCommentBeforeToolStripMenuItem");
            w.SendKeystrokes("^ mb");
            Sleep(200);
            w.SendKeystrokes("it is finished");
            //bugbug: w.InvokeMenuItem("ctxPIBeforeToolStripMenuItem");
            w.SendKeystrokes("^ ob");
            Sleep(200);            
            w.SendKeystrokes("page{TAB}break{ENTER}");

            Save("out.xml");
            Sleep(1000);

            Trace.WriteLine("Test toolStripButtonSave 'save'");
            w.InvokeMenuItem("toolStripButtonSave");            

            this.SaveAndCompare("out.xml", "test5.xml");
        }

        [TestMethod]
        public void TestNudge() {
            Trace.WriteLine("TestNudge==========================================================");
            string testFile = TestDir + "UnitTests\\test1.xml";
            var w = LaunchNotepad(testFile);

            // better test when things are expanded
            w.InvokeMenuItem("collapseAllToolStripMenuItem");
 
            // better test when things are expanded
            w.InvokeMenuItem("expandAllToolStripMenuItem");
            
            int cmds = 0;
            w.SendKeystrokes("^I#"); // select first comment
            w.InvokeMenuItem("downToolStripMenuItem");
            cmds++;
            w.InvokeMenuItem("downToolStripMenuItem");
            cmds++;
            w.InvokeMenuItem("upToolStripMenuItem");
            cmds++;
            w.InvokeMenuItem("upToolStripMenuItem");
            cmds++;


            // test nudge attr ({DOWN} resets type-to-find).
            Sleep(1000);
            w.SendKeystrokes("{DOWN}^Iid"); // select first attribute
            
            w.InvokeMenuItem("downToolStripMenuItem");
            cmds++;
            w.InvokeMenuItem("upToolStripMenuItem");
            cmds++;

            // test nudge element .
            Sleep(1000);
            w.SendKeystrokes("{DOWN}^IEmp");
            
            w.InvokeMenuItem("downToolStripMenuItem");
            cmds++;
            w.InvokeMenuItem("upToolStripMenuItem");
            cmds++;
            w.InvokeMenuItem("rightToolStripMenuItem");
            cmds++;
            w.InvokeMenuItem("leftToolStripMenuItem");
            cmds++;
            w.InvokeMenuItem("upToolStripMenuItem");
            cmds++;
            w.InvokeMenuItem("upToolStripMenuItem");
            cmds++;
            w.InvokeMenuItem("upToolStripMenuItem");
            cmds++;
            w.InvokeMenuItem("upToolStripMenuItem");
            cmds++;
            w.InvokeMenuItem("upToolStripMenuItem");
            cmds++;

            // test nudge pi
            Sleep(1000);
            w.SendKeystrokes("{DOWN}^Ipi"); // select next pi
            
            w.InvokeMenuItem("leftToolStripMenuItem");
            cmds++;
            w.InvokeMenuItem("rightToolStripMenuItem");
            cmds++;
            w.InvokeMenuItem("upToolStripMenuItem");
            cmds++;

            this.SaveAndCompare("out.xml", "test3.xml");

            // Make sure MoveNode is undoable!
            UndoRedo(cmds);

            this.SaveAndCompare("out2.xml", "out.xml");
        }

        [TestMethod]
        public void TestDragDrop() {
            Trace.WriteLine("TestDragDrop==========================================================");
            var w = this.LaunchNotepad();

            Rectangle treeBounds = this.TreeView.Bounds;

            Trace.WriteLine("OpenFileDialog");
            w.InvokeAsyncMenuItem("openToolStripMenuItem");
            Window openDialog = w.WaitForPopup();
            Trace.WriteLine("Opening '" + TestDir + "UnitTests'");
            openDialog.SendKeystrokes(TestDir + "UnitTests{ENTER}");
            Sleep(1000);

            // Drag/drop from open file dialog into xml notepad client area.
            OpenFileDialog dialogWrapper = new OpenFileDialog(openDialog);

            Point drop = GetDropSpot(openDialog, treeBounds);
            Trace.WriteLine("Drop spot = " + drop.ToString());

            AutomationWrapper item = dialogWrapper.GetFileItem("test1.xml");

            if (item == null)
            {
                // try finding the item using the keyboard.
                throw new Exception("File item not found");
            }

            Rectangle ibounds = item.Bounds;
            Point iloc = new Point(ibounds.Left + 10, ibounds.Top + 10);
            Trace.WriteLine("Dragging from " + iloc.ToString());
            Mouse.MouseDragDrop(iloc, drop, 5, MouseButtons.Left);
            Sleep(1000);
            dialogWrapper.DismissPopUp("{ESC}");
            
            // need bigger window to test drag/drop
            w.SetWindowSize(800, 600);
            
            w.InvokeMenuItem("collapseAllToolStripMenuItem");
            w.InvokeMenuItem("expandAllToolStripMenuItem");

            // Test mouse wheel
            AutomationWrapper tree = this.TreeView;
            CheckProperties(tree);
            
            w.SendKeystrokes("{HOME}");
            Cursor.Position = tree.Bounds.Center();
            Sleep(500); // wait for focus to kick in before sending mouse events.
            Mouse.MouseWheel(-120 * 15); // first one doesn't get thru for some reason!
            Sleep(500);
            Mouse.MouseWheel(120 * 15);
            Sleep(500);
            
            // Test navigation keys
            w.SendKeystrokes("{HOME}");
            CheckNodeValue("<?xml version=\"1.0\" encoding=\"utf-8\"?>");
            w.SendKeystrokes("{END}");
            CheckNodeValue("<!--last comment-->");
            w.SendKeystrokes("{PGUP}{PGDN}{UP}{UP}");
            CheckNodeValue("Name");            
            
            // Get AutomationWrapper to selected node in the tree.
            AutomationWrapper ntv = this.NodeTextView;
            CheckProperties(ntv);
            // mouse down in node text view
            AutomationWrapper node = ntv.GetSelectedChild();
            node = node.Parent.NextSibling; // Office node.
            CheckNodeName(node, "Office");
            Rectangle bounds = node.Bounds;
            Mouse.MouseClick(bounds.Center(), MouseButtons.Left);

            // test edit of node value using AccessibilityObject
            string office = "35/1682";
            node.Value = office;

            // for some odd reason the paste expands the element.
            Sleep(300);
            CheckNodeValue(office);  // confirm via copy operation
                        
            node = tree.GetSelectedChild();
            if (node == null) {
                throw new ApplicationException("Selected node not found");
            }
            CheckProperties(node);
            CheckNodeName(node, "Office");
            node.AddToSelection();

            // test edit of node name using accessibility.
            this.SetNodeName(node, "MyOffice");
            CheckNodeValue("MyOffice");  // confirm via copy operation

            // Test that "right arrow" moves over to the nodeTextView.
            w.SendKeystrokes("{RIGHT}{DOWN}{RIGHT}");
            CheckNodeValue("35/1682");  // confirm via copy operation

            Undo(); // make sure we can undo node name change!
            Undo(); // make sure we can undo node value change (while #text is expanded)!
            this.TreeView.SetFocus();
            CheckNodeValue("Office");

            Trace.WriteLine("Select the 'Country' node.");
            bounds = node.Bounds;
            Trace.WriteLine(bounds.ToString());
            int itemHeight = bounds.Height;
            Point pt = bounds.Center();
            pt.Y -= (itemHeight * 2);

            // Test mouse down in tree view;
            Mouse.MouseClick(pt, MouseButtons.Left);
            Sleep(200);
            node = tree.GetSelectedChild();
            CheckNodeName(node, "Country");

            Trace.WriteLine("Drag/drop country up 3 items");
            Sleep(1000); // avoid double click by delaying next click

            Point endPt = new Point(pt.X, pt.Y - (int)(3 * itemHeight) - (itemHeight/2));
            // Drag the node up three slots.
            Mouse.MouseDragDrop(pt, endPt, 5, MouseButtons.Left);

            Sleep(200);

            node = tree.GetSelectedChild();
            CheckNodeName(node, "Country");

            // Drag/drop to auto scroll, then leave the window and drop it on desktop
            Rectangle formBounds = w.GetScreenBounds();
            Mouse.MouseDown(endPt, MouseButtons.Left);
            // Autoscroll
            Point treeTop = TopCenter(tree.Bounds, 2);

            Trace.WriteLine("--- Drag to top of tree view ---"); 
            Mouse.MouseDragTo(endPt, treeTop, 5, MouseButtons.Left);
            Sleep(1000); // autoscroll time.
            // Drag out of tree view.
            Point titleBar = TopCenter(formBounds, 20);
            Trace.WriteLine("--- Drag to titlebar ---");
            Mouse.MouseDragTo(treeTop, titleBar, 10, MouseButtons.Left);
            Sleep(1000); // should now have 'no drop icon'.
            Mouse.MouseUp(titleBar, MouseButtons.Left);            

            // code coverage on expand/collapse.
            w.SendKeystrokes("^IOffice");
            node.Invoke();
            Sleep(500);
            w.SendKeystrokes("{LEFT}");
            Sleep(500);
            w.SendKeystrokes("{RIGHT}");

            Sleep(1000);
            Trace.WriteLine("Test task list resizers");
            AutomationWrapper resizer = w.FindDescendant("TaskResizer");
            Trace.WriteLine(resizer.Parent.Name);
            bounds = resizer.Bounds;
            Point mid = bounds.Center();
            // Drag the resizer up a few pixels.
            Mouse.MouseDragDrop(mid, new Point(mid.X, mid.Y - 15), 2, MouseButtons.Left);

            Trace.WriteLine("Test tree view resizer");
            resizer = w.FindDescendant("XmlTreeResizer");
            Trace.WriteLine(resizer.Parent.Name);
            bounds = resizer.Bounds;
            mid = bounds.Center();
            // Drag the resizer up a few pixels.
            Mouse.MouseDragDrop(mid, new Point(mid.X + 15, mid.Y), 2, MouseButtons.Left);

            this.SaveAndCompare("out.xml", "test4.xml");
        }

        /// <summary>
        /// Make the given rectangle visible by moving the given window out of the way.
        /// </summary>
        /// <param name="w"></param>
        /// <param name="target"></param>
        /// <returns></returns>
        Point GetDropSpot(Window w, Rectangle target) {
            AutomationWrapper acc = w.AccessibleObject;
            Rectangle source = acc.Bounds;
            source.Inflate(20, 20); // add extra margin
            if (source.Contains(target)) {
                // Source window is completely occluding the target window, so we need to move it!
                Point from = new Point(source.Left + (source.Width/2), source.Top + 5);
                int amount = target.Left - source.Left + 300;
                Point end = new Point(from.X + amount, from.Y);
                // Move window to the right.
                Mouse.MouseDown(from, MouseButtons.Left);
                Mouse.MouseDragDrop(from, end, 5, MouseButtons.Left);
                
                source = acc.Bounds;
            }
            if (source.Left > target.Left) {
                // pick a spot along the left margin
                return new Point((target.Left + source.Left) / 2, (target.Top + target.Bottom) / 2);
            } else if (source.Right < target.Right) {
                // pick a spot along the right margin
                return new Point((target.Right + source.Right) / 2, (target.Top + target.Bottom) / 2);
            } else if (source.Top > target.Top) {
                // top margin
                return new Point((target.Right + target.Left) / 2, (source.Top + target.Top) / 2);
            } else if (source.Bottom < target.Bottom) {
                // bottom margin
                return new Point((target.Right + target.Left) / 2, (source.Bottom + target.Bottom) / 2);
            }

            // Then MOVE the window so it's not in the way!
            w.SetWindowPosition(target.Right, source.Top);
            Sleep(1000);
            source = acc.Bounds;
            return new Point((target.Left + source.Left) / 2, (target.Top + target.Bottom) / 2);
        }        

        [TestMethod]
        public void TestAccessibility() {

            Trace.WriteLine("TestAccessibility==========================================================");
            string testFile = TestDir + "UnitTests\\test1.xml";
            Window w = this.LaunchNotepad(testFile);
            Sleep(1000);

            // Get AutomationWrapper to selected node in the tree.
            AutomationWrapper tree = this.TreeView;
            AutomationWrapper root = tree.GetChild(3);
            
            // employee
            AutomationWrapper emp = root.GetChild(7);
            emp.Select();
            CheckNodeName(emp, "Employee");
            Trace.Assert(emp.Name == tree.GetSelectedChild().Name);

            string state = emp.Status;
            emp.IsExpanded = true;
            AutomationWrapper node = emp.FirstChild;
            
            // Test accesibility navigation!
            node = node.Parent;
            CheckNodeName(node, "Employee");
            node = node.FirstChild;
            node = node.NextSibling;
            node.AddToSelection();
            CheckNodeName(node, "id");

            // over to node text view!
            w.SendKeystrokes("{TAB}");
            node = this.NodeTextView.GetSelectedChild();
            CheckProperties(node);
            CheckNodeValue(node, "46613");

            //node = node.Navigate(AccessibleNavigation.Down);
            node = node.NextSibling;
            node.AddToSelection();
            CheckNodeValue(node, "Architect");

            //node = node.Navigate(AccessibleNavigation.Left);
            w.SendKeystrokes("+{TAB}");
            node = this.TreeView.GetSelectedChild();
            CheckNodeName(node, "title");

            //node = node.Navigate(AccessibleNavigation.Right); // over to node text view!
            w.SendKeystrokes("{TAB}");
            w.SendKeystrokes("+{TAB}");
            //node = node.Navigate(AccessibleNavigation.Up);
            node = node.PreviousSibling;
            //node = node.Navigate(AccessibleNavigation.Previous);
            node = node.PreviousSibling;
            CheckNodeName(node, "xmlns");
            //node = node.Navigate(AccessibleNavigation.Down);
            node = node.NextSibling;
            //node = node.Navigate(AccessibleNavigation.Next);
            node = node.NextSibling;
            CheckNodeName(node, "title");
            
            // Test TAB and SHIFT-TAB navigation.
            w.SendKeystrokes("{TAB}");
            Sleep(200);
            CheckNodeValue("Architect");
            w.SendKeystrokes("{TAB}");
            Sleep(200);
            CheckNodeValue("Name");
            w.SendKeystrokes("+{TAB}");
            Sleep(200);
            CheckNodeValue("Architect");
            w.SendKeystrokes("+{TAB}");
            Sleep(200);
            CheckNodeValue("title");

            // change node value!
            w.SendKeystrokes("{TAB}");
            node = this.NodeTextView.GetSelectedChild();
            node.Value = "foo";
            Sleep(200);
            CheckNodeValue("foo");
            w.SendKeystrokes("+{TAB}{LEFT}"); // back to Employee

            // hit test Employee node.
            Point p = emp.Bounds.Center();
            node = node.HitTest(p.X, p.Y);
            Trace.Assert(node.Name == emp.Name);

            emp.RemoveFromSelection();
            emp.Select();
            Trace.Assert(root.Name == emp.Parent.Name);
            AutomationWrapper parent = root.Parent;
            string name = parent.Name;
            Trace.Assert(name == "TreeView");

            // default action on tree is toggle!
            tree.Invoke();
            Sleep(500);

            // state on invisible nodes.
            Trace.Assert(node.IsVisible);

            // get last child of tree
            AutomationWrapper cset = tree.LastChild;
            CheckNodeName(cset, "Root");

            // select tree
            tree.SetFocus();

            // Get AutomationWrapper on node text view.
            AutomationWrapper ntv = NodeTextView;
            ntv.SetFocus();
            AutomationWrapper first = ntv.FirstChild;
            ntv.Invoke(); // enter edit mode
            Sleep(1000); // so we can see it...
            w.SendKeystrokes("{ESC}");

            // scroll back to root so the pi is in view for hittest.
            w.SendKeystrokes("^{HOME}");

            // hit test on node text
            ntv = NodeTextView; // need to refresh this sometimes...
            AutomationWrapper pivalue = ntv.GetChild(2);

            CheckNodeValue(pivalue, "at root level");
            p = pivalue.Bounds.Center();
            var hit = ntv.HitTest(p.X, p.Y);
            string hitname = hit.Name;
            if (hitname != pivalue.Name)
            {
                throw new Exception(string.Format("Hit test failed, hit {0}, but expecting {1}", hitname, pivalue.Name));
            }

            // Navigate to last child
            cset = ntv.LastChild;
            CheckNodeName(cset, "Root");

            AutomationWrapper next = first.NextSibling;
            CheckNodeValue(next, " This tests all element types ");            

            next = next.NextSibling; // pi
            next = next.NextSibling; // root
            next.Invoke(); // toggle
            next.Invoke(); // toggle
            
            emp = root.GetChild(7);
            emp.Select();

            AutomationWrapper ev = next.GetChild(7); // Employee value node
            Trace.Assert(ntv.GetSelectedChild().Name == ev.Name);
            w.SendKeystrokes("{RIGHT}"); // expand employee.

            node = ev.FirstChild;
            CheckNodeValue(node, "http://www.hr.org");
            node = ev.LastChild;
            CheckNodeName(node, "Office");
            node = node.PreviousSibling; // was AccessibleNavigation.Up...
            CheckNodeName(node, "Country");
            node.RemoveFromSelection();
            node.Select();

            // set the node name
            w.SendKeystrokes("{LEFT}");

            SetNodeName(node, "foo");
            CheckNodeName(node, "foo");
            Undo();
            CheckNodeName(node, "Country");

            Save("out.xml");
        }

        private void SetNodeName(AutomationWrapper node, string text)
        { 
            // must not be a leaf node then, unfortunately the mapping from IAccessible to AutomationElement
            // don't add ValuePattern on ListItems that have children, so we have to get the value the hard way.
            Sleep(100);
            this.window.SendKeystrokes("{ENTER}");
            Sleep(300); // wait for editor to pop up 
            this.window.SendKeystrokes(text);
            Sleep(300);
            this.window.SendKeystrokes("{ENTER}");
            Sleep(300);            
        }
        
        [TestMethod]
        public void TestKeyboard() {
            Trace.WriteLine("TestKeyboard==========================================================");
            string testFile = TestDir + "UnitTests\\emp.xml";
            string xsdFile = TestDir + "UnitTests\\emp.xsd";
            var w = this.LaunchNotepad(testFile);

            Sleep(1000);

            Trace.WriteLine("Test goto definition on schemaLocation");
            w.SendKeystrokes("^Ixsi{F12}");
            Window popup = w.ExpectingPopup("XML Notepad - " + xsdFile);
            popup.DismissPopUp("%{F4}");

            Trace.WriteLine("Test namespace intellisense, make sure emp.xsd namespace is in the list.");
            w.SendKeystrokes("{HOME}^Ixmlns{ESC}{TAB}{ENTER}");
            Sleep(250);
            w.SendKeystrokes("{END}{HOME}{ENTER}");
            Sleep(250);

            Trace.WriteLine("Test schemaLocation attribute.");
            w.SendKeystrokes("{LEFT}^Ixsi{DEL}");
            Sleep(1000); // let it validate without xsi location.
            Undo();
            Sleep(1000);

            Trace.WriteLine("Expand all");
            w.SendKeystrokes("{END}");
            w.SendKeystrokes("{MULTIPLY}"); // expandall.

            Trace.WriteLine("Goto next view");
            w.SendKeystrokes("{F6}"); // goto node text view
            Sleep(500);

            Trace.WriteLine("Create some validation errors");
            w.SendKeystrokes("{DOWN}^IRed");
            Sleep(200);
            w.SendKeystrokes("Red");
            Sleep(200);
            w.SendKeystrokes("{ENTER}{BACKSPACE}{ENTER}"); // delete "Redmond"
            Sleep(200);
            w.SendKeystrokes("{UP}^I98");
            Sleep(200);
            w.SendKeystrokes("{ENTER}{BACKSPACE}{ENTER}"); // delete "98052"

            Sleep(1000);  // give it a chance to validate and produce errors.

            Trace.WriteLine("Navigate errors");
            w.SendKeystrokes("{HOME}{F6}"); // Navigate to error list
            w.SendKeystrokes("{DOWN}{DOWN}{ENTER}"); // Select second error
            CheckNodeValue("Zip");

            Trace.WriteLine("Previous pane");
            w.SendKeystrokes("+{F6}"); // Navigate back to error list
            w.SendKeystrokes("{UP}{UP}{UP}{ENTER}"); // Select first error
            CheckNodeValue("City");            

            Trace.WriteLine("Next Error");
            w.SendKeystrokes("{F4}{F4}"); // next error twice
            CheckNodeValue("Zip");

            Trace.WriteLine("Collapse/Expand country");

            w.SendKeystrokes("{DOWN}^ICo{SUBTRACT}"); // collapse country
            Sleep(100); // just so we can watch it
            w.SendKeystrokes("{ADD}"); // re-expand country
            Sleep(100);

            Trace.WriteLine("Nudge commands");
            w.SendKeystrokes("^+{LEFT}"); // nudge country left
            Sleep(100);
            w.SendKeystrokes("^+{RIGHT}"); // nudge country right
            Sleep(100);
            w.SendKeystrokes("^+{UP}^+{UP}^+{UP}"); // nudge country back up to where it was
            Sleep(100);
            w.SendKeystrokes("^+{DOWN}"); // nudge country down
            Sleep(100);

            Trace.WriteLine("Fix errors");
            w.SendKeystrokes("{UP}{TAB}{F2}98052{ENTER}"); // add zip code back
            Sleep(200);
            w.SendKeystrokes("{UP}{ENTER}Redmond{ENTER}"); // add redmond back
            Sleep(300); // let it re-validate

            Trace.WriteLine("Test direct editing of names/values");
            w.SendKeystrokes("y{ENTER}");
            Sleep(100);
            w.SendKeystrokes("{LEFT}x{ENTER}");
            Sleep(100);
            w.SendKeystrokes("^c");

            CheckClipboard("<x xmlns=\"http://Employees\">y</x>");
            Undo();
            Undo();

            Sleep(1000);
            this.SaveAndCompare("out.xml", "emp.xml");
        }

        [TestMethod]
        public void TestMouse() {
            Trace.WriteLine("TestMouse==========================================================");
            string testFile = TestDir + "UnitTests\\emp.xml";
            var w = this.LaunchNotepad(testFile);

            Sleep(1000);

            // Test mouse click on +/-.
            AutomationWrapper tree = this.TreeView;
            AutomationWrapper node = tree.FirstChild;
            node = node.LastChild;
            node.Select();

            Rectangle bounds = node.Bounds;
            TestHitTest(bounds.Center(), tree, node);

            // is there any way to get this state ?
            //bool expanded = node.IsExpanded;
            //if (expanded){
            //    throw new ApplicationException(
            //        string.Format("Did not expect node '{0}' to be expanded here", node.Name));
            //}

            // minus tree indent and image size
            Point plusminus = new Point(bounds.Left - 30 - 16, (bounds.Top + bounds.Bottom) / 2);

            Mouse.MouseClick(plusminus, MouseButtons.Left);

            Sleep(500);

            // is there any way to get this state ?
            //bool expanded2 = node.IsExpanded;
            //if (!expanded2) {
            //    throw new ApplicationException("Node did not become expanded");
            //}

            //mouse down edit of node name
            Mouse.MouseClick(bounds.Center(), MouseButtons.Left);
            Sleep(1000); // give it enough time to kick into edit mode.

            CheckOuterXml("Employee");
            this.window.SendKeystrokes("{ESCAPE}");
            
            // code coverage on scrollbar interaction
            AutomationWrapper vscroll = w.FindDescendant("VScrollBar");
            bounds = vscroll.Bounds;

            Point downArrow = new Point((bounds.Left + bounds.Right) / 2, bounds.Bottom - (bounds.Width / 2));
            for (int i = 0; i < 10; i++) {
                Mouse.MouseClick(downArrow, MouseButtons.Left);
                Sleep(500);
            }

        }

        [TestMethod]
        public void TestUtilities() {
            Trace.WriteLine("TestUtilities==========================================================");
            // code coverage on hard to reach utility code.
            HLSColor hls = new HLSColor(Color.Red);
            Trace.WriteLine(hls.ToString());
            Trace.WriteLine(hls.Darker(0.5F).ToString());
            Trace.WriteLine(hls.Lighter(0.5F).ToString());
            Trace.WriteLine(hls == new HLSColor(Color.Red));
            Trace.WriteLine(hls.GetHashCode());

            // Test resource class.
            Type t = FormMain.ResourceType;
            foreach (PropertyInfo pi in t.GetProperties(BindingFlags.Static)) {
                if (pi.PropertyType == typeof(string)) {
                    string name = pi.Name;
                    object res = pi.GetValue(null, null);
                    if (res == null) {
                        throw new Exception("Unexpected null returned from property: " + name);
                    }
                    Trace.WriteLine(string.Format("{0}={1}", name, res.ToString()));
                }
            }

            // Test XmlIncludeReader
            string test = TestDir + "UnitTests\\includes\\index.xml";
            Uri baseUri = new Uri(test);
            XmlDocument doc = new XmlDocument();
            doc.Load(test);

            XmlReaderSettings settings = new XmlReaderSettings();
            settings.DtdProcessing = DtdProcessing.Parse;
            foreach (XmlElement e in doc.SelectNodes("test/case")) {
                Uri input = new Uri(baseUri, e.GetAttribute("input"));
                Uri output = new Uri(baseUri, e.GetAttribute("results"));
                using (XmlIncludeReader r = XmlIncludeReader.CreateIncludeReader(input.LocalPath, settings)) {
                    CompareResults(ReadNodes(r), output.LocalPath);
                }
            }
        }

        [TestMethod]
        public void TestNoBorderTabControl() {
            Form f = new Form();
            f.Size = new Size(400, 400);

            NoBorderTabControl tabs = new NoBorderTabControl();
            NoBorderTabPage page1 = new NoBorderTabPage();
            page1.Text = "Apple";
            CheckBox c = new CheckBox();
            c.Text = "this is a checkbox";
            c.Location = new Point(10, 10);
            page1.Controls.Add(c);
            tabs.TabPages.Add(page1);
            NoBorderTabPage page2 = new NoBorderTabPage();
            page2.Text = "Orange";
            RadioButton b1 = new RadioButton();
            b1.Text = "test1";
            b1.Location = new Point(10, 10); 
            page2.Controls.Add(b1);
            RadioButton b2 = new RadioButton();
            b2.Text = "test2";
            b2.Location = new Point(10, 30);
            page2.Controls.Add(b2);
            tabs.TabPages.Insert(0, page2);
            tabs.Dock = DockStyle.Fill;
            f.Controls.Add(tabs);

            f.Show();
            
            Sleep(1000);

            tabs.TabPages.Remove(page1);
            tabs.TabPages.Remove(page2);
            tabs.TabPages.Add(page1);
            tabs.TabPages.Add(page2);

            
            Sleep(1000);

            tabs.TabPages.Clear();
            tabs.TabPages.Add(page1);

            
            Sleep(1000);

            Trace.Assert(tabs.TabPages.Contains(page1));
            Trace.Assert(!tabs.TabPages.Contains(page2));
            tabs.TabPages.Insert(0, page2);

            
            Sleep(1000);

            int i = tabs.TabPages.IndexOf(page1);
            Trace.Assert(i == 1);

            i = tabs.TabPages.IndexOf(page2);
            Trace.Assert(i == 0);

            Trace.Assert(!tabs.TabPages.IsFixedSize);
            Trace.Assert(!tabs.TabPages.IsReadOnly);
            Trace.Assert(!tabs.TabPages.IsSynchronized);

            tabs.TabPages.Remove(page1);
            tabs.TabPages.RemoveAt(0);
            
            Sleep(1000);

            tabs.TabPages[0] = page1;
            tabs.TabPages[1] = page2;
            
            Sleep(1000);

            NoBorderTabPage[] a = new NoBorderTabPage[tabs.TabPages.Count];
            tabs.TabPages.CopyTo(a, 0);
            Trace.Assert(a[0] == page1);
            Trace.Assert(a[1] == page2);
            f.Close();
        }

        [TestMethod]
        public void TestInclude() {
            Trace.WriteLine("TestInclude==========================================================");
            string nonexist = TestDir + "UnitTests\\Includes\\nonexist.xml";
            WipeFile(nonexist);

            try {
                
                string testFile = TestDir + "UnitTests\\Includes\\i1.xml";
                var w = this.LaunchNotepad(testFile);

                w.SendKeystrokes("^Iinclude");
                w.InvokeMenuItem("gotoDefinitionToolStripMenuItem");
                Window popup = w.WaitForPopup();
                popup.DismissPopUp("%{F4}");
                
                w.InvokeMenuItem("expandXIncludesToolStripMenuItem");
                this.SaveAndCompare("Includes\\out.xml", "Includes\\r1.xml");

                Trace.WriteLine("Test F12 on non-existant include");
                testFile = TestDir + "UnitTests\\Includes\\i3.xml";
                w.InvokeAsyncMenuItem("openToolStripMenuItem");
                popup = w.WaitForPopup();
                popup.DismissPopUp(testFile + "{ENTER}");
               
                w.SendKeystrokes("^Ix:{F12}");
                popup = w.WaitForPopup();

                popup.SendKeystrokes("{ENTER}");
                // Should create new file and open it!
                Sleep(1000);
                popup = w.WaitForPopup();
                popup.SendKeystrokes("%{F4}");

                Sleep(2000);
                if (!File.Exists(nonexist)) {
                    throw new ApplicationException("File should now exist!");
                }
            } finally {
                WipeFile(nonexist);
            }
        }

        [TestMethod]
        public void TestUnicode() {

            ClearSchemaCache();

            Trace.WriteLine("TestUnicode==========================================================");
            string testFile = TestDir + "UnitTests\\unicode.xml";
            var w = this.LaunchNotepad(testFile);

            string outFile = TestDir + "UnitTests\\out.xml";
            WipeFile(outFile);

            w.InvokeAsyncMenuItem("exportErrorsToolStripMenuItem");
            
            Window popup = w.WaitForPopup();
            popup.DismissPopUp(outFile + "{ENTER}");
            
            string expectedFile = TestDir + "UnitTests\\errors.xml";
            CompareResults(ReadNodes(expectedFile), outFile);

        }

        [TestMethod]
        public void TestChangeTo() {
            Trace.WriteLine("TestChangeTo==========================================================");
            string testFile = TestDir + "UnitTests\\test8.xml";
            var w = this.LaunchNotepad(testFile);
            Sleep(1000);

            w.InvokeMenuItem("expandAllToolStripMenuItem");
            Sleep(1000);

            w.SendKeystrokes("{DOWN}^ICard{ESC}");
            w.SendKeystrokes("^c");
            string expected = Clipboard.GetText(); // save expected text

            Trace.WriteLine("Change element to attribute");
            w.SendKeystrokes("{DOWN}{DOWN}");
            w.InvokeMenuItem("changeToAttributeToolStripMenuItem1");
            Sleep(1000);

            w.SendKeystrokes("{LEFT}");
            w.SendKeystrokes("^c");
            this.CheckClipboard("<Card bar=\"2\"><foo>1</foo><end>3</end></Card>");
            Trace.WriteLine("Make undo inserts element in the right place.");
            this.Undo();
            w.SendKeystrokes("{LEFT}");
            w.SendKeystrokes("^c");
            this.CheckClipboard(expected);

            Sleep(2000);
            w.SendKeystrokes("{DOWN}^IName{ESC}");
            w.InvokeMenuItem("changeToCDATAToolStripMenuItem1");
            this.CheckOuterXml("<![CDATA[<Name First=\"Chris\" Last=\"Lovett\">/[A CDATA block]/</Name>]]>");

            Trace.WriteLine("Change element to Comment (with nested comments).");
            w.SendKeystrokes("{LEFT}");
            w.InvokeMenuItem("changeToCommentToolStripMenuItem1");
            this.CheckOuterXml("<!--<Contact>/*inner comment*/<![CDATA[<Name First=\"Chris\" Last=\"Lovett\">/[A CDATA block]/</Name>]]></Contact>-->");

            Trace.WriteLine("Change comment back to element (with nested comments!)");
            w.InvokeMenuItem("changeToElementToolStripMenuItem1");
            this.CheckOuterXml("<Contact><!--inner comment--><![CDATA[<Name First=\"Chris\" Last=\"Lovett\">/[A CDATA block]/</Name>]]></Contact>");

            Trace.WriteLine("Change CDATA back to element (with nested CDATA!)");
            w.SendKeystrokes("{END}");
            w.InvokeMenuItem("changeToElementToolStripMenuItem1");
            this.CheckOuterXml("<Name First=\"Chris\" Last=\"Lovett\"><![CDATA[A CDATA block]]></Name>");

            Trace.WriteLine("Make sure this is all undoable.");
            this.Undo();
            this.Undo();
            this.Undo();
            this.Undo();

            Trace.WriteLine("Now file should be identical to original");            
            SaveAndCompare("out.xml", "test8.xml");

            Trace.WriteLine("Change attribute to element");
            w.SendKeystrokes("^Iid{ESC}");
            w.InvokeMenuItem("changeToElementToolStripMenuItem1");
            this.CheckOuterXml("<id>55</id>");

            Trace.WriteLine("Change element to attribute");
            w.InvokeMenuItem("changeToAttributeToolStripMenuItem1");//changeToAttributeContextMenuItem");
            this.CheckOuterXml("id=\"55\"");

            this.Undo();
            this.Undo();

            Trace.WriteLine("Change attribute to PI");
            w.InvokeMenuItem("changeToProcessingInstructionToolStripMenuItem");
            this.CheckOuterXml("<?id 55?>");

            Trace.WriteLine("Change PI to element");
            w.InvokeMenuItem("changeToElementToolStripMenuItem1");
            this.CheckOuterXml("<id>55</id>");

            Trace.WriteLine("Change element to text");
            w.InvokeMenuItem("changeToTextToolStripMenuItem1");
            this.CheckOuterXml("&lt;id&gt;55&lt;/id&gt;");

            Trace.WriteLine("Change text to comment");
            w.InvokeMenuItem("changeToCommentToolStripMenuItem1");//changeToCommentContextMenuItem");
            this.CheckOuterXml("<!--<id>55</id>-->");

            Trace.WriteLine("Change comment to CDATA");
            w.InvokeMenuItem("changeToCDATAToolStripMenuItem1");//changeToCDATAContextMenuItem");
            this.CheckOuterXml("<![CDATA[<id>55</id>]]>");

            Trace.WriteLine("Change CDATA to Attribute");
            w.InvokeMenuItem("changeToAttributeToolStripMenuItem1");
            this.CheckOuterXml("id=\"55\"");

            this.Undo();
            this.Undo();
            this.Undo();
            this.Undo();
            this.Undo();
            this.Undo();

            w.SendKeystrokes("{END}");
            Trace.WriteLine("Change CDATA to Comment");
            w.InvokeMenuItem("changeToCommentToolStripMenuItem1");
            this.CheckOuterXml("<!--A CDATA block-->");

            Trace.WriteLine("Change Comment to Text");
            w.InvokeMenuItem("changeToTextToolStripMenuItem1");
            this.CheckOuterXml("A CDATA block");

            Sleep(1000);
            Trace.WriteLine("Change text to PI");
            w.InvokeMenuItem("changeToProcessingInstructionToolStripMenuItem");//changeToProcessingInstructionContextMenuItem");
            this.CheckOuterXml("<?pi A CDATA block?>");

            Trace.WriteLine("Change PI to attribute");
            w.InvokeMenuItem("changeToAttributeToolStripMenuItem1");
            this.CheckOuterXml("pi=\"A CDATA block\"");

            Trace.WriteLine("Change attribute to comment");
            w.InvokeMenuItem("changeToCommentToolStripMenuItem1");
            this.CheckOuterXml("<!--pi=\"A CDATA block\"-->");

            Trace.WriteLine("Change comment to PI");
            w.InvokeMenuItem("changeToProcessingInstructionToolStripMenuItem");
            this.CheckOuterXml("<?pi A CDATA block?>");

            Trace.WriteLine("Change PI to comment");
            w.InvokeMenuItem("changeToCommentToolStripMenuItem1");
            this.CheckOuterXml("<!--<?pi A CDATA block?>-->");

            this.Undo();

            Trace.WriteLine("Change PI to CDATA");
            w.InvokeMenuItem("changeToCDATAToolStripMenuItem1");
            this.CheckOuterXml("<![CDATA[<?pi A CDATA block?>]]>");

            Trace.WriteLine("Change CDATA to Text");
            w.InvokeMenuItem("changeToTextToolStripMenuItem1");
            this.CheckOuterXml("&lt;?pi A CDATA block?&gt;");

            Trace.WriteLine("Change Text to Element");
            w.InvokeMenuItem("changeToElementToolStripMenuItem1");
            this.CheckOuterXml("<pi>A CDATA block</pi>");

            Trace.WriteLine("Change Element to PI");
            w.InvokeMenuItem("changeToProcessingInstructionToolStripMenuItem");
            this.CheckOuterXml("<?pi A CDATA block?>");

            Trace.WriteLine("Change PI to Text");
            w.InvokeMenuItem("changeToTextToolStripMenuItem1");
            this.CheckOuterXml("&lt;?pi A CDATA block?&gt;");

            Trace.WriteLine("Change Text to Attribute");
            w.InvokeMenuItem("changeToAttributeToolStripMenuItem1");
            this.CheckOuterXml("pi=\"A CDATA block\"");

            Trace.WriteLine("Change Attribute to Text");
            w.InvokeMenuItem("changeToTextToolStripMenuItem1");
            this.CheckOuterXml("pi=\"A CDATA block\"");

            Trace.WriteLine("Change Attribute to Text");
            w.InvokeMenuItem("changeToCDATAToolStripMenuItem1");
            this.CheckOuterXml("<![CDATA[pi=\"A CDATA block\"]]>");

            Trace.WriteLine("Change CDATA to PI");
            w.InvokeMenuItem("changeToProcessingInstructionToolStripMenuItem");
            this.CheckOuterXml("<?pi A CDATA block?>");

            Trace.WriteLine("Change PI to Element");
            w.InvokeMenuItem("changeToElementToolStripMenuItem1");
            this.CheckOuterXml("<pi>A CDATA block</pi>");

            Trace.WriteLine("Change Element to PI");
            w.InvokeMenuItem("changeToProcessingInstructionToolStripMenuItem");
            this.CheckOuterXml("<?pi A CDATA block?>");

            Trace.WriteLine("Change PI to Text");
            w.InvokeMenuItem("changeToTextToolStripMenuItem1");
            this.CheckOuterXml("&lt;?pi A CDATA block?&gt;");

            Trace.WriteLine("Change Text to PI");
            w.InvokeMenuItem("changeToProcessingInstructionToolStripMenuItem");
            this.CheckOuterXml("<?pi A CDATA block?>");

            this.Undo(19);

            Trace.WriteLine("Now file should be identical to original");
            this.SaveAndCompare("out.xml", "test8.xml");
        }

        //==================================================================================
        private void SaveAndCompare(string outname, string compareWith) {

            string outFile = Save(outname);

            string expectedFile = TestDir + "UnitTests\\" + compareWith;
            Sleep(1000);
            CompareResults(ReadNodes(expectedFile), outFile);
        }

        private string Save(string outname) {
            Trace.WriteLine("Save");            
            string outFile = TestDir + "UnitTests\\" + outname;
            DeleteFile(outFile);
            
            // Has to be "Async" otherwise automation locks up because of the popup dialog.
            this.window.InvokeAsyncMenuItem("saveAsToolStripMenuItem");
            Window dialog = this.window.WaitForPopup();
            OpenFileDialog od = new OpenFileDialog(dialog);
            od.FileName = outFile;
            dialog.DismissPopUp("{ENTER}");

            Sleep(1000); // give it time to save.
            return outFile;
        }


        void TestHitTest(Point pt, AutomationWrapper parent, AutomationWrapper expected) {
            AutomationWrapper obj = parent.HitTest(pt.X, pt.Y);
            if (obj.Name != expected.Name) {
                throw new ApplicationException(
                    string.Format("Found node '{0}' at {1},{2} instead of node '{3}'",
                        obj.Name, pt.X.ToString(), pt.Y.ToString(), expected.Name)
                    );
            }
        }

        Point TopCenter(Rectangle bounds, int dy) {
            return new Point(bounds.Left + (bounds.Width / 2), bounds.Top + dy);
        }

        void FocusTreeView() {
            AutomationWrapper acc = this.TreeView;
            acc.SetFocus();
        }

        void CheckNodeName(string expected) {
            AutomationWrapper acc = this.TreeView;
            AutomationWrapper node = acc.GetSelectedChild();
            if (node == null) {
                throw new ApplicationException("No node selected in tre view!");
            }
            CheckNodeName(node, expected);
        }

        void CheckNodeName(AutomationWrapper acc, string expected) {
            string name = acc.Name;
            if (name != expected) {
                throw new ApplicationException(string.Format("Expecting node name '{0}', but found '{1}'", expected, name));
            }
            Trace.WriteLine("Name=" + name);
#if DEBUG
            Sleep(200); // so we can watch it!
#endif
        }

        void CheckNodeValue(AutomationWrapper acc, string expected) {
            string value = acc.Value;
            if (value != expected) {
                throw new ApplicationException(string.Format("Expecting node value '{0}'", expected));
            }
            Trace.WriteLine("Value=" + value);
#if DEBUG
            Sleep(200); // so we can watch it!
#endif
        }

        void CheckProperties(AutomationWrapper node) {
            // Get code coverage on the boring stuff.
            Trace.WriteLine("Name=" + node.Name);
            // not all nodes support a ValuePattern (comment, pi, cdata).
            //Trace.WriteLine("\tValue=" + node.Value);
            Trace.WriteLine("\tParent=" + node.Parent.Name);
            Trace.WriteLine("\tChildCount=" + node.GetChildCount());
            Trace.WriteLine("\tBounds=" + node.Bounds.ToString());
            //Trace.WriteLine("\tDefaultAction=" + node.DefaultAction);
            //Trace.WriteLine("\tDescription=" + node.Description);
            Trace.WriteLine("\tHelp=" + node.Help);
            Trace.WriteLine("\tKeyboardShortcut=" + node.KeyboardShortcut);
            Trace.WriteLine("\tRole=" + node.Role);
            //Trace.WriteLine("\tState=" + node.State);
            //string filename = null;
            //Trace.WriteLine("\tHelpTopic=" + node.GetHelpTopic(out filename));
        }

        public override void CheckClipboard(string expected) {
            int retries = 5;
            string text = null;
            while (retries-- > 0)
            {
                if (Clipboard.ContainsText())
                {
                    text = Clipboard.GetText();
                    if (IsNormalizedEqual(Clipboard.GetText(), expected))
                    {
                        return;
                    }
                }
                Sleep(250);    
            }

            if (!Clipboard.ContainsText())
            {
                throw new ApplicationException("clipboard does not contain any text!");
            } 
            AssertNormalizedEqual(""+ text, expected);
        }

        public void CheckClipboard(string expected, StringComparison comparison)
        {
            int retries = 5;
            string text = null;
            while (retries-- > 0)
            {
                if (Clipboard.ContainsText())
                {
                    text = Clipboard.GetText();
                    if (IsNormalizedEqual(text, expected, comparison))
                    {
                        return;
                    }
                }
                Sleep(250);
            }

            if (!Clipboard.ContainsText())
            {
                throw new ApplicationException("clipboard does not contain any text!");
            }
            AssertNormalizedEqual("" + text, expected, comparison);
        }

        public void CheckClipboard(Regex expected)
        {
            int retries = 5;
            while (retries-- > 0)
            {
                if (Clipboard.ContainsText())
                {
                    string text = Clipboard.GetText();
                    if (expected.IsMatch(text))
                    {
                        return;
                    }
                }
                Sleep(250);
            }

            if (!Clipboard.ContainsText())
            {
                throw new ApplicationException("clipboard does not contain any text!");
            }
            throw new ApplicationException(@"clipboard [" + Clipboard.GetText() + "] does not match expected value: [" + expected + "]");
        }

        public void AssertNormalizedEqual(string value, string expected) {
            expected = NormalizeNewLines(expected);
            string text = NormalizeNewLines(value);
            if (!IsNormalizedEqual(text, expected))
            {
                throw new ApplicationException("clipboard '[" + text + "]' does not match expected value: [" + expected + "]");
            }
        }

        public void AssertNormalizedEqual(string value, string expected, StringComparison comparison)
        {
            expected = NormalizeNewLines(expected);
            string text = NormalizeNewLines(value);
            if (!IsNormalizedEqual(text, expected, comparison))
            {
                throw new ApplicationException("clipboard '[" + text + "]' does not match expected value: [" + expected + "]");
            }
        }
        
        public bool IsNormalizedEqual(string value, string expected)
        {
            expected = NormalizeNewLines(expected);
            string text = NormalizeNewLines(value);
            return (text == expected);
        }

        public bool IsNormalizedEqual(string value, string expected, StringComparison comparison)
        {
            expected = NormalizeNewLines(expected);
            string text = NormalizeNewLines(value);
            return string.Compare(text, expected, comparison) == 0;
        }

        public static string NormalizeNewLines(string text) {
            if (text == null) return null;
            StringBuilder sb = new StringBuilder();
            for (int i = 0, n = text.Length; i < n; i++) {
                char ch = text[i];
                if (ch == '\r') {
                    if (i + 1 < n && text[i + 1] == '\n')
                        i++;
                    sb.Append("\r\n");
                } else if (ch == '\n') {
                    sb.Append("\r\n");
                } else {
                    sb.Append(ch);
                }
            }
            return sb.ToString();
        }

    }
}
