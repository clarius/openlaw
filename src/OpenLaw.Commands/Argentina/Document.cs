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
    [property: JsonPropertyName("pub")] Publication? Publication,
    [property: JsonPropertyName("refs")] References? References) : IContentInfo, IWebDocument, ISearchDocument
{
    readonly NormalizedWebDocument normalizer = new(Id);

    public string WebUrl => $"https://www.saij.gob.ar/{Alias}";
    public string DataUrl => $"https://www.saij.gob.ar/view-document?guid={Id}";

    [YamlIgnore, JsonIgnore]
    public string Json
    {
        get => normalizer.Json;
        init => normalizer = normalizer with { Json = value };
    }

    [YamlIgnore, JsonIgnore]
    public string JQ
    {
        get => normalizer.JQ;
        init => normalizer = normalizer with { JQ = value };
    }

    [YamlIgnore, JsonIgnore]
    public Dictionary<string, object?> Data => normalizer.Data;

    [YamlIgnore, JsonIgnore]
    public Search Query { get; init; } = Search.Empty;

    /// <summary>
    /// Parses the document from the given JSON data.
    /// </summary>
    /// <exception cref="NotSupportedException">The data cannot be deserialized into <see cref="Document"/></exception>
    public static async Task<Document> ParseAsync(string json)
    {
        if (await Devlooped.JQ.ExecuteAsync(json, ThisAssembly.Resources.Argentina.SaijDocument.Text) is not { } docjq ||
            JsonOptions.Default.TryDeserialize<Document>(docjq) is not { } doc)
            throw new ArgumentException($"Invalid document data'.", nameof(json));

        return doc with { JQ = docjq, Json = json };
    }

    long? IContentInfo.Timestamp => Timestamp;
}