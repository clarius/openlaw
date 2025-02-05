using System.Text.Json.Serialization;

namespace Clarius.OpenLaw.Saij;

record SearchResults(int Total, int Skip, int Take, DocResult[] Docs);

record DocResult(string Id, string Abstract);

public record DocumentAbstract(string Id, string Ref, string Name, string Title, string Summary, string Type, string Kind,
    string Mecano, string Status, string Date,
    [property: JsonPropertyName("pub")] Publication Publication, string[] Terms);

public record Publication([property: JsonPropertyName("org")] string Organization, string Date);

public record Document(string Id, string Ref, string Name, string Title, string Summary, string Type, string Kind,
    string Mecano, string Status, string Date,
    Publication Publication, string[] Terms, string Text) : DocumentAbstract(Id, Ref, Name, Title, Summary, Type, Kind, Mecano, Status, Date, Publication, Terms);