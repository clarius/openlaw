using System.Text;
using Clarius.OpenLaw.Argentina;
using YamlDotNet.Serialization;

namespace Clarius.OpenLaw;

public class ContentInfo(string id, long timestamp) : IContentInfo
{
    static readonly IDeserializer serializer = new DeserializerBuilder()
        .IgnoreUnmatchedProperties()
        .Build();

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
        var yaml = this.ToYaml();

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
