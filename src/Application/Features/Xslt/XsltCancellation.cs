using System.Net;
using System.Xml;

namespace XmlNotepad.Services;

internal sealed class XsltCancellationResolver(XmlResolver resolver, CancellationToken cancellation) : XmlResolver
{
    public override ICredentials? Credentials
    {
        set => resolver.Credentials = value!;
    }

    public override Uri ResolveUri(Uri? baseUri, string? relativeUri)
    {
        cancellation.ThrowIfCancellationRequested();
        return resolver.ResolveUri(baseUri, relativeUri);
    }

    public override object? GetEntity(Uri absoluteUri, string? role, Type? ofObjectToReturn)
    {
        cancellation.ThrowIfCancellationRequested();
        var entity = RemoteDocument.IsWebUri(absoluteUri)
            ? OpenWebResource(absoluteUri, role, ofObjectToReturn)
            : resolver.GetEntity(absoluteUri, role, ofObjectToReturn);
        return entity is Stream stream ? new XsltCancellationStream(stream, cancellation) : entity;
    }

    private object OpenWebResource(Uri uri, string? role, Type? type)
    {
        var request = resolver.GetEntityAsync(uri, role, type);
        try
        {
            return request.WaitAsync(cancellation).GetAwaiter().GetResult();
        }
        catch (OperationCanceledException) when (cancellation.IsCancellationRequested)
        {
            _ = DisposeAbandonedResourceAsync(request);
            throw;
        }
    }

    private static async Task DisposeAbandonedResourceAsync(Task<object> resource)
    {
        try
        {
            (await resource.ConfigureAwait(false) as IDisposable)?.Dispose();
        }
        catch (Exception)
        {
        }
    }
}

internal sealed class XsltCancellationStream(Stream stream, CancellationToken cancellation) : Stream
{
    public override bool CanRead => stream.CanRead;
    public override bool CanSeek => stream.CanSeek;
    public override bool CanWrite => stream.CanWrite;
    public override long Length => stream.Length;
    public override long Position { get => stream.Position; set => stream.Position = value; }

    public override int Read(byte[] buffer, int offset, int count)
    {
        cancellation.ThrowIfCancellationRequested();
        return stream.ReadAsync(buffer.AsMemory(offset, count), cancellation).AsTask().GetAwaiter().GetResult();
    }

    public override void Write(byte[] buffer, int offset, int count)
    {
        cancellation.ThrowIfCancellationRequested();
        stream.Write(buffer, offset, count);
    }

    public override void Write(ReadOnlySpan<byte> buffer)
    {
        cancellation.ThrowIfCancellationRequested();
        stream.Write(buffer);
    }

    public override void Flush() => stream.Flush();
    public override long Seek(long offset, SeekOrigin origin) => stream.Seek(offset, origin);
    public override void SetLength(long value) => stream.SetLength(value);

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            stream.Dispose();
        }
        base.Dispose(disposing);
    }
}
