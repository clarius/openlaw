using System.Text.Json.Serialization;

namespace Clarius.OpenLaw.Argentina;

record SearchResults(int Total, int Skip, int Take, DocResult[] Docs);

record DocResult(string Uuid, string Abstract);

record IdType(string Uuid, string Type)
{
    public string WebUrl => $"https://www.saij.gob.ar/{Uuid}";
    public string DataUrl => $"https://www.saij.gob.ar/view-document?guid={Uuid}";
}

public record Kind(string Code, string Text);

/// <summary>
/// The original parameters used to search for the document.
/// </summary>
public record Source(TipoNorma? Tipo, Jurisdiccion? Jurisdiccion, Provincia? Provincia);

public record DocumentAbstract(
    string Id,
    string Title, string Summary,
    ContentType Type, Kind Kind, string Status, string Date,
    string Modified, long Timestamp) : IContentInfo
{
    [JsonIgnore]
    public Source Source { get; init; } = new(null, null, null);

    public virtual string WebUrl => $"https://www.saij.gob.ar/{Id}";
    public string DataUrl => $"https://www.saij.gob.ar/view-document?guid={Id}";
};

public record Legislation(
    string Id, string Alias, string Ref,
    string Name, string Title, string Summary,
    ContentType Type, Kind Kind,
    string Status, string Date,
    string Modified, long Timestamp,
    string[] Terms,
    [property: JsonPropertyName("pub")] Publication? Publication) :
    DocumentAbstract(Id, Title, Summary, Type, Kind, Status, Date, Modified, Timestamp)
{
    public override string WebUrl => $"https://www.saij.gob.ar/{Alias}";
}

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

        var full = doc as Legislation;
        if (full != null)
        {
            fm.Add(nameof(full.Name), full.Name);
            if (full.Publication != null)
                fm.Add(nameof(full.Publication), full.Publication);
        }

        fm.Add(nameof(doc.WebUrl), doc.WebUrl);
        fm.Add(nameof(doc.DataUrl), doc.DataUrl);

        if (full != null)
            fm.Add(nameof(full.Alias), full.Alias);

        fm.Add(nameof(doc.Id), doc.Id);
        fm.Add(nameof(doc.Timestamp), doc.Timestamp);

        return fm.ToYaml();
    }
}