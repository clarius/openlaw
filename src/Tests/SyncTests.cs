using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Clarius.OpenLaw.Argentina;
using NuGet.Packaging.Signing;
using NuGet.Versioning;
using Xunit;

namespace Clarius.OpenLaw;

public class SyncTests : IDisposable
{
    string targetDir;

    public SyncTests()
    {
        targetDir = Guid.NewGuid().ToString();
        Directory.CreateDirectory(targetDir);
    }

    public void Dispose()
    {
        if (Directory.Exists(targetDir))
            Directory.Delete(targetDir, true);
    }

    [Fact]
    public async Task CanSyncToTarget()
    {
        var source = new TestContentRepository(Enumerable.Range(1, 3));
        var target = new FileContentRepository(targetDir);

        // 1 won't exist
        // 2 will be skipped
        File.WriteAllText(Path.Combine(targetDir, "2.md"), new ContentInfo("2", 2).ToFrontMatter());
        // 3 will be updated
        File.WriteAllText(Path.Combine(targetDir, "3.md"), new ContentInfo("3", 0).ToFrontMatter());

        var sync = new SyncManager(source, target);

        await foreach (var result in sync.Sync())
        {
            if (result.Id == "1")
            {
                Assert.Equal(ContentAction.Created, result.Action);
            }
            else if (result.Id == "2")
            {
                Assert.Equal(ContentAction.Skipped, result.Action);
            }
            else if (result.Id == "3")
            {
                Assert.Equal(ContentAction.Updated, result.Action);
            }
            else
            {
                Assert.Fail("Should have no more items");
            }
        }
    }

    class TestContentRepository(IEnumerable<int> values) : IContentRepository
    {
        public async IAsyncEnumerable<IContentInfo> EnumerateAsync([EnumeratorCancellation] CancellationToken cancellation)
        {
            foreach (var value in values)
            {
                cancellation.ThrowIfCancellationRequested();
                yield return new ValueContent(value);
                await Task.Yield();
            }
        }

        public ValueTask<Stream?> GetContentAsync(string id) => new ValueTask<Stream?>(new MemoryStream(Encoding.UTF8.GetBytes(id)));
        public ValueTask<long?> GetTimestampAsync(string id) => new ValueTask<long?>(long.Parse(id));
        public ValueTask<ContentAction> SetContentAsync(string id, long timestamp, Stream content) => throw new NotImplementedException();
    }

    class ValueContent(int value) : IContentInfo
    {
        string IContentInfo.Id => value.ToString();

        long IContentInfo.Timestamp => value;
    }
}
