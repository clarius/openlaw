using System.Text.Json.Serialization;

namespace Clarius.OpenLaw.Argentina;

public record Document(
    string Id, string Alias, string Ref,
    ContentType ContentType, DocumentType DocumentType,
    string Name, string Title, string Summary,
    string Status, DateOnly Date,
    string Modified, long Timestamp,
    string[] Terms,
    [property: JsonPropertyName("pub")] Publication? Publication) : IContentInfo, IWebDocument, ISearchDocument
{
    readonly NormalizedWebDocument normalizer = new(Id);

    public string WebUrl => $"https://www.saij.gob.ar/{Alias}";

    [JsonIgnore]
    public string Json
    {
        get => normalizer.Json;
        init => normalizer = normalizer with { Json = value };
    }

    [JsonIgnore]
    public string JQ
    {
        get => normalizer.JQ;
        init => normalizer = normalizer with { JQ = value };
    }

    [JsonIgnore]
    public Dictionary<string, object?> Data => normalizer.Data;

    [JsonIgnore]
    public Search Query { get; init; } = Search.Empty;

    long? IContentInfo.Timestamp => Timestamp;
}