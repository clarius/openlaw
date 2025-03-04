using System.Text;
using SharpYaml.Serialization;

namespace Clarius.OpenLaw;

public class ContentInfo(string id, long timestamp) : IContentInfo
{
    static readonly Serializer serializer = new(new() { IgnoreUnmatchedProperties = true });

    public string Id { get; init; } = id;
    public long Timestamp { get; init; } = timestamp;
    long? IContentInfo.Timestamp => Timestamp;

    public static IContentInfo? ReadFrontMatter(Stream stream)
    {
        using var reader = new StreamReader(stream);
        var frontMatter = new StringBuilder();
        var line = reader.ReadLine();
        // First line must be the front-matter according to spec.
        if (line == "---")
        {
            while ((line = reader.ReadLine()) != "---")
            {
                frontMatter.AppendLine(line);
            }
        }
        else if (line == "<!--")
        {
            while ((line = reader.ReadLine()) != "-->")
            {
                frontMatter.AppendLine(line);
            }
        }
        else
        {
            return null;
        }

        return serializer.Deserialize<FrontMatterContentInfo>(frontMatter.ToString());
    }

    public string ToFrontMatter(FrontMatter style = FrontMatter.Markdown)
    {
        var yaml = serializer.Serialize(this);

        return style switch
        {
            FrontMatter.Markdown =>
                $"""
                ---
                {yaml}
                ---
                """,
            FrontMatter.Html =>
                $"""
                <!--
                {yaml}
                -->
                """,
            _ => yaml
        };
    }

    class FrontMatterContentInfo : IContentInfo
    {
        public required string Id { get; init; }

        public required long? Timestamp { get; init; }
    }
}
