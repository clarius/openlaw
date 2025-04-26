using System.Text.Json.Serialization;

namespace Clarius.OpenLaw.Argentina;

public record SearchResult(string Id,
    ContentType ContentType,
    DocumentType DocumentType,
    string Status, DateOnly Date, long? Timestamp) : IContentInfo, IWebDocument, ISearchDocument
{
    readonly NormalizedWebDocument normalizer = new(Id);

    [JsonIgnore]
    public Dictionary<string, object?> Data => normalizer.Data;

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
    public Search Query { get; init; } = Search.Empty;
};