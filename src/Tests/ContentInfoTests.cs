using System.Text;
using Xunit;

namespace Clarius.OpenLaw;

public class ContentInfoTests
{
    [Fact]
    public void WhenReadingEmptyFrontMatter_ThenReturnsNull()
    {
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes("---\n---"));
        Assert.Null(ContentInfo.ReadFrontMatter(stream));
    }

    [Fact]
    public void WhenReadingFrontMatter_ThenReturnsContentInfo()
    {
        var id = Guid.NewGuid().ToString();
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(new ContentInfo(id, timestamp).ToFrontMatter()));

        var info = ContentInfo.ReadFrontMatter(stream);

        Assert.NotNull(info);
        Assert.Equal(id, info!.Id);
        Assert.Equal(timestamp, info.Timestamp);
    }
}