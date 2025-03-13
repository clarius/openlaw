using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Clarius.OpenLaw.Argentina;

public class FileDocumentRepositoryTests(ITestOutputHelper output)
{
    [Fact]
    public async Task CanReadAndWriteDocument()
    {
        var client = SaijClientTests.CreateClient(output);
        var repo = new FileDocumentRepository(Path.Combine("repo", Guid.NewGuid().ToString(), "ley"));

        var doc = await client.LoadAsync("LNS0007682");
        Assert.Null(await repo.GetTimestampAsync(doc.Id));

        await repo.SetDocumentAsync(doc);

        Assert.Equal(doc.Timestamp, await repo.GetTimestampAsync(doc.Id));
    }
}
