using System.IO;
using System.Net;
using System.Net.Http;
using System.Xml;

namespace XmlNotepad.Services;

internal sealed class RemoteDocument(HttpResponseMessage response) : IDisposable
{
    private static readonly HttpClient Client = new(new HttpClientHandler
    {
        AutomaticDecompression = DecompressionMethods.All
    });

    public Uri Location => response.RequestMessage!.RequestUri!;

    public string Extension => response.Content.Headers.ContentType?.MediaType?.ToLowerInvariant() switch
    {
        "text/csv" or "application/csv" => ".csv",
        "application/json" or "text/json" => ".json",
        "text/html" => ".html",
        "application/xml" or "text/xml" or "application/xhtml+xml" => ".xml",
        var mediaType when mediaType?.EndsWith("+xml", StringComparison.Ordinal) == true => ".xml",
        var mediaType when mediaType?.EndsWith("+json", StringComparison.Ordinal) == true => ".json",
        _ => Path.GetExtension(Location.AbsolutePath).ToLowerInvariant()
    };

    internal static bool TryGetUri(string input, out Uri uri)
    {
        if (Uri.TryCreate(input, UriKind.Absolute, out var parsed) && IsWebUri(parsed))
        {
            uri = parsed;
            return true;
        }
        uri = null!;
        return false;
    }

    internal static bool IsWebUri(Uri uri) =>
        uri.IsAbsoluteUri && uri.Scheme is "https" or "http" && uri.UserInfo.Length == 0;

    internal static async Task<RemoteDocument> DownloadAsync(Uri location)
    {
        var result = await Client.GetAsync(location).ConfigureAwait(false);
        try
        {
            result.EnsureSuccessStatusCode();
            RemoteXmlResolver.EnsureWebUri(result.RequestMessage!.RequestUri!);
            return new RemoteDocument(result);
        }
        catch
        {
            result.Dispose();
            throw;
        }
    }

    public Task<Stream> OpenStreamAsync() => response.Content.ReadAsStreamAsync();

    public Task<string> ReadTextAsync() => response.Content.ReadAsStringAsync();

    internal static string GetFileName(Uri uri)
    {
        var name = Uri.UnescapeDataString(uri.Segments.LastOrDefault() ?? "").Trim('/');
        var invalid = Path.GetInvalidFileNameChars();
        return string.IsNullOrWhiteSpace(name)
            ? uri.Host + ".xml"
            : new string(name.Select(character => invalid.Contains(character) ? '_' : character).ToArray());
    }

    internal static Uri GetBaseUri(string? path, Uri? remoteSource)
    {
        return remoteSource ?? (path is null
            ? new Uri(Path.TrimEndingDirectorySeparator(Environment.CurrentDirectory) + Path.DirectorySeparatorChar)
            : new Uri(Path.GetFullPath(path)));
    }

    public void Dispose() => response.Dispose();
}
