using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
// using System.Windows;
using System.Windows.Automation;
using System.Windows.Forms;
using System.Xml;
using WindowsInput;
using XmlNotepad;

// Here's a handy reference on SendKeys:
// http://msdn2.microsoft.com/en-us/library/system.windows.forms.sendkeys.aspx

namespace UnitTests
{

    [TestClass]
    public class UnitTest1 : TestBase
    {
        const int TestMethodTimeout = 300000; // 5 minutes
        private readonly string _testDir;
        private bool calibrated;
        private List<MouseCalibration> calibration;
        private Settings testSettings;

        public UnitTest1()
        {
            Uri baseUri = new Uri(this.GetType().Assembly.Location);
            Uri resolved = new Uri(baseUri, "..\\..\\..\\");
            _testDir = resolved.LocalPath;
            // Test that we can process updates and show available updates button.
            // Have to fix the location field to show the right thing.
            XmlDocument doc = new XmlDocument();
            doc.Load(_testDir + @"UnitTests\TestUpdates.xml");
            XmlElement e = doc.SelectSingleNode("updates/application/location") as XmlElement;
            string target = _testDir + @"XmlNotepad\bin\Debug\Updates.xml";
            e.InnerText = target;
            doc.Save(target);
        }

        //[ClassInitialize()]
        //public static void MyClassInitialize(TestContext testContext) {
        //}

        //[ClassCleanup()]
        //public static void MyClassCleanup() {            
        //}

        Settings LoadTestSettings()
        {
            Settings testSettings = new Settings()
            {
                Comparer = FormMain.SettingValueMatches,
                StartupPath = Application.StartupPath,
                ExecutablePath = Application.ExecutablePath
            };

            SettingsLoader ls = new SettingsLoader();
            testSettings.SetDefaults();
            ls.LoadSettings(testSettings, true);
            return testSettings;
        }

        [TestInitialize()]
        public void MyTestInitialize()
        {
            this.testSettings = LoadTestSettings();
            // Find the test settings we care about.
            var screen = Screen.PrimaryScreen.WorkingArea;
            Size s = (Size)testSettings["PrimaryScreenSize"];
            var points = (Point[])testSettings["MouseCalibration"];
            if (points.Length > 10 && s == screen.Size)
            {
                var calibration = new List<MouseCalibration>();
                for (int i = 0; i + 1 < points.Length; i += 2)
                {
                    Point expected = points[i];
                    Point actual = points[i + 1];
                    calibration.Add(new MouseCalibration() { Expected = expected, Actual = actual });
                }
                this.calibration = calibration;
                sim.Mouse.Calibrate(calibration);
                this.calibrated = true;
            }

            // reset the test settings before each test.
            testSettings.SetDefaults();

            // Now calibrate the mouse once
            if (!this.calibrated)
            {
                TestCalibrateMouse();
            }

            // Always restore the updated calibration settings.
            points = new Point[this.calibration.Count * 2];
            int pos = 0;
            foreach (var c in this.calibration)
            {
                points[pos++] = c.Expected;
                points[pos++] = c.Actual;
            }
            testSettings["PrimaryScreenSize"] = screen.Size;
            testSettings["MouseCalibration"] = points;

            // Save the reset settings so next LaunchNotepad picks them up
            testSettings.Save(testSettings.FileName);
        }

        [TestCleanup()]
        public void MyTestCleanup()
        {
            if (this.window != null)
            {
                this.window.Dispose(true);
            }
        }

        Window LaunchNotepad(bool debugMouse = false)
        {
            this.window = LaunchNotepad(null, debugMouse: debugMouse);
            if (!debugMouse)
            {
                this.window.InvokeMenuItem("newToolStripMenuItem");
            }
            Sleep(1000);
            return window;
        }

        Window LaunchNotepad(string filename, bool testSettings = true, bool debugMouse = false)
        {
            string args = "\"" + filename + "\"";
            if (testSettings)
            {
                args = "-test " + args;
            }
            if (debugMouse)
            {
                args = "-debugMouse " + args;
            }
            this.window = LaunchApp(Directory.GetCurrentDirectory() + @"\..\..\..\drop\XmlNotepad.exe", args, "FormMain");
            return window;
        }


        AutomationWrapper XmlTreeView
        {
            get
            {
                AutomationWrapper xtv = this.window.FindDescendant("xmlTreeView1");
                return xtv;
            }
        }

        AutomationWrapper TreeView
        {
            get
            {
                AutomationWrapper tv = this.window.FindDescendant("TreeView");
                return tv;
            }
        }


        AutomationWrapper NodeTextView
        {
            get
            {
                AutomationWrapper ntv = this.window.FindDescendant("NodeTextView");
                return ntv;
            }
        }

        AutomationWrapper NodeTextViewCompletionSet
        {
            get
            {
                AutomationWrapper cset = this.window.FindPopup("CompletionSet");
                if (!cset.IsVisible)
                {
                    throw new Exception("CompletionSet is not visible");
                }
                return cset;
            }
        }

        [TestMethod]
        [Timeout(TestMethodTimeout)]
        public void TestCalibrateMouse()
        {
            List<MouseCalibration> calibration = new List<MouseCalibration>();
            var screen = Screen.PrimaryScreen.WorkingArea;

            Rectangle bounds = Rectangle.Empty;
            var window = LaunchNotepad(debugMouse: true);
            window.WaitForInteractive();
            window.SetWindowPosition(screen.Left, screen.Top);
            window.SetWindowSize(screen.Width, screen.Height);
            window.WaitForInteractive();
            Sleep(1000);

            var xPosLabel = window.FindDescendant("XPosition");
            var yPosLabel = window.FindDescendant("YPosition");
            var statusBox = window.FindDescendant("Status");
            ValuePattern xPattern = null;
            ValuePattern yPattern = null;
            ValuePattern sPattern = null;
            if (xPosLabel.AutomationElement.TryGetCurrentPattern(ValuePattern.Pattern, out object o))
            {
                xPattern = (ValuePattern)o;
            }
            else
            {
                Assert.Fail("xPosition TextBox has no ValuePattern?");
            }
            if (yPosLabel.AutomationElement.TryGetCurrentPattern(ValuePattern.Pattern, out object yo))
            {
                yPattern = (ValuePattern)yo;
            }
            else
            {
                Assert.Fail("yPosition TextBox has no ValuePattern?");
            }
            if (statusBox.AutomationElement.TryGetCurrentPattern(ValuePattern.Pattern, out object so))
            {
                sPattern = (ValuePattern)so;
            }
            else
            {
                Assert.Fail("yPosition TextBox has no ValuePattern?");
            }

            bounds = window.GetClientBounds();
            var center = xPosLabel.Bounds.Center();
            var b2 = statusBox.Bounds;
            // visual check if calibration is needed
            sim.Mouse.MoveMouseTo(center.X, center.Y);
            sim.Mouse.MoveMouseTo(bounds.Left, bounds.Bottom);
            sim.Mouse.MoveMouseTo(b2.Left, b2.Bottom);

            Rectangle inner = bounds;
            inner.Inflate(-20, -20);
            int steps = 60;
            int previousX = 0;
            int previousY = 0;
            for (int i = 0; i < steps; i++)
            {
                int x = (int)(inner.Left + ((double)i * inner.Width) / steps);
                int y = (int)(inner.Top + ((double)i * inner.Height) / steps);
                sim.Mouse.MoveMouseTo(x, y);
                while (string.IsNullOrEmpty(xPattern.Current.Value) || string.IsNullOrEmpty(yPattern.Current.Value))
                {
                    sim.Mouse.MoveMouseTo(x, y);
                    Thread.Sleep(30);
                }
                int ax = previousX;
                int ay = previousY;
                // wait for fields to update
                int retries = 10;
                while ((ax == previousX || ay == previousY) && retries > 0)
                {
                    Thread.Sleep(30);
                    // winforms mouse move is relative to content not including window frame
                    ax = int.Parse(xPattern.Current.Value) + bounds.Left;
                    ay = int.Parse(yPattern.Current.Value) + bounds.Top;
                    retries--;
                }

                if (ax > inner.Right || ay > inner.Bottom || retries == 0)
                {
                    // we already stepped outside our box, so we're done!
                    break;
                }

                previousX = ax;
                previousY = ay;

                Debug.WriteLine("{0}, {1}  => {2}, {3}  => {4}, {5}", x, y, ax, ay, x - ax, y - ay);
                calibration.Add(new MouseCalibration()
                {
                    Expected = new Point(x, y),
                    Actual = new Point(ax, ay)
                });
            }

            this.calibration = calibration;
            this.sim.Mouse.Calibrate(calibration);

            // visual check if calibration is worked
            sim.Mouse.MoveMouseTo(center.X, center.Y);
            sim.Mouse.MoveMouseTo(bounds.Left, bounds.Bottom);
            sim.Mouse.MoveMouseTo(b2.Left, b2.Bottom);

            this.calibrated = true;
            // close the form
            window.Close();
        }

        [TestMethod]
        [Timeout(TestMethodTimeout)]
        public void TestUndoRedo()
        {
            Trace.WriteLine("TestUndoRedo==========================================================");
            // Since this is the first test, we have to make sure we don't load some other user settings.
            string testFile = _testDir + "UnitTests\\test1.xml";
            var w = this.LaunchNotepad(testFile);

            // test that we can cancel editing when we click New
            Sleep(500);
            w.SendKeystrokes("^IRoot{ENTER}");
            Sleep(100);
            w.InvokeMenuItem("newToolStripMenuItem");
            Sleep(500);


            Stack<bool> hasChildren = new Stack<bool>();
            XmlReader reader = XmlReader.Create(testFile);
            bool openElement = true;
            int commands = 0;
            bool readyForText = false;

            using (reader)
            {
                while (reader.Read())
                {
                    if (reader.NodeType == XmlNodeType.Whitespace ||
                        reader.NodeType == XmlNodeType.SignificantWhitespace ||
                        reader.NodeType == XmlNodeType.XmlDeclaration)
                        continue;

                    Trace.WriteLine(string.Format("Adding node type {0} with name {1} and value {2}",
                        reader.NodeType.ToString(), reader.Name, reader.Value));

                    bool children = false;
                    Trace.WriteLine(reader.NodeType + " " + reader.Name + "[" + reader.Value + "]");
                    switch (reader.NodeType)
                    {
                        case XmlNodeType.Element:
                            commands++;
                            w.InvokeMenuItem(openElement ? "elementChildToolStripMenuItem" :
                                "elementAfterToolStripMenuItem");
                            openElement = true;
                            bool isEmpty = reader.IsEmptyElement;
                            if (!isEmpty)
                            {
                                hasChildren.Push(children);
                                children = false;
                            }
                            else
                            {
                                openElement = false;
                            }
                            string name = reader.Name;
                            w.SendKeystrokes(name);
                            Sleep(20);
                            w.SendKeystrokes("{ENTER}");

                            readyForText = true;
                            bool firstAttribute = true;
                            while (reader.MoveToNextAttribute())
                            {
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
                            if (isEmpty)
                            {
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
                            if (openElement)
                            {
                                commands++;
                                if (!readyForText)
                                    w.SendKeystrokes("{TAB}{ENTER}");
                            }
                            else
                            {
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
                            if (readyForText)
                            {
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
        [Timeout(TestMethodTimeout)]
        public void TestEditCombinations()
        {
            Trace.WriteLine("TestEditCombinations==========================================================");
            // Test all the combinations of insert before, after, child stuff!
            string testFile = _testDir + "UnitTests\\test1.xml";
            var w = this.LaunchNotepad(testFile);
            w.InvokeMenuItem("newToolStripMenuItem");
            Sleep(500);

            // each node type at root level
            string[] nodeTypes = new string[] { "comment", "PI", "element", "attribute", "text", "cdata" };
            bool[] validInRoot = new bool[] { true, true, true, false, false, false };
            bool[] requiresName = new bool[] { false, true, true, true, false, false };
            string[] clips = new string[] { "<!--{1}-->", "<?{0} {1}?>", "<{0}>{1}</{0}>", "{0}=\"{1}\"", "{1}", "<![CDATA[{1}]]>" };
            nodeIndex = 0;

            for (int i = 0; i < nodeTypes.Length; i++)
            {
                string type = nodeTypes[i];
                if (validInRoot[i])
                {
                    InsertNode(type, "Child", requiresName[i], clips[i]);
                    Undo();
                    Undo();
                }
            }

            w.InvokeMenuItem("commentChildToolStripMenuItem");

            for (int i = 0; i < nodeTypes.Length; i++)
            {
                string type = nodeTypes[i];
                if (validInRoot[i])
                {
                    InsertNode(type, "After", requiresName[i], clips[i]);
                    if (type != "element")
                    {
                        InsertNode(type, "Before", requiresName[i], clips[i]);
                    }
                }
            }
            w.SendKeystrokes("^Ielement");

            // test all combinations of child elements under root element
            for (int i = 0; i < nodeTypes.Length; i++)
            {
                string type = nodeTypes[i];
                InsertNode(type, "Child", requiresName[i], clips[i]);
                InsertNode(type, "After", requiresName[i], clips[i]);
                InsertNode(type, "Before", requiresName[i], clips[i]);
                w.SendKeystrokes("{LEFT}{LEFT}"); // go back up to element.
            }
            this.SaveAndCompare("out.xml", "test7.xml");

        }

        int nodeIndex = 0;

        private void InsertNode(string type, string mode, bool requiresName, string clip)
        {
            string command = type + mode + "ToolStripMenuItem";
            Trace.WriteLine(command);
            this.window.InvokeMenuItem(command);
            string name = type + nodeIndex.ToString();
            if (requiresName)
            {
                this.window.SendKeystrokes(name);
                this.window.SendKeystrokes("{TAB}");
            }
            string value = mode;
            this.window.SendKeystrokes(value + "{ENTER}");
            var result = string.Format(clip, name, value);
            this.window.InvokeMenuItem("toolStripButtonCopy");
            CheckClipboard(result);
            Clipboard.SetText("error");
            UndoRedo(2);
            this.window.InvokeMenuItem("toolStripButtonCopy");
            CheckClipboard(result);

            nodeIndex++;
        }

        /// <summary>
        /// Gets the value of whatever is selected by putting it in edit mode.
        /// This works on the tree view and the node text view depending on which
        /// has the focus.
        /// </summary>
        void CheckNodeValue(string expected)
        {
            CheckNodeValue(expected, StringComparison.Ordinal);
        }

        /// <summary>
        /// Gets the value of whatever is selected by putting it in edit mode.
        /// This works on the tree view and the node text view depending on which
        /// has the focus.
        /// </summary>
        void CheckNodeValue(string expected, StringComparison comparison)
        {
            if (!Window.GetForegroundWindowText().StartsWith("XML Notepad"))
            {
                this.window.Activate();
                Sleep(500);
            }
            // must not be a leaf node then...
            Sleep(300);
            SendKeys.SendWait("{ENTER}");
            Sleep(300);
            SendKeys.SendWait("^c");
            CheckClipboard(expected, comparison);
            Sleep(300);
            SendKeys.SendWait("{ENTER}");
            Sleep(300);
        }

        internal void CheckOuterXml(string expected)
        {
            this.window.SendKeystrokes("^c");
            CheckClipboard(expected);
        }

        [TestMethod]
        [Timeout(TestMethodTimeout)]
        public void TestIntellisense()
        {
            Trace.WriteLine("TestIntellisense==========================================================");
            var w = LaunchNotepad();

            Trace.WriteLine("Add <Basket>");
            w.InvokeMenuItem("elementChildToolStripMenuItem");
            Sleep(500);
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
            Sleep(500); // wait for keys to settle.

            Trace.WriteLine("Get intellisense tooltip");
            AutomationWrapper xtv = this.XmlTreeView;
            xtv.SetFocus();

            Rectangle treeBounds = xtv.Bounds;
            w.Activate();
            sim.Mouse.MoveMouseTo(treeBounds.Left + 80, treeBounds.Top + 10);
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
            w.Activate();
            sim.Mouse.MoveMouseTo(bounds.Left + 20, bounds.Top - 10).LeftButtonClick();
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
            var openDialog = new FileDialogWrapper(popup);
            openDialog.DismissPopUp(_testDir + "UnitTests\\" + "test1.xml{ENTER}");

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
            popup.DismissPopUp("{ENTER}");

            // font dialog selects font different sizes depending on DPI setting
            // so we have to edit this value back to plain "Arial".
            w.SendKeystrokes("{DOWN}{ENTER}");
            Sleep(500);//just so I can see it

            Trace.WriteLine("Add <vegetable>cucumber</vegetable> ");
            w.InvokeMenuItem("elementAfterToolStripMenuItem");
            w.SendKeystrokes("v{TAB}cu{ENTER}");

            Trace.WriteLine("Add <berry>huckleberry</berry> ");
            w.InvokeMenuItem("elementAfterToolStripMenuItem");
            w.SendKeystrokes("b{TAB}hu{ENTER}");

            Trace.WriteLine("Test edit of PI name");
            w.InvokeMenuItem("PIAfterToolStripMenuItem");
            Sleep(200);
            w.SendKeystrokes("test{ENTER}");
            Sleep(100);
            w.SendKeystrokes("{ENTER}");
            Sleep(200);
            w.SendKeystrokes("{LEFT}");
            Sleep(200);
            w.SendKeystrokes("{ENTER}pi");
            Sleep(200);
            // bugbug: app is sometimes receiging the ENTER before the end of the text "pi"
            // which seems like a regression in windows accessibility if you ask me.
            w.SendKeystrokes("{ENTER}");
            Sleep(200);
            UndoRedo();

            Trace.WriteLine("Test validation error and elementBefore command!");
            w.InvokeMenuItem("elementBeforeToolStripMenuItem");
            Sleep(100);
            w.SendKeystrokes("woops{ENTER}");
            Sleep(500);//just so I can see it

            Trace.WriteLine("Move to Basket element");
            w.SendKeystrokes("{ESC}");
            Sleep(500);
            w.SendKeystrokes("{LEFT}{LEFT}");
            Sleep(1000);
            Trace.WriteLine("Navigate to next error");
            NavigateNextError();
            Sleep(100);
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

        private void NavigateErrorWithMouse()
        {
            AutomationWrapper grid = this.window.FindDescendant("DataGridView");
            AutomationWrapper row = grid.FirstChild;
            row = row.NextSibling;
            Point pt = row.Bounds.Center();
            // Double click it
            sim.Mouse.MoveMouseTo(pt.X, pt.Y).LeftButtonDoubleClick();
        }

        private void NavigateNextError()
        {
            this.window.InvokeMenuItem("nextErrorToolStripMenuItem");
        }

        private void Undo(int count)
        {
            while (count-- > 0)
            {
                Undo();
            }
        }

        private void Redo(int count)
        {
            while (count-- > 0)
            {
                Redo();
            }
        }

        private void Undo()
        {
            this.window.InvokeMenuItem("undoToolStripMenuItem");
        }
        private void Redo()
        {
            this.window.InvokeMenuItem("redoToolStripMenuItem");
        }
        private void UndoRedo(int level)
        {
            for (int i = 0; i < level; i++)
            {
                Undo();
            }
            for (int i = 0; i < level; i++)
            {
                Redo();
            }
        }
        private void UndoRedo()
        {
            Undo();
            Redo();
        }

        Rectangle GetXmlBuilderBounds()
        {
            return NodeTextViewCompletionSet.Bounds;
        }

        Window ClickXmlBuilder()
        {
            // Find the intellisense button and click on it
            var w = NodeTextViewCompletionSet;
            Rectangle bounds = w.Bounds;
            Sleep(1000);
            sim.Mouse.MoveMouseTo(bounds.Left + 15, bounds.Top + 10).LeftButtonClick();
            Sleep(100);
            return this.window.WaitForPopup(w.Hwnd);
        }


        [TestMethod]
        [Timeout(TestMethodTimeout)]
        public void TestCompare()
        {
            Trace.WriteLine("TestCompare==========================================================");
            string testFile = _testDir + "UnitTests\\test4.xml";
            var w = LaunchNotepad(testFile);

            // something the same
            w.InvokeAsyncMenuItem("compareXMLFilesToolStripMenuItem");
            Window openDialog = w.WaitForPopup();
            openDialog.SendKeystrokes(_testDir + "UnitTests\\test4.xml{ENTER}");
            Window msgBox = w.WaitForPopup();
            string text = msgBox.GetWindowText();
            Assert.AreEqual<string>(text, "XML Diff Error");
            msgBox.SendKeystrokes("{ENTER}");

            // the file open dialog will reopen...
            openDialog = w.WaitForPopup();
            openDialog.SendKeystrokes(_testDir + "UnitTests\\test5.xml{ENTER}");

            Window browser = w.WaitForPopup();
            browser.DismissPopUp("%{F4}");

            Undo();
        }

        [TestMethod]
        [Timeout(TestMethodTimeout)]
        public void TestClipboard()
        {
            Trace.WriteLine("TestClipboard==========================================================");

            string testFile = _testDir + "UnitTests\\test1.xml";
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

        void WipeFile(string fname)
        {
            if (File.Exists(fname))
            {
                File.SetAttributes(fname, File.GetAttributes(fname) & ~FileAttributes.ReadOnly);
                File.Delete(fname);
            }
        }

        [TestMethod]
        [Timeout(TestMethodTimeout)]
        public void TestOptionsDialog()
        {
            Trace.WriteLine("TestOptionsDialog==========================================================");

            var w = LaunchNotepad();

            // Options dialog
            Trace.WriteLine("Options dialog...");
            Window options = w.OpenDialog("optionsToolStripMenuItem", "FormOptions");

            // Find the PropertyGrid control.
            AutomationWrapper acc = options.FindDescendant("propertyGrid1");

            AutomationWrapper table = acc.FindChild("Properties Window");

            ScrollAutomationWrapper scrollbar = table.FindScroller();
            scrollbar.Simulator = this.sim;

            Trace.WriteLine("Font");
            AutomationWrapper font = table.FindChild("Font"); // this is the group heading
            scrollbar.ScrollIntoView(font, table);

            Rectangle r = font.Bounds;
            // bring up the font dialog.
            var pt = new Point(r.Right - 10, r.Top + r.Height / 2);
            options.Activate();
            sim.Mouse.MoveMouseTo(pt.X, pt.Y).LeftButtonClick();
            Sleep(500);
            Rectangle r2 = font.Bounds;
            if (r2 != r)
            {
                // this happens if the item was the very bottom of the scrollbox and
                // so it scrolls up a bit when it is selected.
                r = r2;
                pt = new Point(r.Right - 10, r.Top + r.Height / 2);
                sim.Mouse.MoveMouseTo(pt.X, pt.Y).LeftButtonClick();
            }
            Sleep(500);
            sim.Mouse.MoveMouseTo(pt.X, pt.Y).LeftButtonClick();
            Window popup = options.WaitForPopup();
            popup.DismissPopUp("{ENTER}");

            string[] names = new string[] { "Element", "Attribute", "Text",
                    "Background", "Comment", "PI", "CDATA" };

            string[] values = new string[] { "Aqua", "128, 64, 64", "64, 0, 0",
                  "64, 0, 128", "Lime", "128, 0, 64", "0, 64, 64"};


            for (int i = 0, n = names.Length; i < n; i++)
            {
                string name = names[i];

                Trace.WriteLine("Click " + name);

                AutomationWrapper child = table.FindChild(name);
                scrollbar.ScrollIntoView(child, table);

                r = child.Bounds;
                sim.Mouse.MoveMouseTo(r.Left + 20, r.Top + 6).LeftButtonClick();
                Sleep(100);
                popup.SendKeystrokes("{TAB}" + values[i] + "{ENTER}");

                Sleep(333); // so we can see it!
            }

            popup.DismissPopUp("%O");
            bool passed = true;

            // Close the app.
            w.Dispose();

            Sleep(2000); // give it time to write out the new settings.

            // verify persisted colors.
            this.testSettings = LoadTestSettings();
            ThemeColors colors = (ThemeColors)this.testSettings["LightColors"];

            Color[] found = new Color[]
            {
                colors.Element, colors.Attribute, colors.Text, colors.Background, colors.Comment, colors.PI, colors.CDATA
            };
            TypeConverter tc = TypeDescriptor.GetConverter(typeof(Color));

            for (int i = 0, n = names.Length; i < n; i++)
            {
                string ename = names[i];
                var expected = values[i];
                var actual = tc.ConvertToString(found[i]);
                if (expected != actual)
                {
                    Trace.WriteLine(string.Format("Color '{0}' has unexpected value '{1}'", ename, actual));
                    passed = false;
                }
            }

            if (!passed)
            {
                throw new ApplicationException("Unexpected colors found in XmlNotepad.settings file.");
            }
        }

        [TestMethod]
        [Timeout(TestMethodTimeout)]
        public void TestDialogs()
        {
            // ensure we get a horizontal scroll bar on the supply.xml file.
            SetTreeViewWidth(290);

            Trace.WriteLine("TestDialogs==========================================================");
            string testFile = _testDir + "UnitTests\\supply.xml";
            var w = LaunchNotepad(testFile);

            // Window/NewWindow!
            Trace.WriteLine("Window/NewWindow");
            w.InvokeAsyncMenuItem("newWindowToolStripMenuItem");
            Window popup = w.WaitForPopup();
            Debug.WriteLine("Found popup " + popup.GetWindowText());
            popup.DismissPopUp("%{F4}"); // close second window!

            if (!Window.GetForegroundWindowText().StartsWith("XML Notepad"))
            {
                w.Activate(); // alt-f4 sometimes sends focus to another window (namely, the VS process running this test!)
                Sleep(500);
            }
            Sleep(1000);

            // About...
            Trace.WriteLine("About...");
            w.InvokeAsyncMenuItem("aboutXMLNotepadToolStripMenuItem");
            popup = w.WaitForPopup();
            popup.DismissPopUp("{ENTER}");

            // hide/show status bar
            Trace.WriteLine("hide/show status bar...");
            w.InvokeAsyncMenuItem("statusBarToolStripMenuItem");
            Sleep(500);
            w.InvokeAsyncMenuItem("statusBarToolStripMenuItem");
            Sleep(500);

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

            this.TreeView.SetFocus();
            w.SendKeystrokes("{TAB}");

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

            // XmlDocument lazily creates namespace nodes causing this test to "modify" the document!
            Save("out.xml");
        }


        [TestMethod]
        [Timeout(TestMethodTimeout)]
        public void TestSaveLoad()
        {
            // ensure we get a horizontal scroll bar on the supply.xml file.
            SetTreeViewWidth(290);

            Trace.WriteLine("TestDialogs==========================================================");
            var w = LaunchNotepad();

            // open bad file.            
            Trace.WriteLine("open bad file");
            w.InvokeAsyncMenuItem("openToolStripMenuItem");
            FileDialogWrapper fd = w.WaitForFileDialog();
            fd.SendKeystrokes(_testDir + "UnitTests\\bad.xml{ENTER}");
            Window popup = w.WaitForPopup();
            popup.SendKeystrokes("%Y");
            Window notepad = w.WaitForPopup();
            notepad.DismissPopUp("%{F4}");

            // Test OpenFileDialog
            Trace.WriteLine("OpenFileDialog");
            w.InvokeAsyncMenuItem("openToolStripMenuItem");
            fd = w.WaitForFileDialog();
            var filename = _testDir + "UnitTests\\supply.xml";
            fd.DismissPopUp(filename + "{ENTER}");

            // make an edit.
            this.TreeView.SetFocus();
            w.SendKeystrokes("{END}{RIGHT}{DOWN}{DOWN}{ENTER}");
            Sleep(100);
            w.SendKeystrokes("FooBar{ENTER}");

            // Test reload - discard changes
            Trace.WriteLine("Reload- discard changes");
            w.InvokeAsyncMenuItem("reloadToolStripMenuItem");
            popup = w.WaitForPopup();
            popup.DismissPopUp("{ENTER}");

            // Save As...
            Trace.WriteLine("Save As...");
            string outFile = _testDir + "UnitTests\\out.xml";
            WipeFile(outFile);
            w.InvokeAsyncMenuItem("saveAsToolStripMenuItem");
            fd = w.WaitForFileDialog();
            fd.DismissPopUp("out.xml{ENTER}");

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

            // make document dirty again
            this.TreeView.SetFocus();
            w.SendKeystrokes("{END}{RIGHT}{DOWN}{DOWN}{ENTER}");
            Sleep(100);
            w.SendKeystrokes("FooBar{ENTER}");

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
            CheckNodeName("FooBar");
            w.InvokeAsyncMenuItem("exitToolStripMenuItem");
            popup = w.WaitForPopup();

            // save the changes!
            w.Closed = true;
            popup.SendKeystrokes("%Y");
        }

        [TestMethod]
        [Timeout(TestMethodTimeout)]
        public void TestSchemaDialog()
        {
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
            FileDialogWrapper fd = new FileDialogWrapper(fileDialog);
            string schema = _testDir + "UnitTests\\emp.xsd";
            fd.DismissPopUp(schema + "{ENTER}");

            schemaDialog.SendKeystrokes("^{HOME}+ "); // select first row
            Sleep(300); // just so we can watch it happen
            schemaDialog.SendKeystrokes("^c"); // copy
            string text = Clipboard.GetText();
            if (!text.ToLowerInvariant().Contains("emp.xsd"))
            {
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
            if (!text.ToLowerInvariant().Contains("emp.xsd"))
            {
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
            schema = _testDir + "UnitTests\\emp2.xsd";
            schemaDialog.SendKeystrokes("{DOWN}{RIGHT}{RIGHT}^ "); // select first row
            Clipboard.SetText(schema);
            schemaDialog.SendKeystrokes("^v");
            schemaDialog.SendKeystrokes("^c"); // copy
            text = Clipboard.GetText();
            if (!text.ToLowerInvariant().Contains("emp2.xsd"))
            {
                throw new ApplicationException("Did not find 'test2.xsd' on the clipboard!");
            }

            Trace.WriteLine("Add duplicate schema via file dialog ");
            Sleep(1000);
            schemaDialog.InvokeAsyncMenuItem("addSchemasToolStripMenuItem");

            fileDialog = schemaDialog.WaitForPopup();
            fd = new FileDialogWrapper(fileDialog);
            schema = _testDir + "UnitTests\\emp.xsd";
            fd.DismissPopUp(schema + "{ENTER}");

            Sleep(300); // just so we can watch it happen
            schemaDialog.SendKeystrokes("^c"); // copy first row
            text = Clipboard.GetText();
            if (!text.ToLowerInvariant().Contains("emp.xsd"))
            {
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
            this.window.Activate();
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
        [Timeout(TestMethodTimeout)]
        public void TestXPathFind()
        {
            Trace.WriteLine("TestXPathFind==========================================================");
            string testFile = _testDir + "UnitTests\\test1.xml";
            var w = LaunchNotepad(testFile);

            Sleep(1000);

            Trace.WriteLine("test path of 'pi' node");
            w.SendKeystrokes("^Ipi");

            FindDialog fd = OpenFindDialog();
            fd.ClearFindCheckBoxes();
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
            Sleep(100);
            fd.Window.DismissPopUp("{ESC}");
            Sleep(100);

            Trace.WriteLine("test 'id' attribute path generation.");
            this.TreeView.SetFocus();
            w.SendKeystrokes("{ESC}");
            Sleep(100);
            w.SendKeystrokes("{DOWN}");
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
        [Timeout(TestMethodTimeout)]
        public void TestXsltOutput()
        {
            Trace.WriteLine("TestXsltOutput==========================================================");

            var w = LaunchNotepad();

            MainWindowWrapper xw = new MainWindowWrapper(w);
            xw.LoadXmlAddress(_testDir + "Application\\Samples\\rss.xml", "<?xml version=\"1.0\" encoding=\"utf-8\"?>");

            Trace.WriteLine("Show XSLT");
            xw.ShowXslt();

            xw.CopyHtml();
            this.CheckClipboard(new Regex(@"RSS Feed for MSDN Just Published"));

            // make sure "xsl-output" tag was honored.
            string path = xw.GetXmlOutputFilename();
            Assert.AreEqual(Path.GetFileName(path), "rss.htm");

            Trace.WriteLine("Enter custom XSL with script code.");
            xw.EnterXslFilename(_testDir + "UnitTests\\rss.xsl");
            Window popup = w.WaitForPopup();
            string title = Window.GetForegroundWindowText();
            if (title != "Untrusted Script Code")
            {
                throw new ApplicationException("Expecting script security dialog");
            }
            Sleep(1000);
            popup.DismissPopUp("%Y");

            Trace.WriteLine("Make sure it executed");
            xw.CopyHtml();
            this.CheckClipboard(new Regex(@"Found [\d]* RSS items. The script executed successfully."));

            Trace.WriteLine("Try xslt with error");
            xw.EnterXslFilename(_testDir + "UnitTests\\bad.xsl");
            Sleep(2000);
            xw.CopyHtml();
            this.CheckClipboard(@"Error Transforming XML 
Prefix 'user' is not defined. ");

            Trace.WriteLine("Back to tree view");
            xw.ShowXmlTree();

            // test http transforms
            w.InvokeAsyncMenuItem("toolStripButtonNew");
            Trace.WriteLine("Make sure we can transform XML from HTTP locations");
            xw.LoadXmlAddress("https://lovettsoftwarestorage.blob.core.windows.net/downloads/XmlNotepad/Updates.xml", "<?xml version=\"1.0\" encoding=\"utf-8\"?>");

            Trace.WriteLine("Show XSLT");
            xw.ShowXslt();

            xw.CopyHtml();
            this.CheckClipboard(new Regex(@"Microsoft XML Notepad - Change History"));

            string outputFilename = "updates.htm";
            var tempOutput = Path.Combine(Path.GetTempPath(), outputFilename);
            if (File.Exists(tempOutput))
            {
                File.Delete(tempOutput);
            }

            Trace.WriteLine("Make sure we can transform to local temp file");
            xw.EnterXmlOutputFilename(outputFilename);

            xw.InvokeTransformButton();

            var output = File.ReadAllText(tempOutput);
            Assert.IsTrue(output.Contains("Microsoft XML Notepad - Change History"));

            // make sure we can exit without any save changes dialog, since none of this should have edited any XML.
        }

        [TestMethod]
        [Timeout(TestMethodTimeout)]
        public void TestFind()
        {
            Trace.WriteLine("TestFind==========================================================");
            string testFile = _testDir + "UnitTests\\test1.xml";
            var w = LaunchNotepad(testFile);

            Trace.WriteLine("Test auto-move of Find Window to reveal what was found");
            Rectangle treeBounds = this.XmlTreeView.Bounds;

            var findDialog = OpenFindDialog();

            Rectangle findBounds = findDialog.Window.GetScreenBounds();
            Point treeCenter = treeBounds.Center();
            Point findCenter = findBounds.Center();
            Point start = new Point(findBounds.Left + (findBounds.Width / 2), findBounds.Top + 15);
            Point end = new Point(start.X + treeCenter.X - findCenter.X,
                                  start.Y + treeCenter.Y - findCenter.Y);
            sim.Mouse.LeftButtonDragDrop(start.X, start.Y, end.X, end.Y, 5, 1);

            // Refocus the combo box...
            Sleep(500);
            findDialog.FocusFindString();

            Sleep(500);
            // check we can find attribute values!
            findDialog.Window.SendKeystrokes("foo{ENTER}");
            Sleep(500);

            findDialog.Window.DismissPopUp("{ESC}");
            w.SendKeystrokes("^c{ESC}");
            CheckClipboard("foo");
            Sleep(200);
            w.SendKeystrokes("^{HOME}");

            Trace.WriteLine("Test find error dialog");
            findDialog = OpenFindDialog();
            findDialog.Window.SendKeystrokes("will not find{ENTER}");
            Window popup = w.ExpectingPopup("Find Error");
            popup.DismissPopUp("{ENTER}");

            Sleep(200);
            Trace.WriteLine("test we can find the 'this' text twice in one paragraph.");
            findDialog.Window.SendKeystrokes("this{ENTER}");
            Sleep(200);
            findDialog.Window.DismissPopUp("{ESC}");
            Sleep(200);
            w.SendKeystrokes("^c");
            CheckClipboard("this");
            Trace.WriteLine("repeat find with shortcut");
            w.SendKeystrokes("{F3}");
            Sleep(200);
            w.SendKeystrokes("^c");
            CheckClipboard("This");
            w.SendKeystrokes("{ESC}");
            Sleep(200);
            w.SendKeystrokes("^c");
            // make sure we are on the right node.
            this.CheckClipboard(new Regex(@".*Copyright © 1999 Jon Bosak.*"));

            Trace.WriteLine("Test illegal regular expressions.");
            findDialog = OpenFindDialog();
            findDialog.UseRegex = false; // make sure %e turns it on!
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
            ;
            // find should not modify the document, so we should be able to exit without saveas dialog.
        }

        [TestMethod]
        [Timeout(TestMethodTimeout)]
        public void TestFindNested()
        {
            Trace.WriteLine("TestReplace==========================================================");
            string testFile = _testDir + "UnitTests\\test12.xml";
            var w = LaunchNotepad(testFile);

            var findDialog = OpenFindDialog();
            findDialog.FindString = "item";

            List<string> found = new List<string>();

            for (int i = 0; i < 8; i++)
            {
                w.Activate();
                findDialog.FindNext();
                Sleep(100);
                w.WaitForInteractive();
                var node = this.NodeTextView.GetSelectedChild();
                var value = node.SimpleValue;
                if (!string.IsNullOrEmpty(value)) 
                    found.Add(value);
            }

            string[] expected = new string[] { "Apple", "Banana", "Grape", "Peach",
                "This contains the 'item' text also",
                "This contains the 'item' text also", "Watermelon" };

            this.AssertArraysEqual(expected, found.ToArray());
        }

        [TestMethod]
        [Timeout(TestMethodTimeout)]
        public void TestReplaceMany()
        {
            Trace.WriteLine("TestReplace==========================================================");
            string testFile = _testDir + "UnitTests\\test10.xml";
            var w = LaunchNotepad(testFile);

            w.SendKeystrokes("{HOME}");
            w.SendKeystrokes("^c");
            var original = GetClipboardText();
            var findDialog = OpenReplaceDialog();

            // replace all instances of "item" with something longer "xxxxxxx";
            findDialog.Window.SendKeystrokes("item{TAB}xxxxxxx%a");
            findDialog.Window.DismissPopUp("{ESC}");

            w.SendKeystrokes("{ESC}{HOME}");
            CheckOuterXml(original.Replace("item", "xxxxxxx"));

            Trace.WriteLine("Check compound undo.");
            Undo();
            w.SendKeystrokes("{HOME}");
            CheckOuterXml(original);

            findDialog = OpenReplaceDialog();

            // replace all instances of "item" with something shorter "YY";
            findDialog.Window.SendKeystrokes("item{TAB}YY%a");
            findDialog.Window.DismissPopUp("{ESC}");

            w.SendKeystrokes("{ESC}{HOME}");
            CheckOuterXml(original.Replace("item", "YY"));

            Trace.WriteLine("Check compound undo.");
            Undo();
            w.SendKeystrokes("{HOME}");
            CheckOuterXml(original);
        }

        [TestMethod]
        [Timeout(TestMethodTimeout)]
        public void TestReplace()
        {
            Trace.WriteLine("TestReplace==========================================================");
            string testFile = _testDir + "UnitTests\\test1.xml";
            var w = LaunchNotepad(testFile);

            w.SendKeystrokes("{HOME}");
            var findDialog = OpenReplaceDialog();

            Trace.WriteLine("Toggle dialog using ctrl+f & ctrl+h");
            findDialog.Window.SendKeystrokes("^f");
            Sleep(300); // so I can see it...
            findDialog.Window.SendKeystrokes("^h");
            Sleep(300);

            Trace.WriteLine("test we can replace 'This' using case sensitive.");
            findDialog.Window.SendKeystrokes("This{TAB}xxx{TAB}%m%w{TAB}{TAB}{TAB}e%a");
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

            Trace.WriteLine("Check compound undo.");
            Undo();
            var original = @"
    The XML markup in this version is Copyright © 1999 Jon Bosak.
    This work may freely be distributed on condition that it not be
    modified or altered in any way.
    ";
            CheckOuterXml(original);

            Trace.WriteLine("Failed replace, via replace button");
            w.SendKeystrokes("{HOME}");
            findDialog = OpenReplaceDialog();
            findDialog.Window.SendKeystrokes("will not find%r");
            var popup = findDialog.Window.ExpectingPopup("Replace Error");
            popup.DismissPopUp("{ENTER}");

            Trace.WriteLine("Test we can replace 2 things in sequence");
            w.SendKeystrokes("{HOME}");
            findDialog.Window.SendKeystrokes("XML{TAB}XXXXX{TAB}");
            findDialog.Window.SendKeystrokes("%m"); // match case
            findDialog.Window.SendKeystrokes("%r"); // find first change
            findDialog.Window.SendKeystrokes("%r"); // make the first change
            findDialog.Window.SendKeystrokes("%r"); // make the second change
            popup = findDialog.Window.ExpectingPopup("Replace Complete");
            popup.DismissPopUp("{ENTER}");
            findDialog.Window.DismissPopUp("{ESC}");

            // hack: weird windows 11 bug causes a focus problem after editing a node
            // the Escape key fixes it.
            window.SendKeystrokes("{ESC}");
            CheckOuterXml(@"
    The XXXXX markup in this version is Copyright © 1999 Jon Bosak.
    This work may freely be distributed on condition that it not be
    modified or altered in any way.
    ");

            Undo();
            Undo(); // should move us back to the previous paragraph
            Redo(); // check that this replace operation actually worked.
            CheckOuterXml(@"XXXXX version by Jon Bosak, 1996-1999.");

            Sleep(1000);
            Save("out.xml");

            w.Dispose();
            Sleep(2000);

        }

        [TestMethod]
        [Timeout(TestMethodTimeout)]
        public void TestReplaceBackwards()
        {
            Trace.WriteLine("TestReplaceBackwards==========================================================");
            string testFile = _testDir + "UnitTests\\test1.xml";
            var w = LaunchNotepad(testFile);

            w.SendKeystrokes("{HOME}");
            var findDialog = OpenReplaceDialog();
            findDialog.ClearFindCheckBoxes();

            Trace.WriteLine("Test we can replace 2 things in backwards sequence");
            w.SendKeystrokes("{HOME}");
            findDialog.Window.SendKeystrokes("XML{TAB}XXXXX%m%w%u%r");
            Sleep(100);
            findDialog.Window.SendKeystrokes("%r"); // make the first change
            Sleep(100);
            findDialog.Window.SendKeystrokes("%r"); // make the second change
            var popup = findDialog.Window.ExpectingPopup("Replace Complete");
            popup.DismissPopUp("{ENTER}");
            findDialog.Window.DismissPopUp("{ESC}");

            // hack: weird windows 11 bug causes a focus problem after editing a node
            // the Escape key fixes it.
            window.SendKeystrokes("{ESC}");
            CheckOuterXml(@"XXXXX version by Jon Bosak, 1996-1999.");

            Undo();
            Undo(); // should move us back to the previous paragraph
            Redo(); // check that this replace operation actually worked.
            CheckOuterXml(@"
    The XXXXX markup in this version is Copyright © 1999 Jon Bosak.
    This work may freely be distributed on condition that it not be
    modified or altered in any way.
    ");

            Sleep(1000);
            Save("out.xml");

            w.Dispose();
            Sleep(2000);
        }

        void SetTreeViewWidth(int width)
        {
            this.testSettings["TreeViewSize"] = width;
            this.testSettings.Save(this.testSettings.FileName);
        }

        [TestMethod]
        [Timeout(TestMethodTimeout)]
        public void TestToolbarAndContextMenus()
        {
            Trace.WriteLine("TestToolbarAndContextMenus==========================================================");

            string testFile = _testDir + "UnitTests\\test1.xml";
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
        [Timeout(TestMethodTimeout)]
        public void TestNudge()
        {
            Trace.WriteLine("TestNudge==========================================================");
            string testFile = _testDir + "UnitTests\\test1.xml";
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
        [Timeout(TestMethodTimeout)]
        public void TestDragDrop()
        {
            Trace.WriteLine("TestDragDrop==========================================================");
            var w = this.LaunchNotepad();

            Rectangle treeBounds = this.TreeView.Bounds;

            Trace.WriteLine("OpenFileDialog");
            w.InvokeAsyncMenuItem("openToolStripMenuItem");
            var openDialog = w.WaitForFileDialog();
            Trace.WriteLine("Opening '" + _testDir + "UnitTests'");
            openDialog.SendKeystrokes(_testDir + "UnitTests{ENTER}");
            Sleep(1000);

            // Drag/drop from open file dialog into xml notepad client area.
            Point drop = GetDropSpot(openDialog.Window, treeBounds);
            Sleep(500); // give time for window to update before starting drag/drop
            Trace.WriteLine("Drop spot = " + drop.ToString());

            var item = openDialog.GetFileItem("test1.xml");
            if (item == null)
            {
                // try finding the item using the keyboard.
                throw new Exception("File item not found");
            }

            Rectangle ibounds = item.Bounds;
            Point iloc = new Point(ibounds.Left + 10, ibounds.Top + 10);
            Trace.WriteLine("Dragging from " + iloc.ToString());
            w.Activate();
            sim.Mouse.LeftButtonDragDrop(iloc.X, iloc.Y, drop.X, drop.Y, 5, 1);
            Sleep(500);
            openDialog.DismissPopUp("{ESC}");

            // need bigger window to test drag/drop
            w.SetWindowSize(800, 600);

            w.InvokeMenuItem("collapseAllToolStripMenuItem");
            w.InvokeMenuItem("expandAllToolStripMenuItem");

            // Test mouse wheel
            AutomationWrapper tree = this.TreeView;
            CheckProperties(tree);

            w.SendKeystrokes("{HOME}");
            // AutomationElement returns physical coords, but Cursor.Position wants Logical coords.
            var pos = w.AccessibleObject.PhysicalToLogicalPoint(tree.Bounds.Center());
            Cursor.Position = pos;
            Sleep(500); // wait for focus to kick in before sending mouse events.
            sim.Mouse.MoveMouseTo(pos.X, pos.Y).VerticalScroll(-15);
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
            Point center = bounds.Center();
            sim.Mouse.MoveMouseTo(center.X, center.Y).LeftButtonClick();

            // test edit of node value using AccessibilityObject
            string office = "35/1682";
            node.Value = office;

            // for some odd reason the paste expands the element.
            Sleep(300);
            CheckNodeValue(office);  // confirm via copy operation

            node = tree.GetSelectedChild();
            if (node == null)
            {
                throw new ApplicationException("Selected node not found");
            }
            CheckProperties(node);
            CheckNodeName(node, "Office");
            node.AddToSelection();

            // test edit of node name using accessibility.
            this.SetNodeName("MyOffice");
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
            sim.Mouse.MoveMouseTo(pt.X, pt.Y).LeftButtonClick();
            Sleep(200);
            node = tree.GetSelectedChild();
            CheckNodeName(node, "Country");

            Trace.WriteLine("Drag/drop country up 3 items");
            Mouse.AvoidDoubleClick(); // avoid double click by delaying next click

            bounds = node.Bounds;
            pt = bounds.Center();
            Point endPt = new Point(pt.X, pt.Y - (int)(3 * itemHeight));
            // Drag the node up three slots.
            sim.Mouse.LeftButtonDragDrop(pt.X, pt.Y, endPt.X, endPt.Y, 5, 10);

            Sleep(200);

            node = tree.GetSelectedChild();
            CheckNodeName(node, "Country");

            // Drag/drop to auto scroll, then leave the window and drop it on desktop
            Rectangle formBounds = w.GetScreenBounds();

            // Autoscroll
            Point treeTop = TopCenter(tree.Bounds, 2);
            Trace.WriteLine("--- Drag to top of tree view ---");
            sim.Mouse.LeftButtonDragDrop(endPt.X, endPt.Y, treeTop.X, treeTop.Y + itemHeight, 5, 10);
            Sleep(1000); // autoscroll time.

            // Drag out of tree view.
            Point titleBar = TopCenter(formBounds, 20);
            Trace.WriteLine("--- Drag to titlebar ---");
            w.Activate();
            bounds = node.Bounds;
            pt = bounds.Center();
            sim.Mouse.LeftButtonDragDrop(pt.X, pt.Y, titleBar.X, titleBar.Y, 5, 10);
            Sleep(1000); // should now have 'no drop icon'.

            this.SaveAndCompare("out.xml", "test11.xml");
        }

        [TestMethod]
        [Timeout(TestMethodTimeout)]
        public void TestResizePanes()
        {
            Trace.WriteLine("TestResizePanes==========================================================");
            var w = this.LaunchNotepad();

            Sleep(1000);
            Trace.WriteLine("Test task list resizers");
            AutomationWrapper resizer = w.FindDescendant("TaskResizer");
            Trace.WriteLine(resizer.Parent.Name);
            var bounds = resizer.Bounds;
            Point mid = bounds.Center();
            sim.Mouse.MoveMouseTo(mid.X, mid.Y);

            // Drag the resizer up a few pixels.
            sim.Mouse.LeftButtonDragDrop(mid.X, mid.Y, mid.X, mid.Y - 20, 5, 1);
            Sleep(100);
            var newbounds = resizer.Bounds;
            Assert.IsTrue(newbounds.Center().Y < mid.Y);

            Trace.WriteLine("Test tree view resizer");
            resizer = w.FindDescendant("XmlTreeResizer");
            Trace.WriteLine(resizer.Parent.Name);
            bounds = resizer.Bounds;
            mid = bounds.Center();
            // Drag the resizer right a few pixels.
            sim.Mouse.LeftButtonDragDrop(mid.X, mid.Y, mid.X + 50, mid.Y, 5, 1);
            Sleep(100);
            newbounds = resizer.Bounds;
            Assert.IsTrue(newbounds.Center().X > mid.X);
        }

        [TestMethod]
        [Timeout(TestMethodTimeout)]
        public void TestAccessibility()
        {

            Trace.WriteLine("TestAccessibility==========================================================");
            string testFile = _testDir + "UnitTests\\test1.xml";
            Window w = this.LaunchNotepad(testFile);
            Sleep(1000);

            // Get AutomationWrapper to selected node in the tree.
            AutomationWrapper tree = this.TreeView;
            AutomationWrapper root = tree.GetChild(3);

            // employee
            AutomationWrapper emp = root.GetChild(7);
            emp.Select();
            CheckNodeName(emp, "Employee");
            Assert.AreEqual(emp.Name, tree.GetSelectedChild().Name);

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
            node = w.AccessibleObject.HitTest(p.X, p.Y);

            // bugbug: this seems to be broken when scaling DPI to 150%.
            // Assert.IsTrue(node.Name == emp.Name);

            emp.RemoveFromSelection();
            emp.Select();
            Assert.AreEqual(root.Name, emp.Parent.Name);
            AutomationWrapper parent = root.Parent;
            string name = parent.Name;
            Assert.AreEqual(name, "TreeView");

            // default action on tree is toggle!
            tree.Invoke();
            Sleep(500);

            // state on invisible nodes.
            Assert.IsTrue(node.IsVisible);

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
            CheckNodeValue(next, " Test all element types ");

            next = next.NextSibling; // pi
            next = next.NextSibling; // root
            next.Invoke(); // toggle
            next.Invoke(); // toggle

            emp = root.GetChild(7);
            emp.Select();

            AutomationWrapper ev = next.GetChild(7); // Employee value node
            Assert.AreEqual(ntv.GetSelectedChild().Name, ev.Name);
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

            SetNodeName("foo");
            CheckNodeName(node, "foo");
            Undo();
            CheckNodeName(node, "Country");

            Save("out.xml");
        }

        private void SetNodeName(string text)
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
        [Timeout(TestMethodTimeout)]
        public void TestKeyboard()
        {
            Trace.WriteLine("TestKeyboard==========================================================");
            string testFile = _testDir + "UnitTests\\emp.xml";
            string xsdFile = _testDir + "UnitTests\\emp.xsd";
            var w = this.LaunchNotepad(testFile);

            Sleep(1000);

            Trace.WriteLine("Test goto definition on schemaLocation");
            w.SendKeystrokes("^Ixsi{F12}");
            Window popup = w.ExpectingPopup("emp.xsd");
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

            // code coverage on expand/collapse.
            w.SendKeystrokes("^ICountry");
            Sleep(500);
            w.SendKeystrokes("{RIGHT}");
            Sleep(500);
            w.SendKeystrokes("{LEFT}");

            Sleep(1000);
            this.SaveAndCompare("out.xml", "emp.xml");
        }

        [TestMethod]
        [Timeout(TestMethodTimeout)]
        public void TestMouse()
        {
            Trace.WriteLine("TestMouse==========================================================");
            string testFile = _testDir + "UnitTests\\plants.xml";
            var w = this.LaunchNotepad(testFile);

            Sleep(1000);

            // Test mouse click on +/-.
            AutomationWrapper tree = this.TreeView;
            AutomationWrapper node = tree.FirstChild;
            node = node.NextSibling;
            node = node.FirstChild;
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
            sim.Mouse.MoveMouseTo(bounds.Left - 30 - 13, (bounds.Top + bounds.Bottom) / 2)
                .LeftButtonClick();

            Sleep(500);

            // is there any way to get this state ?
            //bool expanded2 = node.IsExpanded;
            //if (!expanded2) {
            //    throw new ApplicationException("Node did not become expanded");
            //}

            //mouse down edit of node name
            var center = bounds.Center();
            sim.Mouse.MoveMouseTo(center.X, center.Y).LeftButtonClick();
            Sleep(1000); // give it enough time to kick into edit mode.

            CheckOuterXml("PLANT");
            this.window.SendKeystrokes("{ESCAPE}");

            // code coverage on scrollbar interaction
            AutomationWrapper vscroll = w.FindDescendant("VScrollBar");
            bounds = vscroll.Bounds;

            Point downArrow = new Point((bounds.Left + bounds.Right) / 2, bounds.Bottom - (bounds.Width / 2));
            sim.Mouse.MoveMouseTo(downArrow.X, downArrow.Y);
            for (int i = 0; i < 10; i++)
            {
                sim.Mouse.LeftButtonClick();
                Sleep(500);
            }

            Rectangle finalBounds = node.Bounds;
            Assert.IsTrue(finalBounds.Y < bounds.Y);
        }

        [TestMethod]
        [Timeout(TestMethodTimeout)]
        public void TestUtilities()
        {
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
            foreach (PropertyInfo pi in t.GetProperties(BindingFlags.Static))
            {
                if (pi.PropertyType == typeof(string))
                {
                    string name = pi.Name;
                    object res = pi.GetValue(null, null);
                    if (res == null)
                    {
                        throw new Exception("Unexpected null returned from property: " + name);
                    }
                    Trace.WriteLine(string.Format("{0}={1}", name, res.ToString()));
                }
            }

            // Test XmlIncludeReader
            string test = _testDir + "UnitTests\\includes\\index.xml";
            Uri baseUri = new Uri(test);
            XmlDocument doc = new XmlDocument();
            doc.Load(test);

            XmlReaderSettings settings = new XmlReaderSettings();
            settings.DtdProcessing = DtdProcessing.Parse;
            foreach (XmlElement e in doc.SelectNodes("test/case"))
            {
                Uri input = new Uri(baseUri, e.GetAttribute("input"));
                Uri output = new Uri(baseUri, e.GetAttribute("results"));
                using (var r = XmlIncludeReader.CreateIncludeReader(input.LocalPath, settings))
                {
                    CompareResults(ReadNodes(r), output.LocalPath);
                }
            }
        }

        [TestMethod]
        [Timeout(TestMethodTimeout)]
        public void TestNoBorderTabControl()
        {
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

            Assert.IsTrue(tabs.TabPages.Contains(page1));
            Assert.IsTrue(!tabs.TabPages.Contains(page2));
            tabs.TabPages.Insert(0, page2);


            Sleep(1000);

            int i = tabs.TabPages.IndexOf(page1);
            Assert.AreEqual(i, 1);

            i = tabs.TabPages.IndexOf(page2);
            Assert.AreEqual(i, 0);

            Assert.IsTrue(!tabs.TabPages.IsFixedSize);
            Assert.IsTrue(!tabs.TabPages.IsReadOnly);
            Assert.IsTrue(!tabs.TabPages.IsSynchronized);

            tabs.TabPages.Remove(page1);
            tabs.TabPages.RemoveAt(0);

            Sleep(1000);

            tabs.TabPages[0] = page1;
            tabs.TabPages[1] = page2;

            Sleep(1000);

            NoBorderTabPage[] a = new NoBorderTabPage[tabs.TabPages.Count];
            tabs.TabPages.CopyTo(a, 0);
            Assert.AreEqual(a[0], page1);
            Assert.AreEqual(a[1], page2);
            f.Close();
        }

        [TestMethod]
        [Timeout(TestMethodTimeout)]
        public void TestInclude()
        {
            Trace.WriteLine("TestInclude==========================================================");
            string nonexist = _testDir + "UnitTests\\Includes\\nonexist.xml";
            WipeFile(nonexist);

            try
            {

                string testFile = _testDir + "UnitTests\\Includes\\i1.xml";
                var w = this.LaunchNotepad(testFile);

                w.SendKeystrokes("^Iinclude");
                w.InvokeMenuItem("gotoDefinitionToolStripMenuItem");
                Window popup = w.WaitForPopup();
                popup.DismissPopUp("%{F4}");

                w.InvokeMenuItem("expandXIncludesToolStripMenuItem");
                this.SaveAndCompare("Includes\\out.xml", "Includes\\r1.xml");

                Trace.WriteLine("Test F12 on non-existant include");
                testFile = _testDir + "UnitTests\\Includes\\i3.xml";
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
                if (!File.Exists(nonexist))
                {
                    throw new ApplicationException("File should now exist!");
                }
            }
            finally
            {
                WipeFile(nonexist);
            }
        }

        [TestMethod]
        [Timeout(TestMethodTimeout)]
        public void TestUnicode()
        {
            Trace.WriteLine("TestUnicode==========================================================");
            string testFile = _testDir + "UnitTests\\unicode.xml";
            var w = this.LaunchNotepad(testFile);

            string outFile = _testDir + "UnitTests\\out.xml";
            WipeFile(outFile);

            w.InvokeAsyncMenuItem("exportErrorsToolStripMenuItem");

            Window popup = w.WaitForPopup();
            popup.DismissPopUp(outFile + "{ENTER}");

            string expectedFile = _testDir + "UnitTests\\errors.xml";
            CompareResults(ReadNodes(expectedFile), outFile);

        }

        [TestMethod]
        [Timeout(TestMethodTimeout)]
        public void TestXmlSettings()
        {
            Trace.WriteLine("TestXmlSettings==========================================================");
            string testFile = _testDir + "UnitTests\\test10.xml";

            // make sure we have a settings file.
            var w = this.LaunchNotepad(testFile, false);
            w.Close(); // this will save default settings.

            // now we should be able to open the settings.
            w = this.LaunchNotepad(testFile, false);
            w.InvokeAsyncMenuItem("openSettingsToolStripMenuItem");

            // <DisableDefaultXslt>False</DisableDefaultXslt>
            w.SendKeystrokes("^IDis");
            w.InvokeMenuItem("toolStripButtonCopy");
            CheckClipboard(new Regex(".*DisableDefaultXslt.*"));
        }

        [TestMethod]
        [Timeout(TestMethodTimeout)]
        public void TestChangeTo()
        {
            Trace.WriteLine("TestChangeTo==========================================================");
            string testFile = _testDir + "UnitTests\\test8.xml";
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
        private void SaveAndCompare(string outname, string compareWith)
        {
            string outFile = Save(outname);

            string expectedFile = _testDir + "UnitTests\\" + compareWith;
            Sleep(1000);
            CompareResults(ReadNodes(expectedFile), outFile);
        }

        private string Save(string outname)
        {
            Trace.WriteLine("Save");
            string outFile = _testDir + "UnitTests\\" + outname;
            DeleteFile(outFile);

            // Has to be "Async" otherwise automation locks up because of the popup dialog.
            this.window.InvokeAsyncMenuItem("saveAsToolStripMenuItem");
            Window dialog = this.window.WaitForPopup();
            FileDialogWrapper od = new FileDialogWrapper(dialog);
            dialog.DismissPopUp(outFile + "{ENTER}");

            Sleep(1000); // give it time to save.
            return outFile;
        }


        void TestHitTest(Point pt, AutomationWrapper parent, AutomationWrapper expected)
        {
            AutomationWrapper obj = parent.HitTest(pt.X, pt.Y);
            if (obj.Name != expected.Name)
            {
                throw new ApplicationException(
                    string.Format("Found node '{0}' at {1},{2} instead of node '{3}'",
                        obj.Name, pt.X.ToString(), pt.Y.ToString(), expected.Name)
                    );
            }
        }

        Point TopCenter(Rectangle bounds, int dy)
        {
            return new Point(bounds.Left + (bounds.Width / 2), bounds.Top + dy);
        }

        void FocusTreeView()
        {
            AutomationWrapper acc = this.TreeView;
            acc.SetFocus();
        }

        void CheckNodeName(string expected)
        {
            AutomationWrapper acc = this.TreeView;
            AutomationWrapper node = acc.GetSelectedChild();
            if (node == null)
            {
                throw new ApplicationException("No node selected in tre view!");
            }
            CheckNodeName(node, expected);
        }

        void CheckNodeName(AutomationWrapper acc, string expected)
        {
            string name = acc.Name;
            if (name != expected)
            {
                throw new ApplicationException(string.Format("Expecting node name '{0}', but found '{1}'", expected, name));
            }
            Trace.WriteLine("Name=" + name);
#if DEBUG
            Sleep(200); // so we can watch it!
#endif
        }

        void CheckNodeValue(AutomationWrapper acc, string expected)
        {
            string value = acc.Value;
            if (value != expected)
            {
                throw new ApplicationException(string.Format("Expecting node value '{0}'", expected));
            }
            Trace.WriteLine("Value=" + value);
#if DEBUG
            Sleep(200); // so we can watch it!
#endif
        }

        void CheckProperties(AutomationWrapper node)
        {
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

        public string GetClipboardText()
        {
            int retries = 5;
            while (retries-- > 0)
            {
                if (Clipboard.ContainsText())
                {
                    return Clipboard.GetText();
                }
                Sleep(250);
            }

            throw new ApplicationException("clipboard does not contain any text!");
        }

        public override void CheckClipboard(string expected)
        {
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
            AssertNormalizedEqual("" + text, expected);
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

        public void AssertNormalizedEqual(string value, string expected)
        {
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

        public static string NormalizeNewLines(string text)
        {
            if (text == null) return null;
            StringBuilder sb = new StringBuilder();
            for (int i = 0, n = text.Length; i < n; i++)
            {
                char ch = text[i];
                if (ch == '\r')
                {
                    if (i + 1 < n && text[i + 1] == '\n')
                        i++;
                    sb.Append("\r\n");
                }
                else if (ch == '\n')
                {
                    sb.Append("\r\n");
                }
                else
                {
                    sb.Append(ch);
                }
            }
            return sb.ToString();
        }

        /// <summary>
        /// Make the given rectangle visible by moving the given window out of the way.
        /// </summary>
        /// <param name="w"></param>
        /// <param name="target"></param>
        /// <returns></returns>
        Point GetDropSpot(Window w, Rectangle target)
        {
            AutomationWrapper acc = w.AccessibleObject;
            Rectangle source = acc.Bounds;
            Rectangle inflated = source;
            inflated.Inflate(20, 20); // add extra margin
            if (inflated.Contains(target))
            {
                // Source window is completely occluding the target window, so we need to move it!
                int x = source.Left + (source.Width / 2);
                int y = source.Top + 10;
                int amount = target.Left - source.Left + 300;
                int step = 5;

                sim.Mouse
                   .MoveMouseTo(x, y)
                   .Sleep(100)
                   .LeftButtonDown();

                for (int i = x; i < x + amount; i += step)
                {
                    sim.Mouse.MoveMouseTo(i, y).Sleep(1);
                }

                sim.Mouse.LeftButtonUp();

                // get new moved bounds.
                source = acc.Bounds;
            }
            if (source.Left > target.Left)
            {
                // pick a spot along the left margin
                return new Point((target.Left + source.Left) / 2, (target.Top + target.Bottom) / 2);
            }
            else if (source.Right < target.Right)
            {
                // pick a spot along the right margin
                return new Point((target.Right + source.Right) / 2, (target.Top + target.Bottom) / 2);
            }
            else if (source.Top > target.Top)
            {
                // top margin
                return new Point((target.Right + target.Left) / 2, (source.Top + target.Top) / 2);
            }
            else if (source.Bottom < target.Bottom)
            {
                // bottom margin
                return new Point((target.Right + target.Left) / 2, (source.Bottom + target.Bottom) / 2);
            }

            // Then MOVE the window so it's not in the way!
            w.SetWindowPosition(target.Right, source.Top);
            source = acc.Bounds;
            return new Point((target.Left + source.Left) / 2, (target.Top + target.Bottom) / 2);
        }

    }
}
