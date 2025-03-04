using System.Text.Json;

namespace Clarius.OpenLaw.Argentina;

public static class DocumentExtensions
{
    public static string ToFrontMatter(this IWebDocument document)
    {
        var data = JsonSerializer.Deserialize<Dictionary<string, object?>>(document.JQ, JsonOptions.Default);
        if (data == null)
            return string.Empty;

        data = new Dictionary<string, object?>(data, StringComparer.OrdinalIgnoreCase);
        var fm = new Dictionary<string, object?>();

        if (data.TryGetValue(nameof(Document.Date), out var date))
            fm.Add(nameof(Document.Date), date);

        if (data.TryGetValue(nameof(Document.Name), out var name))
            fm.Add(nameof(Document.Name), name);
        if (data.TryGetValue("pub", out var publication))
            fm.Add(nameof(Document.Publication), publication);

        fm.Add(nameof(document.WebUrl), document.WebUrl);
        fm.Add(nameof(document.DataUrl), document.DataUrl);

        if (data.TryGetValue(nameof(Document.Alias), out var alias))
            fm.Add(nameof(Document.Alias), alias);

        fm.Add(nameof(document.Id), document.Id);
        if (data.TryGetValue(nameof(IContentInfo.Timestamp), out var timestamp))
            fm.Add(nameof(IContentInfo.Timestamp), timestamp);

        return fm.ToYaml();
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
            <!-- 
            {document.ToYaml()}
            -->            
            """ : markdown;
    }
}