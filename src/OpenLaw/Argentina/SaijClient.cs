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

    public async IAsyncEnumerable<DocumentAbstract> SearchAsync(
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
        if (Document.JsonOptions.TryDeserialize<SearchResults>(jq) is not { } result)
            yield break;

        var source = new Source(tipo, jurisdiccion, provincia);
        var query = ThisAssembly.Resources.Argentina.SaijAbstract.Text;

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
                    Document.JsonOptions.TryDeserialize<IdType>(idType) is not { } id)
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
                    AnsiConsole.MarkupLine($":cross_mark: [dim]{id.Type}[/] [blue][link={id.HtmlUrl}]{id.Id}[/][/] ([blue][link={id.JsonUrl}]JSON[/][/])");
#if DEBUG
                    Debugger.Launch();
                    AnsiConsole.Write(new JsonText(item.Abstract));
#endif
                    continue;
                }

                if (Document.JsonOptions.TryDeserialize<DocumentAbstract>(jq) is not { } doc)
                {
                    AnsiConsole.MarkupLine($":cross_mark: [dim]{id.Type}[/] [blue][link={id.HtmlUrl}]{id.Id}[/][/] ([blue][link={id.JsonUrl}]JSON[/][/])");
#if DEBUG
                    Debugger.Launch();
                    AnsiConsole.Write(new JsonText(item.Abstract));
#endif
                    continue;
                }

                percentage = (skip + count) * 100 / result.Total;
                progress.Report(new($"Processing {skip + count} of {result.Total}", percentage));

                yield return doc with
                {
                    Source = source,
                    Summary = StringMarkup.Cleanup(doc.Summary)
                };
            }

            skip = skip + take;
            url = BuildUrl(tipo, jurisdiccion, provincia, skip, take);

            var response = await http.GetAsync(url, cancellation);
            if (!response.IsSuccessStatusCode)
                break;

            json = await response.Content.ReadAsStringAsync(cancellation);
            jq = await JQ.ExecuteAsync(json, ThisAssembly.Resources.Argentina.SaijSearch.Text);
            result = Document.JsonOptions.TryDeserialize<SearchResults>(jq);
            if (result == null)
                break;
        }
    }

    public async Task<Document> FetchAsync(string id, HttpClient? http = default)
    {
        var dispose = http == null;
        try
        {
            http ??= httpFactory.CreateClient("saij");
            var response = await http.GetAsync("https://www.saij.gob.ar/view-document?guid=" + id);
            if (!response.IsSuccessStatusCode)
                throw new ArgumentException($"Document not found with ID '{id}.", nameof(id));

            var doc = await response.Content.ReadAsStringAsync();
            var data = JsonNode.Parse(doc)?["data"]?.GetValue<string>();
            if (string.IsNullOrEmpty(data))
                throw new ArgumentException($"Invalid document data for '{id}'.");

            if (await JQ.ExecuteAsync(data, ThisAssembly.Resources.Argentina.SaijIdType.Text) is not { } jq ||
                Document.JsonOptions.TryDeserialize<IdType>(jq) is not { } idType)
            {
                throw new ArgumentException($"Invalid document data for ID '{id}'.");
            }

            if (!DisplayValue.TryParse<ContentType>(idType.Type, true, out var contentType))
                throw new ArgumentException($"Unsupported document content type '{idType.Type}' with ID '{id}'.");

            return new Document(idType.Id, contentType, data);
        }
        finally
        {
            if (dispose)
                http?.Dispose();
        }
    }

    static string BuildUrl(TipoNorma? tipo, Jurisdiccion? jurisdiccion, Provincia? provincia, int skip, int take) => string.Format(
        CultureInfo.InvariantCulture, UrlFormat, skip, take,
        tipo == null ? "|Tipo+de+Documento/Legislación" : $"|Tipo+de+Documento/Legislación/{DisplayValue.ToString(tipo.Value)}",
        tipo == TipoNorma.Ley || tipo == TipoNorma.Decreto ? "|Estado+de+Vigencia/Vigente,+de+alcance+general" : "",
        provincia == null ?
            jurisdiccion == null ? "" : $"|Jurisdicción/{DisplayValue.ToString(jurisdiccion.Value)}" :
            $"|Jurisdicción/{DisplayValue.ToString(Jurisdiccion.Provincial)}/{DisplayValue.ToString(provincia.Value)}");
}