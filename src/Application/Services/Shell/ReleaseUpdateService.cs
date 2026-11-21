using System.Net.Http;

namespace XmlNotepad.Services;

internal static class ReleaseUpdateService
{
    private static readonly HttpClient Client = new()
    {
        Timeout = TimeSpan.FromSeconds(12),
        MaxResponseContentBufferSize = 4 * 1024 * 1024
    };

    internal static readonly TimeSpan MinimumInterval = TimeSpan.FromSeconds(5);
    internal static readonly Version InstalledVersion = typeof(App).Assembly.GetName().Version!;
    internal static readonly Uri Releases = new("https://github.com/microsoft/XmlNotepad/releases/latest");

    internal static bool TryGetLocation(string value, out Uri location)
    {
        if (Uri.TryCreate(value, UriKind.Absolute, out var candidate) &&
            (candidate.IsFile || RemoteDocument.IsWebUri(candidate)))
        {
            location = candidate;
            return true;
        }
        location = null!;
        return false;
    }

    internal static async Task<UpdateManifest> CheckAsync(Uri location, CancellationToken cancellation)
    {
        if (location.IsFile)
        {
            return await Task.Run(() =>
            {
                using var stream = File.OpenRead(location.LocalPath);
                return UpdateManifest.Read(stream, location);
            }, cancellation);
        }

        using var response = await Client.GetAsync(location, cancellation);
        response.EnsureSuccessStatusCode();
        var source = response.RequestMessage!.RequestUri!;
        RemoteXmlResolver.EnsureWebUri(source);
        using var content = await response.Content.ReadAsStreamAsync(cancellation);
        return await Task.Run(() => UpdateManifest.Read(content, source), cancellation);
    }
}
