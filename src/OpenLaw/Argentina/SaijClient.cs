using System.Diagnostics;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text.Json.Nodes;
using Devlooped;
using Spectre.Console;
using Spectre.Console.Json;

namespace Clarius.OpenLaw.Argentina;

public class SaijClient(IHttpClientFactory httpFactory, IProgress<ProgressMessage> progress)
{
    const string UrlFormat = "https://www.saij.gob.ar/busqueda?o={0}&p={1}&f=Total{2}{3}{4}&s=fecha-rango|DESC&v=colapsada";

    record SearchResults(int Total, int Skip, int Take, DocResult[] Docs);
    record DocResult(string Uuid, string Abstract);

    public async IAsyncEnumerable<SearchResult> SearchAsync(
        TipoNorma? tipo = TipoNorma.Ley,
        Jurisdiccion? jurisdiccion = Jurisdiccion.Nacional,
        Provincia? provincia = null,
        int skip = 0, int take = 25, [EnumeratorCancellation] CancellationToken cancellation = default)
    {
        using var http = httpFactory.CreateClient("saij");
        var url = BuildUrl(tipo, jurisdiccion, provincia, skip, take);

        progress.Report(new("Initiating query...", 0));
        var json = await http.GetStringAsync(url, cancellation);

        var jq = await JQ.ExecuteAsync(json, ThisAssembly.Resources.Argentina.SearchResults.Text);
        if (JsonOptions.Default.TryDeserialize<SearchResults>(jq) is not { } result)
            yield break;

        var source = new Search(tipo, jurisdiccion, provincia);
        var query = ThisAssembly.Resources.Argentina.SearchResult.Text;

        while (true)
        {
            cancellation.ThrowIfCancellationRequested();

            if (result.Total == 0)
                break;

            var percentage = skip * 100 / result.Total;
            var count = 0;
            foreach (var item in result.Docs)
            {
                count++;

                // This is the bare minimum we expect all results to have.
                if (await JQ.ExecuteAsync(item.Abstract, ThisAssembly.Resources.Argentina.SaijIdType.Text) is not { } idType ||
                    JsonOptions.Default.TryDeserialize<IdType>(idType) is not { } id)
                {
                    progress.Report(new($"Skipping {skip + count} of {result.Total} (unsupported document)", percentage));
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
                    AnsiConsole.MarkupLine($":cross_mark: [dim]{id.Type}[/] [blue][link={id.WebUrl}]{id.Uuid}[/][/] ([blue][link={id.DataUrl}]JSON[/][/])");
#if DEBUG
                    Debugger.Launch();
                    AnsiConsole.Write(new JsonText(item.Abstract));
#endif
                    continue;
                }

                if (JsonOptions.Default.TryDeserialize<SearchResult>(jq) is not { } doc)
                {
                    AnsiConsole.MarkupLine($":cross_mark: [dim]{id.Type}[/] [blue][link={id.WebUrl}]{id.Uuid}[/][/] ([blue][link={id.DataUrl}]JSON[/][/])");
#if DEBUG
                    Debugger.Launch();
                    AnsiConsole.Write(new JsonText(item.Abstract));
#endif
                    continue;
                }

                percentage = (skip + count) * 100 / result.Total;
                progress.Report(new($"Processing {skip + count} of {result.Total} ([blue][link={id.WebUrl}]source[/][/])", percentage));

                yield return doc with
                {
                    Json = item.Abstract,
                    JQ = jq,
                    Query = source,
                };
            }

            skip = skip + take;
            url = BuildUrl(tipo, jurisdiccion, provincia, skip, take);

            var response = await http.GetAsync(url, cancellation);
            if (!response.IsSuccessStatusCode)
                break;

            json = await response.Content.ReadAsStringAsync(cancellation);
            jq = await JQ.ExecuteAsync(json, ThisAssembly.Resources.Argentina.SearchResults.Text);
            result = JsonOptions.Default.TryDeserialize<SearchResults>(jq);
            if (result == null)
                break;
        }
    }

    /// <summary>
    /// Loads the document corresponding to a search result.
    /// </summary>
    /// <returns>The located document.</returns>
    /// <exception cref="ArgumentException">The document was not found with the given item ID.</exception>
    /// <exception cref="NotSupportedException">The document was found but its content type or data is not supported.</exception>
    /// <seealso cref="ContentType/>
    public async Task<Document> LoadAsync(SearchResult item)
    {
        using var http = httpFactory.CreateClient("saij");
        var (data, jq, doc) = await ReadDocument(item.Id, http);
        return doc with
        {
            Json = data,
            JQ = jq,
            Query = item.Query,
        };
    }

    /// <summary>
    /// Retrieves full document content by ID.
    /// </summary>
    /// <param name="id">Either the SAIJ identifier or the document UUID.</param>
    /// <param name="http">Optional http client, defaults to a transient one.</param>
    /// <returns>The located document.</returns>
    /// <exception cref="ArgumentException">The document was not found with the given ID.</exception>
    /// <exception cref="NotSupportedException">The document was found but its content type or data is not supported.</exception>
    /// <seealso cref="ContentType/>
    public async Task<Document> LoadAsync(string id)
    {
        using var http = httpFactory.CreateClient("saij");

        // Resolve SAIJ ID > UUID if needed
        if (!id.Contains('-'))
        {
            var json = await http.GetStringAsync($"https://www.saij.gob.ar/busqueda?r=(id-infojus%3A{id})&f=Total");
            if (await JQ.ExecuteAsync(json, ThisAssembly.Resources.Argentina.SearchResults.Text) is { Length: > 0 } search &&
                JsonOptions.Default.TryDeserialize<SearchResults>(search) is { } result &&
                result.Total == 1)
            {
                id = result.Docs[0].Uuid;
            }
        }

        var (data, jq, doc) = await ReadDocument(id, http);
        return doc with
        {
            JQ = jq,
            Json = data,
        };
    }

    static async Task<(string data, string jq, Document doc)> ReadDocument(string id, HttpClient http)
    {
        var response = await http.GetAsync("https://www.saij.gob.ar/view-document?guid=" + id);
        // Even for invalid document ids, the server returns a 200 status code with empty data.
        response.EnsureSuccessStatusCode();

        var raw = await response.Content.ReadAsStringAsync();
        var data = JsonNode.Parse(raw)?["data"]?.GetValue<string>();
        if (string.IsNullOrEmpty(data))
            throw new ArgumentException($"Document not found with ID '{id}.", nameof(id));

        if (await JQ.ExecuteAsync(data, ThisAssembly.Resources.Argentina.SaijIdType.Text) is not { } idjq ||
            JsonOptions.Default.TryDeserialize<IdType>(idjq) is not { } idType)
        {
            throw new NotSupportedException($"Invalid document data for ID '{id}'.");
        }

        if (!DisplayValue.TryParse<ContentType>(idType.Type, true, out var contentType))
            throw new NotSupportedException($"Unsupported document content type '{idType.Type}' with ID '{id}'.");

        if (await JQ.ExecuteAsync(data, ThisAssembly.Resources.Argentina.SaijDocument.Text) is not { } docjq ||
            JsonOptions.Default.TryDeserialize<Document>(docjq) is not { } doc)
            throw new NotSupportedException($"Invalid document data for ID '{id}'.");

        return (data, docjq, doc);
    }

    static string BuildUrl(TipoNorma? tipo, Jurisdiccion? jurisdiccion, Provincia? provincia, int skip, int take) => string.Format(
        CultureInfo.InvariantCulture, UrlFormat, skip, take,
        tipo == null ? "|Tipo+de+Documento/Legislación" : $"|Tipo+de+Documento/Legislación/{DisplayValue.ToString(tipo.Value)}",
        tipo == TipoNorma.Ley || tipo == TipoNorma.Decreto ? "|Estado+de+Vigencia/Vigente,+de+alcance+general" : "",
        provincia == null ?
            jurisdiccion == null ? "" : $"|Jurisdicción/{DisplayValue.ToString(jurisdiccion.Value)}" :
            $"|Jurisdicción/{DisplayValue.ToString(Jurisdiccion.Provincial)}/{DisplayValue.ToString(provincia.Value)}");
}