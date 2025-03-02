using System.Runtime.CompilerServices;

namespace Clarius.OpenLaw;

/// <summary>
/// A file-based <see cref="IContentRepository"/> implementation that uses 
/// yaml front-matter (both at the beginning or the end of the file) 
/// to store metadata alongside the content.
/// </summary>
public class FileContentRepository : IContentRepository
{
    readonly string baseDir;

    public FileContentRepository(string baseDir)
    {
        this.baseDir = baseDir;
        Directory.CreateDirectory(baseDir);
    }

    public async IAsyncEnumerable<IContentInfo> EnumerateAsync([EnumeratorCancellation] CancellationToken cancellation)
    {
        foreach (var file in Directory.EnumerateFiles(baseDir, "*.md"))
        {
            cancellation.ThrowIfCancellationRequested();
            using var stream = File.OpenRead(file);
            if (ContentInfo.ReadFrontMatter(stream) is { } info)
            {
                yield return info;
                await Task.Yield();
            }
        }
    }

    public ValueTask<Stream?> GetContentAsync(string id)
    {
        var file = Path.Combine(baseDir, id + ".md");
        if (!File.Exists(file))
            return ValueTask.FromResult(default(Stream));

        return new ValueTask<Stream?>(File.OpenRead(file));
    }

    public ValueTask<long?> GetTimestampAsync(string id)
    {
        var file = Path.Combine(baseDir, id + ".md");
        if (!File.Exists(file))
            return ValueTask.FromResult<long?>(null);

        using var stream = File.OpenRead(file);
        if (ContentInfo.ReadFrontMatter(stream) is { Timestamp: var timestamp })
            return new ValueTask<long?>(timestamp);

        return ValueTask.FromResult<long?>(null);
    }

    public ValueTask<ContentAction> SetContentAsync(string id, long timestamp, Stream content)
    {
        var file = Path.Combine(baseDir, id + ".md");
        var action = ContentAction.Created;

        if (File.Exists(file))
        {
            using var stream = File.OpenRead(file);
            if (ContentInfo.ReadFrontMatter(stream) is { Timestamp: var existingTimestamp })
            {
                if (existingTimestamp == timestamp)
                    return new ValueTask<ContentAction>(ContentAction.Skipped);
            }
            action = ContentAction.Updated;
        }

        using var writer = new StreamWriter(file);
        content.CopyTo(writer.BaseStream);
        return new ValueTask<ContentAction>(action);
    }
}
