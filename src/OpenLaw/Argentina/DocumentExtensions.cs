using System.Text.Json.Serialization;
using YamlDotNet.Serialization;

namespace Clarius.OpenLaw.Argentina;

public static class DocumentExtensions
{
    record FrontMatter(
        [property: YamlMember(Alias = "Fecha")] string Date,
        [property: YamlMember(Alias = "Título")] string Name,
        [property: JsonPropertyName("pub"), YamlMember(Alias = "Publicación")] Publication? Publication)
    {
        [YamlMember(Alias = "SAIJ")]
        public string? WebUrl { get; set; }
    }

    record Publication(
        [property: JsonPropertyName("org"), YamlMember(Alias = "Organismo")] string Organization,
        [property: YamlMember(Alias = "Fecha")] string Date);

    public static string ToFrontMatter(this IWebDocument document)
    {
        var data = JsonOptions.Default.TryDeserialize<FrontMatter>(document.JQ);
        if (data == null)
            return string.Empty;

        data.WebUrl = document.WebUrl;

        return data.ToYaml();
    }

    public static string ToMarkdown(this IWebDocument document, bool includeMetadata = true)
    {
        var markdown = DictionaryConverter.ToMarkdown(document.Data, out var links, renderLinks: false);
        if (string.IsNullOrEmpty(markdown))
        {
            markdown =
                $"""
                # {(document.Data.TryGetValue("Title", out var title) ? title : "")}

                {(document.Data.TryGetValue("Summary", out var summary) ? summary : "")}

                {DictionaryConverter.ToMarkdown(document.Data, renderLinks: true)}
                """;
        }
        else if (links.Count > 0)
        {
            // Re-render to append links at the end of the content
            markdown = DictionaryConverter.ToMarkdown(document.Data, renderLinks: true);
        }

        return includeMetadata ?
          $"""
            ---
            {document.ToFrontMatter()}
            ---
            {markdown}
            """ : markdown;
    }
}