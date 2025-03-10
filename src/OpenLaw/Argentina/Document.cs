using System.Text.Json.Serialization;
using YamlDotNet.Serialization;

namespace Clarius.OpenLaw.Argentina;

public record Document(
    string Id, string Alias, string Ref,
    [property: JsonPropertyName("type")] ContentType ContentType,
    [property: JsonPropertyName("kind")] DocumentType DocumentType,
    string Name, string Title, string Summary,
    string Status, DateOnly Date,
    string Modified, long Timestamp,
    string[] Terms,
    [property: JsonPropertyName("pub")] Publication? Publication) : IContentInfo, IWebDocument, ISearchDocument
{
    readonly NormalizedWebDocument normalizer = new(Id);

    public string WebUrl => $"https://www.saij.gob.ar/{Alias}";
    public string DataUrl => $"https://www.saij.gob.ar/view-document?guid={Id}";

    [YamlIgnore, SharpYaml.Serialization.YamlIgnore, JsonIgnore]
    public string Json
    {
        get => normalizer.Json;
        init => normalizer = normalizer with { Json = value };
    }

    [YamlIgnore, SharpYaml.Serialization.YamlIgnore, JsonIgnore]
    public string JQ
    {
        get => normalizer.JQ;
        init => normalizer = normalizer with { JQ = value };
    }

    [YamlIgnore, SharpYaml.Serialization.YamlIgnore, JsonIgnore]
    public Dictionary<string, object?> Data => normalizer.Data;

    [YamlIgnore, SharpYaml.Serialization.YamlIgnore, JsonIgnore]
    public Search Query { get; init; } = Search.Empty;

    long? IContentInfo.Timestamp => Timestamp;
}