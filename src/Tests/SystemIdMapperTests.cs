using Devlooped;

namespace Clarius.OpenLaw;

public class SystemIdMapperTests
{
    [Fact]
    public async Task Map()
    {
        var mapper = new SystemIdMapper(MemoryRepository.Create());
        var from = ("github", "123");
        var to = ("discord", "456");

        await mapper.MapAsync(from, to);
        await mapper.MapAsync(from, ("chebot", "asdf"));

        Assert.Equal("456", await mapper.FindAsync(from, "discord"));
        Assert.Equal("asdf", await mapper.FindAsync(from, "chebot"));

        // Test reverse mappings too, leverage tuple conversion
        Assert.Equal("123", await mapper.FindAsync(("chebot", "asdf"), "github"));
        Assert.Equal("123", await mapper.FindAsync(("discord", "456"), "chebot"));
    }
}
