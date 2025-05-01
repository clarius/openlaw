using System.Text.Json.Serialization;

namespace Clarius.OpenLaw.Argentina;

record IdType(string Uuid, string Type)
{
    public string WebUrl => $"https://www.saij.gob.ar/{Uuid}";
    public string DataUrl => $"https://www.saij.gob.ar/view-document?guid={Uuid}";
}

public record Kind(string Code, string Text);

public record DocumentAbstract(
    string Id,
    ContentType Type, Kind Kind, string Status, DateOnly Date,
    string Modified, long? Timestamp) : IContentInfo
{
    [JsonIgnore]
    public Search Source { get; init; } = new(null, null, null);

    [JsonIgnore]
    public string Json { get; init; } = "{}";

    public virtual string WebUrl => $"https://www.saij.gob.ar/{Id}";
    public string DataUrl => $"https://www.saij.gob.ar/view-document?guid={Id}";
};

public record Legislation(
    string Id, string Alias, string Ref,
    string Name, string Title, string Summary,
    ContentType Type, Kind Kind,
    string Status, DateOnly Date,
    string Modified, long Timestamp,
    string[] Terms,
    [property: JsonPropertyName("pub")] Publication? Publication)
{
    public virtual string WebUrl => $"https://www.saij.gob.ar/{Id}";
    public string DataUrl => $"https://www.saij.gob.ar/view-document?guid={Id}";
}

public record Publication([property: JsonPropertyName("org")] string Organization, string Date);
