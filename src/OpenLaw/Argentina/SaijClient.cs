using System.Diagnostics;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using Devlooped;
using Spectre.Console;
using Spectre.Console.Json;

namespace Clarius.OpenLaw.Argentina;

public class SaijClient(IHttpClientFactory httpFactory, IProgress<ProgressMessage> progress)
{
    const string UrlFormat = "https://www.saij.gob.ar/busqueda?o={0}&p={1}&f=Total{2}{3}{4}&s=fecha-rango|DESC&v=colapsada";

    static readonly JsonSerializerOptions options = new(JsonSerializerDefaults.Web)
    {
        TypeInfoResolver = new DefaultJsonTypeInfoResolver(),
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) },
        WriteIndented = true,
        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    };

    public async IAsyncEnumerable<DocumentAbstract> EnumerateAsync(
        TipoNorma? tipo = TipoNorma.Ley,
        Jurisdiccion? jurisdiccion = Jurisdiccion.Nacional,
        Provincia? provincia = null,
        int skip = 0, int take = 25, [EnumeratorCancellation] CancellationToken cancellation = default)
    {
        using var http = httpFactory.CreateClient("saij");
        var url = BuildUrl(tipo, jurisdiccion, provincia, skip, take);

        progress.Report(new("Initiating query...", 0));
        var json = await http.GetStringAsync(url, cancellation);

        var jq = await JQ.ExecuteAsync(json, ThisAssembly.Resources.Argentina.SaijSearch.Text);
        if (TryDeserialize<SearchResults>(jq) is not { } result)
            yield break;

        var query = ThisAssembly.Resources.Argentina.SaijAbstract.Text
            .Replace("$$kind$$", tipo?.ToString() ?? "Any");

        while (true)
        {
            if (result.Total == 0)
                break;

            var percentage = skip * 100 / result.Total;
            var count = 0;
            foreach (var item in result.Docs)
            {
                count++;

                // This is the bare minimum we expect all results to have.
                if (await JQ.ExecuteAsync(item.Abstract, ThisAssembly.Resources.Argentina.SaijIdType.Text) is not { } idType ||
                    TryDeserialize<IdType>(idType) is not { } id)
                {
                    progress.Report(new($"Skipping {skip + count} of {result.Total})", percentage));
                    continue;
                }

                // If it's not one of our supported content types, just skip.
                if (!DisplayValue.TryParse<ContentType>(id.Type, true, out _))
                {
                    progress.Report(new($"Skipping {skip + count} of {result.Total} (unspported content type '{id.Type}')", percentage));
                    continue;
                }

                jq = await JQ.ExecuteAsync(item.Abstract, query);
                if (string.IsNullOrEmpty(jq))
                {
                    AnsiConsole.MarkupLine($":cross_mark: [dim]{id.Type}[/] [blue][link={id.Url}]{id.Id}[/][/]");
#if DEBUG
                    Debugger.Launch();
                    AnsiConsole.Write(new JsonText(item.Abstract));
#endif
                    continue;
                }

                if (TryDeserialize<DocumentAbstract>(jq) is not { } doc)
                {
                    AnsiConsole.MarkupLine($":cross_mark: [dim]{id.Type}[/] [blue][link={id.Url}]{id.Id}[/][/]");
#if DEBUG
                    Debugger.Launch();
                    AnsiConsole.Write(new JsonText(item.Abstract));
#endif
                    continue;
                }

                percentage = (skip + count) * 100 / result.Total;
                progress.Report(new($"Processing {skip + count} of {result.Total}", percentage));

                yield return doc with { Summary = StringMarkup.Cleanup(doc.Summary) };
            }

            skip = skip + take;
            url = BuildUrl(tipo, jurisdiccion, provincia, skip, take);

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

    static string BuildUrl(TipoNorma? tipo, Jurisdiccion? jurisdiccion, Provincia? provincia, int skip, int take) => string.Format(
        CultureInfo.InvariantCulture, UrlFormat, skip, take,
        tipo == null ? "|Tipo+de+Documento/Legislación" : $"|Tipo+de+Documento/Legislación/{DisplayValue.ToString(tipo.Value)}",
        tipo == TipoNorma.Ley || tipo == TipoNorma.Decreto ? "|Estado+de+Vigencia/Vigente,+de+alcance+general" : "",
        provincia == null ?
            jurisdiccion == null ? "" : $"|Jurisdicción/{DisplayValue.ToString(jurisdiccion.Value)}" :
            $"|Jurisdicción/{DisplayValue.ToString(Jurisdiccion.Provincial)}/{DisplayValue.ToString(provincia.Value)}");

    public async Task<JsonObject?> FetchJsonAsync(string id)
    {
        if (await FetchRawAsync(id) is not { } data ||
            JsonNode.Parse(data) is not JsonObject json)
            return null;

        return json;
    }

    public async Task<Legislacion?> FetchDocumentAsync(string id)
    {
        if (await FetchRawAsync(id) is not { } data ||
            await JQ.ExecuteAsync(data, ThisAssembly.Resources.Argentina.SaijDocument.Text) is not { Length: > 0 } jq ||
            TryDeserialize<Legislacion>(jq) is not { } doc)
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
        if (string.IsNullOrEmpty(json))
            return default;

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