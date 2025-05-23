﻿using System.Diagnostics;
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

    public async Task<SearchResult?> SearchIdAsync(string id, CancellationToken token = default)
    {
        var url = $"https://www.saij.gob.ar/busqueda?r=(id-infojus:{id})&f=Total";
        using var http = httpFactory.CreateClient("saij");
        var response = await http.GetAsync(url, token);
        if (!response.IsSuccessStatusCode)
            return null;

        var json = await response.Content.ReadAsStringAsync(token);
        var jq = await JQ.ExecuteAsync(json, ThisAssembly.Resources.Argentina.SearchResults.Text);

        if (JsonOptions.Default.TryDeserialize<SearchResults>(jq) is not { } result)
            return null;
        if (result.Total == 0 || result.Docs.Length == 0)
            return null;
        if (result.Total > 1)
            throw new ArgumentException($"Multiple results found for ID '{id}'.", nameof(id));

        var item = result.Docs[0];

        if (await JQ.ExecuteAsync(item.Abstract, ThisAssembly.Resources.Argentina.SaijIdType.Text) is not { } idTypeJson ||
            JsonOptions.Default.TryDeserialize<IdType>(idTypeJson) is not { } idType)
            throw new ArgumentException($"Invalid document data for ID '{id}'.", nameof(id));

        if (!DisplayValue.TryParse<ContentType>(idType.Type, true, out _))
            throw new NotSupportedException($"Unsupported document content type '{idType.Type}' with ID '{id}'.");

        jq = await JQ.ExecuteAsync(item.Abstract, ThisAssembly.Resources.Argentina.SearchResult.Text);
        if (string.IsNullOrEmpty(jq))
        {
            AnsiConsole.MarkupLine($":cross_mark: [dim]{idType.Type}[/] [blue][link={idType.WebUrl}]{idType.Uuid}[/][/] ([blue][link={idType.DataUrl}]JSON[/][/])");
#if DEBUG
            Debugger.Launch();
            AnsiConsole.Write(new JsonText(item.Abstract));
#endif
            return null;
        }

        if (JsonOptions.Default.TryDeserialize<SearchResult>(jq) is not { } doc)
        {
            AnsiConsole.MarkupLine($":cross_mark: [dim]{idType.Type}[/] [blue][link={idType.WebUrl}]{idType.Uuid}[/][/] ([blue][link={idType.DataUrl}]JSON[/][/])");
#if DEBUG
            Debugger.Launch();
            AnsiConsole.Write(new JsonText(item.Abstract));
#endif
            return null;
        }

        var source = new Search(
            doc.DocumentType.Code == "LEY" ?
            TipoNorma.Ley :
            doc.DocumentType.Code == "DEC" ?
            TipoNorma.Decreto :
            // TODO: we don't attempt to convert the other types just yet.
            null, null, null);

        return doc with
        {
            Json = result.Docs[0].Abstract,
            JQ = jq,
            Query = source,
        };
    }

    // TODO: add int top value to stop the search after a certain number of results.
    public async IAsyncEnumerable<SearchResult> SearchAsync(
        TipoNorma? tipo = TipoNorma.Ley,
        Jurisdiccion? jurisdiccion = Jurisdiccion.Nacional,
        Provincia? provincia = null, IDictionary<string, string>? filters = null,
        int skip = 0, int take = 25, [EnumeratorCancellation] CancellationToken cancellation = default)
    {
        using var http = httpFactory.CreateClient("saij");
        var url = BuildUrl(tipo, jurisdiccion, provincia, filters, skip, take);

        var response = await http.GetAsync(url, cancellation);
        if (!response.IsSuccessStatusCode)
            yield break;

        var json = await response.Content.ReadAsStringAsync(cancellation);
        var jq = await JQ.ExecuteAsync(json, ThisAssembly.Resources.Argentina.SearchResults.Text);
        if (JsonOptions.Default.TryDeserialize<SearchResults>(jq) is not { } result)
            yield break;

        var source = new Search(tipo, jurisdiccion, provincia);
        var query = ThisAssembly.Resources.Argentina.SearchResult.Text;
        var total = result.Total;
        progress.Report(new($"Processing {result.Total} results", skip, total));

        while (true)
        {
            cancellation.ThrowIfCancellationRequested();

            if (result.Total == 0)
                break;

            var count = 0;
            foreach (var item in result.Docs)
            {
                count++;
                cancellation.ThrowIfCancellationRequested();

                // This is the bare minimum we expect all results to have.
                if (await JQ.ExecuteAsync(item.Abstract, ThisAssembly.Resources.Argentina.SaijIdType.Text) is not { } idType ||
                    JsonOptions.Default.TryDeserialize<IdType>(idType) is not { } id)
                {
                    progress.Report(new($"Skipping {skip + count} of {result.Total} (unsupported document)", skip + count, total));
                    continue;
                }

                // If it's not one of our supported content types, just skip.
                if (!DisplayValue.TryParse<ContentType>(id.Type, true, out _))
                {
                    progress.Report(new($"Skipping {skip + count} of {result.Total} (unspported content type '{id.Type}')", skip + count, total));
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

                progress.Report(new($"Processing {skip + count} of {result.Total} ([blue][link={id.WebUrl}]source[/][/])", skip + count, total));

                yield return doc with
                {
                    Json = item.Abstract,
                    JQ = jq,
                    Query = source,
                };
            }

            skip += take;
            if (skip > total)
                break;

            url = BuildUrl(tipo, jurisdiccion, provincia, filters, skip, take);

            response = await http.GetAsync(url, cancellation);
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
        var doc = await ReadDocument(item.Id, http);
        return doc with
        {
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

        return await ReadDocument(id, http);
    }

    static async Task<Document> ReadDocument(string id, HttpClient http)
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

        if (!DisplayValue.TryParse<ContentType>(idType.Type, true, out _))
            throw new NotSupportedException($"Unsupported document content type '{idType.Type}' with ID '{id}'.");

        var doc = await Document.ParseAsync(data);

        // Sanitize markup in summary, which is otherwise missing since we're not doing dictionary-based 
        // deserialization to Document. This is only needed because we're loading from original web source.
        // Document.ParseAsync does not do this.
        return doc with { Summary = StringMarkup.Cleanup(doc.Summary) };
    }

    static string BuildUrl(TipoNorma? tipo, Jurisdiccion? jurisdiccion, Provincia? provincia, IDictionary<string, string>? filters, int skip, int take) => string.Format(
        CultureInfo.InvariantCulture, UrlFormat, skip, take,
        tipo == null ? "|Tipo+de+Documento/Legislación" : $"|Tipo+de+Documento/Legislación/{DisplayValue.ToString(tipo.Value)}",
        filters is not { Count: > 0 } ? "" : "|" + string.Join('|', filters.Select(x => $"{x.Key.Replace(' ', '+')}/{x.Value.Replace(' ', '+')}")),
        provincia == null ?
            jurisdiccion == null ? "" : $"|Jurisdicción/{DisplayValue.ToString(jurisdiccion.Value)}" :
            $"|Jurisdicción/{DisplayValue.ToString(Jurisdiccion.Provincial)}/{DisplayValue.ToString(provincia.Value)}");
}