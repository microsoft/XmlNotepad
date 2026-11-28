using System.Drawing;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Xsl;
using XmlNotepad;

namespace XmlNotepad.Services;

public sealed class XmlXsltService
{
    private const string DefaultStylesheetResource = "XmlNotepad.DefaultSS.xslt";
    private readonly Settings _settings;
    private readonly XmlResolver _resolver;

    public XmlXsltService(AppSettingsService settingsService)
    {
        _settings = settingsService.Settings;
        _resolver = new XmlResourceResolver(_settings.Resolver ?? new XmlUrlResolver());
    }

    public string? GetLinkedStylesheetPath(XmlDocument document)
    {
        foreach (XmlProcessingInstruction instruction in document.ChildNodes.OfType<XmlProcessingInstruction>())
        {
            if (!string.Equals(instruction.Target, "xml-stylesheet", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            try
            {
                var attributes = new XmlDocument();
                attributes.LoadXml("<stylesheet " + instruction.Data + "/>");
                var element = attributes.DocumentElement!;
                if (element.GetAttribute("type") == "text/xsl")
                {
                    var href = element.GetAttribute("href");
                    if (!string.IsNullOrWhiteSpace(href))
                    {
                        return href;
                    }
                }
            }
            catch (XmlException)
            {
            }
        }

        return null;
    }

    public PreviewOutput Transform(XmlDocument document, string? documentPath, Uri? remoteSource, string? stylesheetPath,
        bool explicitlySelected, ColorTheme theme, Uri? trustedStylesheet = null, CancellationToken cancellation = default)
    {
        cancellation.ThrowIfCancellationRequested();
        var baseUri = RemoteDocument.GetBaseUri(documentPath, remoteSource);
        var stylesheetUri = ResolveStylesheetUri(baseUri, stylesheetPath);
        if (remoteSource is not null && stylesheetUri is not null && !explicitlySelected)
        {
            RemoteXmlResolver.EnsureWebUri(stylesheetUri);
        }
        var resolver = new XsltCancellationResolver(_resolver, cancellation);
        var webResolver = new RemoteXmlResolver(resolver);
        var stylesheetResolver = stylesheetUri is not null && RemoteDocument.IsWebUri(stylesheetUri) ? (XmlResolver)webResolver : resolver;
        var isDefaultStylesheet = stylesheetUri is null;
        var transformDocument = isDefaultStylesheet && _settings.GetBoolean("DisableDefaultXslt")
            ? CreateDisabledDefaultDocument()
            : document;
        var scriptsEnabled = _settings.GetBoolean("EnableXsltScripts", true);
        var canOfferScripts = scriptsEnabled && remoteSource is null && stylesheetUri is { IsFile: true };
        var readerSettings = XmlHelpers.CreateXmlSettings(remoteSource is null ? resolver : webResolver);
        byte[] bytes;
        Encoding encoding;
        XmlOutputMethod outputMethod;
        if (trustedStylesheet is not null)
        {
            if (!canOfferScripts || !trustedStylesheet.Equals(stylesheetUri))
            {
                throw new XsltException("Script approval is only available for the selected local stylesheet.");
            }
            XmlResourceResolver.ValidateUri(trustedStylesheet);
            using var reader = XmlIncludeReader.CreateIncludeReader(transformDocument, readerSettings, baseUri.AbsoluteUri, estimateSize: false);
            using var registration = cancellation.Register(reader.Cancel);
            (bytes, outputMethod, encoding) = XsltScriptRunner.Transform(trustedStylesheet, baseUri, reader, cancellation);
        }
        else
        {
            XslCompiledTransform transform;
            XmlWriterSettings outputSettings;
            try
            {
                transform = LoadTransform(stylesheetUri, stylesheetResolver, theme, out outputSettings);
            }
            catch (ScriptedStylesheetException exception)
            {
                if (canOfferScripts && exception.Location.IsFile)
                {
                    throw new ScriptedStylesheetException(exception.Location, stylesheetUri);
                }
                var reason = scriptsEnabled
                    ? "Script approval is available for local documents and stylesheets only."
                    : "Embedded stylesheet scripts are disabled in Options.";
                throw new ScriptedStylesheetException(exception.Location, reason: reason);
            }
            bytes = WriteTransform(transform, transformDocument, readerSettings, baseUri, stylesheetResolver,
                outputSettings, isDefaultStylesheet, cancellation);
            encoding = outputSettings.Encoding ?? new UTF8Encoding(false);
            outputMethod = outputSettings.OutputMethod;
        }

        cancellation.ThrowIfCancellationRequested();
        var preamble = encoding.GetPreamble();
        var offset = preamble.Length > 0 && bytes.AsSpan().StartsWith(preamble) ? preamble.Length : 0;
        var content = encoding.GetString(bytes, offset, bytes.Length - offset);
        var isHtml = outputMethod == XmlOutputMethod.Html ||
            outputMethod == XmlOutputMethod.AutoDetect &&
            Regex.IsMatch(content, "<html\\b", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        if (isHtml)
        {
            content = AddBaseUri(content, baseUri);
        }

        var extension = outputMethod switch
        {
            XmlOutputMethod.Html => ".html",
            XmlOutputMethod.Text => ".txt",
            XmlOutputMethod.AutoDetect when isHtml => ".html",
            _ => ".xml"
        };
        var suggestedFileName = GetSuggestedFileName(document,
            remoteSource is null ? documentPath : RemoteDocument.GetFileName(remoteSource), stylesheetUri, extension);
        return new PreviewOutput(content, bytes, extension, suggestedFileName);
    }

    private static byte[] WriteTransform(XslCompiledTransform transform, XmlDocument document, XmlReaderSettings readerSettings,
        Uri baseUri, XmlResolver resolver, XmlWriterSettings outputSettings, bool isDefaultStylesheet, CancellationToken cancellation)
    {
        XsltArgumentList? arguments = null;
        if (isDefaultStylesheet)
        {
            arguments = new XsltArgumentList();
            arguments.AddParam("stylesheetHint", "", "To preview a different stylesheet, choose View > Transform with XSLT.");
        }
        cancellation.ThrowIfCancellationRequested();
        using var stream = new MemoryStream();
        using var output = new XsltCancellationStream(stream, cancellation);
        using (var reader = XmlIncludeReader.CreateIncludeReader(document, readerSettings, baseUri.AbsoluteUri, estimateSize: false))
        using (var registration = cancellation.Register(reader.Cancel))
        using (var writer = XmlWriter.Create(output, outputSettings))
        {
            transform.Transform(reader, arguments, writer, resolver);
        }
        return stream.ToArray();
    }

    private XslCompiledTransform LoadTransform(Uri? stylesheetUri, XmlResolver resolver,
        ColorTheme theme, out XmlWriterSettings outputSettings)
    {
        var transform = new XslCompiledTransform();
        if (stylesheetUri is null)
        {
            using var stream = typeof(XmlXsltService).Assembly.GetManifestResourceStream(DefaultStylesheetResource)
                ?? throw new InvalidOperationException($"The built-in XSLT stylesheet '{DefaultStylesheetResource}' is missing.");
            using var reader = new StreamReader(stream);
            var stylesheet = ApplyDefaultStyles(reader.ReadToEnd(), theme);
            using var xmlReader = XmlReader.Create(new StringReader(stylesheet), XmlHelpers.CreateXmlSettings(resolver));
            transform.Load(xmlReader, XsltSettings.Default, resolver);
        }
        else
        {
            var stylesheets = new XsltStylesheetResolver(resolver);
            using var reader = stylesheets.Open(stylesheetUri);
            try
            {
                transform.Load(reader, new XsltSettings(enableDocumentFunction: true, enableScript: false), stylesheets);
            }
            catch (XsltException) when (stylesheets.ScriptError is { } scriptError)
            {
                throw scriptError;
            }
        }

        outputSettings = transform.OutputSettings?.Clone() ?? new XmlWriterSettings();
        return transform;
    }

    private string ApplyDefaultStyles(string stylesheet, ColorTheme theme)
    {
        var colors = _settings[theme == ColorTheme.Light ? "LightColors" : "DarkColors"] as ThemeColors
            ?? ThemeColors.GetDefaultColors(theme);
        var fontFamily = _settings["FontFamily"] as string ?? "Consolas, Courier New";
        var fontSize = Convert.ToDouble(_settings["FontSize"] ?? 10.0, System.Globalization.CultureInfo.InvariantCulture);
        var replacements = new Dictionary<string, string>
        {
            ["$FONT_FAMILY"] = fontFamily,
            ["$FONT_SIZE"] = fontSize.ToString("0.##", System.Globalization.CultureInfo.InvariantCulture) + "pt",
            ["$BACKGROUND_COLOR"] = ColorTranslator.ToHtml(colors.ContainerBackground),
            ["$ATTRIBUTE_NAME_COLOR"] = ColorTranslator.ToHtml(colors.Attribute),
            ["$ATTRIBUTE_VALUE_COLOR"] = ColorTranslator.ToHtml(colors.Text),
            ["$PI_COLOR"] = ColorTranslator.ToHtml(colors.PI),
            ["$TEXT_COLOR"] = ColorTranslator.ToHtml(colors.Text),
            ["$COMMENT_COLOR"] = ColorTranslator.ToHtml(colors.Comment),
            ["$ELEMENT_COLOR"] = ColorTranslator.ToHtml(colors.Element),
            ["$MARKUP_COLOR"] = ColorTranslator.ToHtml(colors.Markup),
            ["$SIDENOTE_COLOR"] = ColorTranslator.ToHtml(colors.EditorBackground),
            ["$OUTPUT_TIP_DISPLAY"] = "block"
        };

        foreach (var (token, value) in replacements)
        {
            stylesheet = stylesheet.Replace(token, value, StringComparison.Ordinal);
        }

        return stylesheet;
    }

    private static Uri? ResolveStylesheetUri(Uri baseUri, string? stylesheetPath)
    {
        if (string.IsNullOrWhiteSpace(stylesheetPath))
        {
            return null;
        }

        if (Uri.TryCreate(stylesheetPath, UriKind.Absolute, out var absoluteUri))
        {
            return absoluteUri;
        }

        return new Uri(baseUri, stylesheetPath);
    }

    private static XmlDocument CreateDisabledDefaultDocument()
    {
        var document = new XmlDocument();
        document.LoadXml("<Note>Default styling of your XML documents is disabled in Options.</Note>");
        return document;
    }

    private static string AddBaseUri(string content, Uri documentBaseUri)
    {
        if (Regex.IsMatch(content, "<base\\b", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant))
        {
            return content;
        }

        var baseUri = new Uri(documentBaseUri, ".").AbsoluteUri;
        var baseElement = $"<base href=\"{System.Net.WebUtility.HtmlEncode(baseUri)}\">";
        var head = Regex.Match(content, "<head\\b[^>]*>", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        if (head.Success)
        {
            return content.Insert(head.Index + head.Length, baseElement);
        }

        var html = Regex.Match(content, "<html\\b[^>]*>", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        return html.Success ? content.Insert(html.Index + html.Length, "<head>" + baseElement + "</head>") : content;
    }

    private static string GetSuggestedFileName(XmlDocument document, string? documentPath, Uri? stylesheetUri, string extension)
    {
        foreach (XmlProcessingInstruction instruction in document.ChildNodes.OfType<XmlProcessingInstruction>())
        {
            if (!string.Equals(instruction.Target, "xsl-output", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            try
            {
                var attributes = new XmlDocument();
                attributes.LoadXml("<output " + instruction.Data + "/>");
                var outputName = attributes.DocumentElement!.GetAttribute("default");
                if (!string.IsNullOrWhiteSpace(outputName))
                {
                    var fileName = Path.GetFileName(outputName);
                    return Path.HasExtension(fileName) ? fileName : fileName + extension;
                }
            }
            catch (XmlException)
            {
            }
        }

        var baseName = stylesheetUri is not null
            ? Path.GetFileNameWithoutExtension(stylesheetUri.LocalPath)
            : Path.GetFileNameWithoutExtension(documentPath) ?? "NewDocument";
        return baseName + "_output" + extension;
    }
}

