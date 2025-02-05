using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using Clarius.OpenLaw.Saij;
using Devlooped;

namespace Clarius.OpenLaw;

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

    public async IAsyncEnumerable<JsonObject> EnumerateAsync(string? tipo = "Ley", string? jurisdiccion = "Nacional", int skip = 0, int take = 25, [EnumeratorCancellation] CancellationToken cancellation = default)
    {
        using var http = httpFactory.CreateClient("saij");
        var url = string.Format(CultureInfo.InvariantCulture, UrlFormat, skip, take,
            !string.IsNullOrEmpty(tipo) ? $"/Legislación/{tipo}" : "",
            !string.IsNullOrEmpty(jurisdiccion) ? "/" + jurisdiccion : "");

        progress.Report(new("Initiating query...", 0));
        var json = await http.GetStringAsync(url, cancellation);

        var jq = await JQ.ExecuteAsync(json, ThisAssembly.Resources.SaijSearch.Text);
        var result = JsonSerializer.Deserialize<SearchResults>(jq, options);
        if (result == null)
            yield break;

        while (true)
        {
            var percentage = skip * 100 / result.Total;
            var count = 0;
            foreach (var item in result.Docs)
            {
                count++;
                var id = await JQ.ExecuteAsync(item.Abstract, ".document.metadata.uuid");
                percentage = (skip + count) * 100 / result.Total;
                progress.Report(new($"Processing {skip + count} of {result.Total}", percentage));
                if (await FetchImplAsync(http, id) is not JsonObject obj)
                    // TODO: log warning?
                    continue;

                yield return obj;
            }

            skip = skip + take;
            url = string.Format(CultureInfo.InvariantCulture, UrlFormat, skip, take,
                !string.IsNullOrEmpty(tipo) ? $"/Legislación/{tipo}" : "",
                !string.IsNullOrEmpty(jurisdiccion) ? "/" + jurisdiccion : "");

            var response = await http.GetAsync(url, cancellation);
            if (!response.IsSuccessStatusCode)
                break;

            json = await response.Content.ReadAsStringAsync(cancellation);
            jq = await JQ.ExecuteAsync(json, ThisAssembly.Resources.SaijSearch.Text);
            result = JsonSerializer.Deserialize<SearchResults>(jq, options);
            if (result == null)
                break;
        }
    }

    public async Task<JsonObject?> FetchAsync(string id)
    {
        using var http = httpFactory.CreateClient("saij");
        return await FetchImplAsync(http, id);
    }

    async Task<JsonObject?> FetchImplAsync(HttpClient http, string id)
    {
        var response = await http.GetAsync("https://www.saij.gob.ar/view-document?guid=" + id);
        if (!response.IsSuccessStatusCode)
            return null;
        var doc = await response.Content.ReadAsStringAsync();
        var data = JsonNode.Parse(doc)?["data"]?.GetValue<string>();
        if (data is null ||
            JsonNode.Parse(data) is not JsonObject json)
            return null;

        return json;
    }
}