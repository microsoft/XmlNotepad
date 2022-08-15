using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.IO;
using XmlNotepadBuildTasks;
using Microsoft.Build.Framework;
using Moq;
using System.Diagnostics;

namespace BuildTaskTests
{
    [TestClass]
    public class BuildTaskUnitTest
    {
        delegate void LogMessageHandler(BuildMessageEventArgs e);
        delegate void LogCustomHandler(CustomBuildEventArgs e);
        delegate void LogErrorHandler(BuildErrorEventArgs e);
        delegate void LogWarningHandler(BuildWarningEventArgs e);


        [TestMethod]
        public void SimpleTest()
        {
            var location = Path.GetDirectoryName(typeof(BuildTaskUnitTest).Assembly.Location);
            // find root of XmlNotepad repo.
            location = new Uri(new Uri("file:///" + location), "../../..").LocalPath;

            SyncVersions wix = new SyncVersions()
            {
                DropDir = Path.Combine(location, @"src\drop"),
                MasterVersionFile = Path.Combine(location, @"src\Version\Version.props"),
                CSharpVersionFile = Path.Combine(location, @"src\Version\Version.cs"),
                ApplicationProjectFile = Path.Combine(location, @"src\Application\Application.csproj"),
                WixFile = Path.Combine(location, @"src\XmlNotepadSetup\Product.wxs"),
                UpdatesFile = Path.Combine(location, @"src\Updates\Updates.xml"),
                AppManifestFile = Path.Combine(location, @"src\XmlNotepadPackage\Package.appxmanifest"),
            };

            var mock = new Mock<IBuildEngine>();
            mock.Setup(x => x.LogMessageEvent(It.IsAny<BuildMessageEventArgs>())).Callback(new LogMessageHandler((e) =>
            {
                Trace.WriteLine(e.Message);
            }));
            mock.Setup(x => x.LogCustomEvent(It.IsAny<CustomBuildEventArgs>())).Callback(new LogCustomHandler((e) =>
            {
                Trace.WriteLine(e.Message);
            }));
            mock.Setup(x => x.LogErrorEvent(It.IsAny<BuildErrorEventArgs>())).Callback(new LogErrorHandler((e) =>
            {
                Trace.WriteLine(e.Message);
            }));
            mock.Setup(x => x.LogWarningEvent(It.IsAny<BuildWarningEventArgs>())).Callback(new LogWarningHandler((e) =>
            {
                Trace.WriteLine(e.Message);
            }));

            wix.BuildEngine = mock.Object;
            wix.Execute();

            Assert.IsTrue(!string.IsNullOrEmpty(wix.WebView2Version));
            
        }
    }
}
