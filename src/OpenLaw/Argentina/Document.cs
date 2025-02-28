using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using Devlooped;

namespace Clarius.OpenLaw.Argentina;

public class Document
{
    static readonly JsonSerializerOptions writerOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true
    };

    public static JsonSerializerOptions JsonOptions { get; } = new(JsonSerializerDefaults.Web)
    {
        TypeInfoResolver = new DefaultJsonTypeInfoResolver(),
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        Converters =
        {
            new TipoNormaConverter(),
            new JsonStringEnumConverter(JsonNamingPolicy.CamelCase),
            new JsonDictionaryConverter(),
        },
        WriteIndented = true,
        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    };

    bool metadataDone;
    Dictionary<string, object?>? dictionary;
    DocumentAbstract? metadata;
    string? markdown;

    public Document(string id, ContentType type, string json)
    {
        Id = id;
        Type = type;

        // Cleanups and re-serialize to ensure consistent formatting and markup removal.
        dictionary = JsonSerializer.Deserialize<Dictionary<string, object?>>(json, JsonOptions);
        Json = JsonSerializer.Serialize(dictionary, writerOptions);
    }

    public string Id { get; }
    public ContentType Type { get; }
    public string Json { get; }
    public Dictionary<string, object?>? Dictionary => dictionary;

    public string HtmlUrl => $"https://www.saij.gob.ar/{Id}";
    public string JsonUrl => $"https://www.saij.gob.ar/view-document?guid={Id}";

    public async Task<DocumentAbstract?> GetAbstractAsync()
    {
        // We only need to attempt metadata conversion once
        if (metadataDone)
            return metadata;

        metadata = JsonOptions.TryDeserialize<Legislation>(
            await JQ.ExecuteAsync(Json, ThisAssembly.Resources.Argentina.SaijDocument.Text)) ??
            JsonOptions.TryDeserialize<DocumentAbstract>(
            await JQ.ExecuteAsync(Json, ThisAssembly.Resources.Argentina.SaijAbstract.Text));

        if (metadata != null)
        {
            metadata = metadata with
            {
                Summary = StringMarkup.Cleanup(metadata.Summary)
            };
        }

        metadataDone = true;
        return metadata;
    }

    public async Task<string> GetMarkdownAsync(bool includeMetadata = true)
    {
        if (markdown != null)
        {
            return includeMetadata ?
                $"""
                ---
                {metadata?.ToFrontMatter()}
                ---
                {markdown}
                <!-- 
                {(await GetAbstractAsync())?.ToYaml()}
                -->            
                """ : markdown;
        }

        if (dictionary == null)
            return string.Empty;

        var doc = await GetAbstractAsync();
        if (doc == null)
            return string.Empty;

        var fm = doc.ToFrontMatter();

        markdown = DictionaryConverter.ToMarkdown(dictionary, out var links, renderLinks: false);
        if (string.IsNullOrEmpty(markdown))
        {
            markdown =
                $"""
                # {doc.Title}

                {doc.Summary}

                {DictionaryConverter.ToMarkdown(dictionary, renderLinks: true)}
                """;
        }
        else if (links.Count > 0)
        {
            // Re-render to append links at the end of the content
            markdown = DictionaryConverter.ToMarkdown(dictionary, renderLinks: true);
        }

        return includeMetadata ?
          $"""
            ---
            {metadata?.ToFrontMatter()}
            ---
            {markdown}
            <!-- 
            {(await GetAbstractAsync())?.ToYaml()}
            -->            
            """ : markdown;
    }

    public override string ToString() => $"[{Id}]({HtmlUrl})";
}