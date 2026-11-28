using System.ComponentModel;
using System.Diagnostics;
using System.Text;
using System.Xml;
using System.Xml.Xsl;
using Microsoft.Win32;

namespace XmlNotepad.Services;

internal static class XsltScriptRunner
{
    private const string HostName = "XmlNotepad.XsltScriptHost.exe";

    internal static (byte[] Bytes, XmlOutputMethod Method, Encoding Encoding) Transform(
        Uri stylesheet, Uri documentBase, XmlReader content, CancellationToken cancellation)
    {
        var host = Path.Combine(ApplicationFiles.ContentDirectory, HostName);
        if (!File.Exists(host))
        {
            throw new XsltException("The embedded XSLT script host is missing from this installation.");
        }
        using var registry = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry32);
        using var runtime = registry.OpenSubKey(@"SOFTWARE\Microsoft\NET Framework Setup\NDP\v4\Full");
        if (runtime?.GetValue("Release") is not int release || release < 528040)
        {
            throw new XsltException("Embedded XSLT scripts require .NET Framework 4.8 or later.");
        }

        var workspace = Directory.CreateTempSubdirectory("XmlNotepadXslt-");
        var input = Path.Combine(workspace.FullName, "input.xml");
        var output = Path.Combine(workspace.FullName, "output.dat");
        var metadata = Path.Combine(workspace.FullName, "output.txt");
        try
        {
            using (var writer = XmlWriter.Create(input, new XmlWriterSettings { NewLineHandling = NewLineHandling.Entitize }))
            {
                writer.WriteNode(content, defattr: true);
            }
            cancellation.ThrowIfCancellationRequested();

            var start = new ProcessStartInfo(host)
            {
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardError = true,
                WorkingDirectory = Path.GetDirectoryName(stylesheet.LocalPath) ?? AppContext.BaseDirectory
            };
            start.ArgumentList.Add(stylesheet.AbsoluteUri);
            start.ArgumentList.Add(input);
            start.ArgumentList.Add(documentBase.AbsoluteUri);
            start.ArgumentList.Add(output);
            start.ArgumentList.Add(metadata);

            using var process = Process.Start(start) ?? throw new XsltException("The embedded XSLT script host could not start.");
            var errors = process.StandardError.ReadToEndAsync();
            WaitForExit(process, cancellation);
            if (process.ExitCode != 0)
            {
                var error = errors.Wait(2000) ? errors.GetAwaiter().GetResult().Trim() : string.Empty;
                throw new XsltException(error.Length == 0 ? "The embedded XSLT script failed." : error);
            }
            var result = File.ReadAllLines(metadata);
            if (result.Length != 2 || !Enum.TryParse<XmlOutputMethod>(result[0], out var method))
            {
                throw new XsltException("The embedded XSLT script returned invalid output information.");
            }
            return (File.ReadAllBytes(output), method, Encoding.GetEncoding(result[1]));
        }
        catch (Win32Exception exception)
        {
            throw new XsltException("Windows could not run the embedded XSLT script host. " + exception.Message, exception);
        }
        finally
        {
            try
            {
                File.Delete(input);
                File.Delete(output);
                File.Delete(metadata);
                workspace.Delete();
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
            {
            }
        }
    }

    private static void WaitForExit(Process process, CancellationToken cancellation)
    {
        using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(45));
        using var lifetime = CancellationTokenSource.CreateLinkedTokenSource(cancellation, deadline.Token);
        try
        {
            process.WaitForExitAsync(lifetime.Token).GetAwaiter().GetResult();
        }
        catch (OperationCanceledException)
        {
            if (!process.HasExited)
            {
                process.Kill(entireProcessTree: true);
                process.WaitForExit(2000);
            }
            cancellation.ThrowIfCancellationRequested();
            throw new XsltException("The embedded XSLT script timed out.");
        }
    }
}
