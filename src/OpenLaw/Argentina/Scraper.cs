using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using System.Xml.Linq;
using Devlooped;
using Devlooped.Web;

namespace Clarius.OpenLaw.Argentina;

//public record Norma(string Id, string Metadata, string Texto, string? Original);

public class Scraper(IHttpClientFactory httpFactory, IProgress<string> progress)
{
    const string UrlFormat = "https://www.saij.gob.ar/busqueda?o={0}&p={1}&f=Total|Tipo+de+Documento/Legislación/{2}|Fecha|Organismo|Publicación|Tema|Estado+de+Vigencia/Vigente,+de+alcance+general|Autor|Jurisdicción/Nacional&s=fecha-rango|DESC&v=colapsada";
    //const string UrlFormat = "https://www.saij.gob.ar/busqueda?o={0}&p={1}&f=Total|Fecha|Estado+de+Vigencia/Vigente,+de+alcance+general|Tema[5,1]|Organismo[5,1]|Autor[5,1]|Jurisdicción/Nacional|Tribunal[5,1]|Publicación[5,1]|Colección+temática[5,1]|Tipo+de+Documento/Legislación/{2}&s=fecha-rango|DESC&v=colapsada";

    public static class Tipo
    {
        public const string Ley = nameof(Ley);
        public const string Decreto = nameof(Decreto);
    }

    static readonly HtmlReaderSettings settings = new()
    {
        CaseFolding = Sgml.CaseFolding.ToLower,
        TextWhitespace = Sgml.TextWhitespaceHandling.TrimBoth,
        WhitespaceHandling = System.Xml.WhitespaceHandling.None,
    };

    static readonly JsonSerializerOptions camelOptions = new(JsonSerializerDefaults.Web)
    {
        TypeInfoResolver = new DefaultJsonTypeInfoResolver(),
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) },
        WriteIndented = true,
        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    };

    record SearchResult(string Id, string Abstract);

    public record DocumentAbstract(string Id, string Ref, string Name, string Title, string Summary, string Type, string Kind,
        string Mecano, string Status, string Date,
        [property: JsonPropertyName("pub")] Publication Publication, string[] Terms);

    public record Publication([property: JsonPropertyName("org")] string Organization, string Date);

    public record Document(string Id, string Ref, string Name, string Title, string Summary, string Type, string Kind,
        string Mecano, string Status, string Date,
        Publication Publication, string[] Terms, string Text) : DocumentAbstract(Id, Ref, Name, Title, Summary, Type, Kind, Mecano, Status, Date, Publication, Terms);

    public async Task<Document> FetchSaij(string id)
    {
        using var http = httpFactory.CreateClient();
        var json = await http.GetStringAsync("https://www.saij.gob.ar/view-document?guid=" + id);
        var data = JsonNode.Parse(json)?["data"]?.GetValue<string>();
        if (data is null)
            throw new InvalidOperationException("No data found for " + id);

        var jq = await JQ.ExecuteAsync(data, ThisAssembly.Resources.Argentina.SaijDocument.Text);
        var doc = JsonSerializer.Deserialize<Document>(jq, camelOptions);

        return doc;
    }

    public async IAsyncEnumerable<DocumentAbstract> SearchSaij(string tipo = Tipo.Ley, int skip = 0, int take = 25, [EnumeratorCancellation] CancellationToken cancellation = default)
    {
        using var http = httpFactory.CreateClient();
        var url = string.Format(CultureInfo.InvariantCulture, UrlFormat, skip, take, tipo);
        var json = await http.GetStringAsync(url, cancellation);

        var jq = await JQ.ExecuteAsync(json, ThisAssembly.Resources.Argentina.SaijSearch.Text);
        var result = JsonSerializer.Deserialize<SearchResult[]>(jq, camelOptions);
        if (result == null)
            yield break;

        foreach (var item in result)
        {
            jq = await JQ.ExecuteAsync(item.Abstract, ThisAssembly.Resources.Argentina.SaijAbstract.Text);
            var doc = JsonSerializer.Deserialize<DocumentAbstract>(jq, camelOptions);
            if (doc is null)
                continue;

            progress.Report(
                $"""
                {doc.Title}
                {doc.Name} {doc.Date}. {doc.Status}
                """);
            yield return doc;
        }
    }

    public async Task<string?> ScrapAsync(int id, CancellationToken cancellation = default)
    {
        using var http = httpFactory.CreateClient();
        var html = await http.GetStringAsync("https://servicios.infoleg.gob.ar/infolegInternet/verNorma.do?id=" + id, cancellation);
        var doc = HtmlDocument.Parse(html, settings);

        return doc.ToString(SaveOptions.None);
    }

    public async Task<string?> ScrapAsync(string url, CancellationToken cancellation = default)
    {
        progress.Report($"cargando texto de {url}");
        using var http = httpFactory.CreateClient();
        var html = await http.GetStringAsync(url, cancellation);
        var doc = HtmlDocument.Parse(html, settings);

        if (doc.CssSelectElement("#Textos_Completos") is { } texto)
            return GetText(texto);

        if (doc.CssSelectElement("#resultados") is { } resultados)
            return GetText(resultados);

        if (doc.CssSelectElement("#resultados_caja") is { } caja)
            return GetText(caja);

        progress.Report("No se pudo cargar texto de " + url);

        return null;
    }

    static string? GetText(XElement element)
    {
        var text = element.ToString(SaveOptions.DisableFormatting);
        if (string.IsNullOrEmpty(text))
            return null;

        // replace <br> and <br/> with newlines, and remove all markup to 
        // turn into plain text
        text = text.Replace("<br>", "\n").Replace("<br/>", "\n").Replace("<br />", "\n").Trim();
        if (HtmlDocument.Parse(text, settings).CssSelectElement("body")?.Value is { } body)
            return body;

        return text;
    }
}
