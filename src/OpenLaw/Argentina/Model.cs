using System.Text.Json.Serialization;

namespace Clarius.OpenLaw.Argentina;

record SearchResults(int Total, int Skip, int Take, DocResult[] Docs);

record DocResult(string Id, string Abstract);

record IdType(string Id, string Type)
{
    public string HtmlUrl => $"https://www.saij.gob.ar/{Id}";
    public string JsonUrl => $"https://www.saij.gob.ar/view-document?guid={Id}";
}

public record Kind(string Code, string Text);

/// <summary>
/// The original parameters used to search for the document.
/// </summary>
public record Source(TipoNorma? Tipo, Jurisdiccion? Jurisdiccion, Provincia? Provincia);

public record DocumentAbstract(
    string Id, string Title, string Summary,
    ContentType Type, Kind Kind, string Status, string Date,
    string Modified, long Timestamp)
{
    [JsonIgnore]
    public Source Source { get; init; } = new(null, null, null);

    public string HtmlUrl => $"https://www.saij.gob.ar/{Id}";
    public string JsonUrl => $"https://www.saij.gob.ar/view-document?guid={Id}";
};

public record Legislation(
    string Id, string Ref, string Name, string Title, string Summary,
    ContentType Type, Kind Kind,
    string Status, string Date,
    string Modified, long Timestamp, string[] Terms,
    [property: JsonPropertyName("pub")] Publication? Publication) :
    DocumentAbstract(Id, Title, Summary, Type, Kind, Status, Date, Modified, Timestamp);

public record Publication([property: JsonPropertyName("org")] string Organization, string Date);

public record Link(string Url, string Name);

public static class DocumentExtensions
{
    public static string ToFrontMatter(this DocumentAbstract? doc)
    {
        if (doc == null)
            return string.Empty;

        var fm = new Dictionary<string, object?>
        {
            { nameof(doc.Date), doc.Date },
        };
        if (doc is Legislation full && full.Publication != null)
        {
            fm.Add(nameof(full.Name), full.Name);
            fm.Add(nameof(full.Publication), full.Publication);
        }
        fm.Add("Web", doc.HtmlUrl);
        fm.Add("Data", doc.JsonUrl);
        return fm.ToYaml();
    }
}