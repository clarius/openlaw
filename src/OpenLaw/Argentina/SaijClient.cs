using System.Diagnostics;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using Devlooped;

namespace Clarius.OpenLaw.Argentina;

public class SaijClient(IHttpClientFactory httpFactory, IProgress<ProgressMessage> progress)
{
    const string UrlFormat = "https://www.saij.gob.ar/busqueda?o={0}&p={1}&f=Total|Tipo+de+Documento{2}|Fecha|Organismo|Publicación|Tema|Estado+de+Vigencia/Vigente,+de+alcance+general|Autor|Jurisdicción{3}&s=fecha-rango|DESC&v=colapsada";

    static readonly JsonSerializerOptions options = new(JsonSerializerDefaults.Web)
    {
        TypeInfoResolver = new DefaultJsonTypeInfoResolver(),
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) },
        WriteIndented = true,
        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    };

    public async IAsyncEnumerable<DocumentAbstract> EnumerateAsync(string? tipo = "Ley", string? jurisdiccion = "Nacional", int skip = 0, int take = 25, [EnumeratorCancellation] CancellationToken cancellation = default)
    {
        using var http = httpFactory.CreateClient("saij");
        var url = string.Format(CultureInfo.InvariantCulture, UrlFormat, skip, take,
            !string.IsNullOrEmpty(tipo) ? $"/Legislación/{tipo}" : "",
            !string.IsNullOrEmpty(jurisdiccion) ? "/" + jurisdiccion : "");

        progress.Report(new("Initiating query...", 0));
        var json = await http.GetStringAsync(url, cancellation);

        var jq = await JQ.ExecuteAsync(json, ThisAssembly.Resources.Argentina.SaijSearch.Text);
        if (TryDeserialize<SearchResults>(jq) is not { } result)
            yield break;

        var query = ThisAssembly.Resources.Argentina.SaijAbstract.Text;

        while (true)
        {
            var percentage = skip * 100 / result.Total;
            var count = 0;
            foreach (var item in result.Docs)
            {
                count++;

                jq = await JQ.ExecuteAsync(item.Abstract, query);
                if (string.IsNullOrEmpty(jq))
                    Debugger.Launch();

                if (TryDeserialize<DocumentAbstract>(jq) is not { } doc)
                    continue;

                percentage = (skip + count) * 100 / result.Total;
                progress.Report(new($"Processing {skip + count} of {result.Total}", percentage));

                yield return doc with { Summary = StringMarkup.Cleanup(doc.Summary) };
            }

            skip = skip + take;
            url = string.Format(CultureInfo.InvariantCulture, UrlFormat, skip, take,
                !string.IsNullOrEmpty(tipo) ? $"/Legislación/{tipo}" : "",
                !string.IsNullOrEmpty(jurisdiccion) ? "/" + jurisdiccion : "");

            var response = await http.GetAsync(url, cancellation);
            if (!response.IsSuccessStatusCode)
                break;

            json = await response.Content.ReadAsStringAsync(cancellation);
            jq = await JQ.ExecuteAsync(json, ThisAssembly.Resources.Argentina.SaijSearch.Text);
            result = TryDeserialize<SearchResults>(jq);
            if (result == null)
                break;
        }
    }

    public async Task<JsonObject?> FetchJsonAsync(string id)
    {
        if (await FetchRawAsync(id) is not { } data ||
            JsonNode.Parse(data) is not JsonObject json)
            return null;

        return json;
    }

    public async Task<Document?> FetchDocumentAsync(string id)
    {
        if (await FetchRawAsync(id) is not { } data || 
            await JQ.ExecuteAsync(data, ThisAssembly.Resources.Argentina.SaijDocument.Text) is not { Length: > 0 } jq ||
            TryDeserialize<Document>(jq) is not { } doc)
        {
            Debugger.Launch();
            return null;
        }

        return doc with { Summary = StringMarkup.Cleanup(doc.Summary) };
    }

    public async Task<string?> FetchRawAsync(string id, HttpClient? http = default)
    {
        var dispose = http == null;
        try
        {
            http ??= httpFactory.CreateClient("saij");
            var response = await http.GetAsync("https://www.saij.gob.ar/view-document?guid=" + id);
            if (!response.IsSuccessStatusCode)
                return null;

            var doc = await response.Content.ReadAsStringAsync();

            return JsonNode.Parse(doc)?["data"]?.GetValue<string>();
        }
        finally
        {
            if (dispose)
                http?.Dispose();
        }
    }

    static T? TryDeserialize<T>(string json)
    {
        try
        {
            return JsonSerializer.Deserialize<T>(json, options);
        }
        catch (JsonException e)
        {
            Debugger.Launch();
            Debugger.Log(0, "", e.Message);
            return default;
        }
    }
}