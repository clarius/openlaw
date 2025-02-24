using System.Text.Json.Serialization;

namespace Clarius.OpenLaw.Argentina;

record SearchResults(int Total, int Skip, int Take, DocResult[] Docs);

record DocResult(string Id, string Abstract);

public record DocumentAbstract(
    string Id, string Ref,
    string Name, string Title, string Summary,
    string Type, string Kind, string Status, string Date,
    long Modified, string[] Terms)
{
    public string Url => $"https://www.saij.gob.ar/{Id}";
};

public record Document(
    string Id, string Ref, long Timestamp,
    string Name, string Title, string Summary,
    string Type, string Kind, string Status, string Date,
    long Modified, string[] Terms,
    [property: JsonPropertyName("pub")] Publication? Publication) :
    DocumentAbstract(Id, Ref, Name, Title, Summary, Type, Kind, Status, Date, Modified, Terms);

public record Publication([property: JsonPropertyName("org")] string Organization, string Date);

public static class DocumentExtensions
{
    public static string ToFrontMatter(this DocumentAbstract doc)
    {
        var fm = new Dictionary<string, object?>
        {
            { nameof(doc.Name), doc.Name },
            { nameof(doc.Date), doc.Date },
        };
        if (doc is Document full && full.Publication != null)
        {
            fm.Add(nameof(full.Publication), full.Publication);
        }
        fm.Add(nameof(doc.Url), doc.Url);

        return fm.ToYaml();
    }
}