using System.Text.Json;
using System.Text.Json.JsonDiffPatch;
using System.Text.Json.JsonDiffPatch.Diffs.Formatters;

namespace Clarius.OpenLaw;

public class JsonDIffTests
{
    [Fact]
    public void TestJsonDiff()
    {
        var first = JsonDocument.Parse(File.ReadAllText("Content/data1.json"));
        var second = JsonDocument.Parse(File.ReadAllText("Content/data2.json"));

        Assert.False(first.DeepEquals(second, JsonElementComparison.Semantic));

        var diff = JsonDiffPatcher.Diff(File.ReadAllText("Content/data1.json"), File.ReadAllText("Content/data2.json"),
            new JsonPatchDeltaFormatter());

        var patches = JsonSerializer.Deserialize<JsonPatch[]>(diff);
        Assert.NotNull(patches);
        Assert.Equal(2, patches.Length);
    }

    [Fact]
    public void CanDeserializeJsonPatchAdd()
    {
        var patch = JsonSerializer.Deserialize<JsonPatch>(
            """
            {
                "op": "add",
                "path": "/example",
                "value": "test"
            }
            """);

        var addPatch = Assert.IsType<JsonPatchAdd>(patch);
        Assert.Equal("/example", addPatch.Path);
        Assert.Equal("test", addPatch.Value);
    }

    [Fact]
    public void CanDeserializeJsonPatchRemove()
    {
        var patch = JsonSerializer.Deserialize<JsonPatch>(
            """
            {
                "op": "remove",
                "path": "/example"
            }
            """);

        var removePatch = Assert.IsType<JsonPatchRemove>(patch);
        Assert.Equal("/example", removePatch.Path);
    }

    [Fact]
    public void CanDeserializeJsonPatchReplace()
    {
        var patch = JsonSerializer.Deserialize<JsonPatch>(
            """
            {
                "op": "replace",
                "path": "/example",
                "value": "newValue"
            }
            """);

        var replacePatch = Assert.IsType<JsonPatchReplace>(patch);
        Assert.Equal("/example", replacePatch.Path);
        Assert.Equal("newValue", replacePatch.Value);
    }

    [Fact]
    public void CanDeserializeJsonPatchMove()
    {
        var patch = JsonSerializer.Deserialize<JsonPatch>(
            """
            {
                "op": "move",
                "from": "/source",
                "path": "/destination"
            }
            """);

        var movePatch = Assert.IsType<JsonPatchMove>(patch);
        Assert.Equal("/source", movePatch.From);
        Assert.Equal("/destination", movePatch.Path);
    }

    [Fact]
    public void CanDeserializeJsonPatchCopy()
    {
        var patch = JsonSerializer.Deserialize<JsonPatch>(
            """
            {
                "op": "copy",
                "from": "/source",
                "path": "/destination"
            }
            """);

        var copyPatch = Assert.IsType<JsonPatchCopy>(patch);
        Assert.Equal("/source", copyPatch.From);
        Assert.Equal("/destination", copyPatch.Path);
    }

    [Fact]
    public void CanDeserializeJsonPatchTest()
    {
        var patch = JsonSerializer.Deserialize<JsonPatch>(
            """
            {
                "op": "test",
                "path": "/example",
                "value": "testValue"
            }
            """);

        var testPatch = Assert.IsType<JsonPatchTest>(patch);
        Assert.Equal("/example", testPatch.Path);
        Assert.Equal("testValue", testPatch.Value);
    }
}
