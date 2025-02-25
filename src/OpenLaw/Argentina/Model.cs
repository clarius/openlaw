using System.Text.Json.Serialization;

namespace Clarius.OpenLaw.Argentina;

record SearchResults(int Total, int Skip, int Take, DocResult[] Docs);

record DocResult(string Id, string Abstract);

record IdType(string Id, string Type)
{
    public string Url => $"https://www.saij.gob.ar/{Id}";
}

public record DocumentAbstract(
    string Id, string Title, string Summary,
    ContentType Type, string Kind, string Status, string Date,
    long Modified, long Timestamp)
{
    public string Url => $"https://www.saij.gob.ar/{Id}";
};

public record Legislacion(
    string Id, string Ref, string Name, string Title, string Summary,
    ContentType Type, string Kind, string Status, string Date,
    long Modified, long Timestamp, string[] Terms,
    [property: JsonPropertyName("pub")] Publication? Publication) :
    DocumentAbstract(Id, Title, Summary, Type, Kind, Status, Date, Modified, Timestamp);

public record Publication([property: JsonPropertyName("org")] string Organization, string Date);

public static class DocumentExtensions
{
    public static string ToFrontMatter(this DocumentAbstract doc)
    {
        var fm = new Dictionary<string, object?>
        {
            { nameof(doc.Date), doc.Date },
        };
        if (doc is Legislacion full && full.Publication != null)
        {
            fm.Add(nameof(full.Name), full.Name);
            fm.Add(nameof(full.Publication), full.Publication);
        }
        fm.Add(nameof(doc.Url), doc.Url);

        return fm.ToYaml();
    }
}