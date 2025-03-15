using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using Devlooped;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http.Resilience;
using Xunit;
using Xunit.Abstractions;

namespace Clarius.OpenLaw.Argentina;

public class SaijClientTests(ITestOutputHelper output)
{
    static readonly JsonSerializerOptions writeIndented = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true
    };

    public static SaijClient CreateClient(ITestOutputHelper output, Action<ProgressMessage>? report = default)
    {
        var services = new ServiceCollection()
            .ConfigureHttpClientDefaults(defaults => defaults.ConfigureHttpClient(http =>
            {
                http.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue(ThisAssembly.Info.Product, ThisAssembly.Info.InformationalVersion));
                if (Debugger.IsAttached)
                    http.Timeout = TimeSpan.FromMinutes(10);
                else
                    http.Timeout = TimeSpan.FromSeconds(60);
            }).AddStandardResilienceHandler());

        services.AddHttpClient("saij")
            .AddStandardResilienceHandler(config => config.Retry.ShouldHandle = args => new ValueTask<bool>(
                // We'll get a 403 if we hit the rate limit, so we'll consider that transient.
                (args.Outcome.Result?.StatusCode == System.Net.HttpStatusCode.Forbidden ||
                HttpClientResiliencePredicates.IsTransient(args.Outcome)) &&
                // We'll get a 500 error when enumerating past the available items too :/
                args.Outcome.Result?.StatusCode != System.Net.HttpStatusCode.InternalServerError));

        return new SaijClient(
            services.BuildServiceProvider().GetRequiredService<IHttpClientFactory>(),
            new Progress<ProgressMessage>(x =>
            {
                output.WriteLine($"{x.Value}% {x.Message}");
                report?.Invoke(x);
            }));
    }

    [Fact]
    public async Task WhenRetrievingTwiceReturnsSameTimestamp()
    {
        var client = CreateClient(output);
        JsonNode? first = default;

        await foreach (var doc in client.SearchAsync())
        {
            first = JsonNode.Parse(doc.Json);
            break;
        }

        Assert.NotNull(first);

        JsonNode? second = default;
        var id = first["document"]?["metadata"]?["uuid"]?.GetValue<string>();
        Assert.NotNull(id);

        await foreach (var doc in client.SearchAsync())
        {
            var id2 = doc.Id;
            if (id == id2)
            {
                second = JsonNode.Parse(doc.Json);
                break;
            }
        }

        Assert.NotNull(second);
        Assert.Equal(
            first["document"]?["metadata"]?["timestamp"]?.GetValue<long>(),
            second["document"]?["metadata"]?["timestamp"]?.GetValue<long>());
    }

    [Fact]
    public async Task CanDeserializeEnumeratedDocs()
    {
        var client = CreateClient(output);
        var count = 0;

        await foreach (var doc in client.SearchAsync())
        {
            count++;
            if (count == 10)
                break;
        }

        Assert.Equal(10, count);
    }

    [DebuggerFact]
    public async Task AllDocsContainPublication()
    {
        var total = 0;
        var nopub = 0;
        var fullpub = 0;

        var client = CreateClient(output, progress =>
        {
            if (progress.Message.Contains("Fetched full doc"))
                fullpub++;
        });

        await foreach (var item in client.SearchAsync())
        {
            total++;
            if (await client.LoadAsync(item) is { } doc &&
                doc.Publication is null)
            {
                nopub++;
                var json = doc.Json;
                Assert.NotNull(json);
                var pub = await JQ.ExecuteAsync(json, ".document.content[\"publicacion-codificada\"]");
                output.WriteLine($"{item.Id}: {pub}");
            }
        }

        // Assert.Equal(0, count);
        output.WriteLine($"Total: {total}, NoPub: {nopub}, FullPub: {fullpub}");
    }

    [DebuggerFact]
    public async Task NoArticleContainsSegments()
    {
        var client = CreateClient(output);
        await foreach (var item in client.SearchAsync())
        {
            if (await client.LoadAsync(item) is { } doc)
            {
                var segments = await JQ.ExecuteAsync(doc.Json,
                    ".document.content.articulo | .. | .segmento? | select(. != null)");

                Assert.True(string.IsNullOrEmpty(segments), $"Expected null for document {item.Id}");
            }
        }
    }

    [Fact]
    public async Task CanEnumerateDocs()
    {
        var client = CreateClient(output);
        var count = 0;

        await foreach (var doc in client.SearchAsync())
        {
            count++;
            if (count == 10)
                break;
        }

        Assert.Equal(10, count);
    }

    [Theory]
    [InlineData(TipoNorma.Ley, Jurisdiccion.Nacional)]
    [InlineData(TipoNorma.Decreto, Jurisdiccion.Nacional)]
    [InlineData(TipoNorma.Ley, Jurisdiccion.Internacional)]
    public async Task CanApplyFilters(TipoNorma tipo, Jurisdiccion jurisdiccion)
    {
        var total1 = 0L;
        var total2 = 0L;
        var client1 = CreateClient(output, progress => total1 = progress.Total);
        var client2 = CreateClient(output, progress => total2 = progress.Total);

        await foreach (var doc in client1.SearchAsync(tipo, jurisdiccion, filters:
            new Dictionary<string, string>().AddFilter(KnownFilters.EstadoDeVigencia.VigenteDeAlcanceGeneral)))
        {
            break;
        }

        await foreach (var doc in client2.SearchAsync(tipo, jurisdiccion))
        {
            break;
        }

        Assert.NotEqual(0, total1);
        Assert.NotEqual(0, total2);

        Assert.True(total1 < total2);
    }

    [DebuggerFact]
    public async Task EnumerateAllDocs()
    {
        var client = CreateClient(output);
        var count = 0;
        var watch = Stopwatch.StartNew();

        await foreach (var doc in client.SearchAsync(TipoNorma.Acordada))
        {
            var json = await client.LoadAsync(doc);
            Assert.NotNull(json);
            output.WriteLine(json.Json);
            count++;
        }

        Assert.True(count >= 5550, "Did not get expected docs or greater");
        watch.Stop();
        output.WriteLine($"Elapsed: {watch.Elapsed}");
    }

    [LocalTheory]
    [InlineData("123456789-0abc-defg-g21-62000tcanyel")]
    [InlineData("123456789-0abc-defg-g24-62000tcanyel")]
    [InlineData("123456789-0abc-defg-g23-85000scanyel")]
    [InlineData("123456789-0abc-defg-g56-95000scanyel")]
    [InlineData("123456789-0abc-defg-704-6000xvorpyel")]
    [InlineData("123456789-0abc-defg-382-5100bvorpyel")]
    [InlineData("123456789-0abc-945-54ti-lpssedadevon")]
    [InlineData("123456789-0abc-934-54ti-lpssedadevon")]
    [InlineData("123456789-0abc-281-0000-5202bvorpced")]
    [InlineData("123456789-0abc-430-0000-5202kvorpced")]
    [InlineData("123456789-0abc-104-0000-4202xvorpced")]
    [InlineData("123456789-0abc-defg-g07-67000tcanyel")]
    [InlineData("123456789-0abc-defg-g81-87000tcanyel")]
    [InlineData("123456789-0abc-defg-g29-54000scanyel")]
    [InlineData("LNS0004592")]
    public async Task CanFetchSpecificById(string id)
    {
        var client = CreateClient(output);

        var doc = await client.LoadAsync(id);

        Assert.NotNull(doc);

        await WriteAsync(client, doc, @$"..\..\..\Argentina\SaijSamples");
    }

    [DebuggerFact]
    public async Task CollectDocsWithLinks()
    {
        var client = CreateClient(output);
        var count = 0;
        await foreach (var doc in client.SearchAsync())
        {
            var raw = await client.LoadAsync(doc);
            Assert.NotNull(raw);
            var json = await JQ.ExecuteAsync(raw.Json, ".document.content.d_link // empty");
            if (string.IsNullOrEmpty(json))
                continue;

            var link = JsonSerializer.Deserialize<Link>(json);
            if (link == null)
            {
                output.WriteLine($"Failed to parse link from {json}");
                continue;
            }

            File.AppendAllText(@$"..\..\..\Argentina\SaijSamples\links.txt",
                $"{doc.Id}: {link.filename} {link.uuid}\n",
                System.Text.Encoding.UTF8);

            count++;
            if (count == 10)
                break;
        }
    }

    record Link(string filename, string uuid);

    [Theory]
    [InlineData("123456789-0abc-317-54ti-lpssedadevon")]
    [InlineData("123456789-0abc-517-54ti-lpssedadevon")]
    [InlineData("123456789-0abc-017-54ti-lpssedadevon")]
    [InlineData("123456789-0abc-117-54ti-lpssedadevon")]
    [InlineData("123456789-0abc-217-54ti-lpssedadevon")]
    [InlineData("123456789-0abc-896-54ti-lpssedadevon")]
    [InlineData("123456789-0abc-507-54ti-lpssedadevon")]
    [InlineData("123456789-0abc-996-54ti-lpssedadevon")]
    [InlineData("123456789-0abc-796-54ti-lpssedadevon")]
    [InlineData("123456789-0abc-127-54ti-lpssedadevon")]
    [InlineData("123456789-0abc-417-54ti-lpssedadevon")]
    [InlineData("123456789-0abc-696-54ti-lpssedadevon")]
    [InlineData("123456789-0abc-396-54ti-lpssedadevon")]
    [InlineData("123456789-0abc-876-54ti-lpssedadevon")]
    [InlineData("123456789-0abc-496-54ti-lpssedadevon")]
    public async Task WhenDocHasLinkThenAddsToMarkdown(string id)
    {
        var client = CreateClient(output);
        var doc = await client.LoadAsync(id);

        var markdown = doc.ToMarkdown(true);

        Assert.NotNull(markdown);
        Assert.Matches(@"\[.*\]\(.*\)", markdown);

        await WriteAsync(client, doc, @$"..\..\..\Argentina\SaijSamples\md");
    }

    [Theory]
    [InlineData(TipoNorma.Ley)]
    [InlineData(TipoNorma.Decreto)]
    [InlineData(TipoNorma.Resolucion)]
    [InlineData(TipoNorma.Disposicion)]
    [InlineData(TipoNorma.Decision)]
    [InlineData(TipoNorma.Acordada)]
    public async Task CanEnumerateAllTypes(TipoNorma tipo)
    {
        var client = CreateClient(output);
        await foreach (var doc in client.SearchAsync(tipo, null))
        {
            Assert.Equal(tipo, doc.Query.Tipo);

            var full = await WriteAsync(client, doc, $@"..\..\..\Argentina\SaijSamples\{tipo}");
            Assert.Equal(doc.Timestamp, full.Timestamp);
            return;
        }

        Assert.Fail("Did not get at least one document of the specified type");
    }

    [Theory]
    [MemberData(nameof(ForJurisdiction), TipoNorma.Ley)]
    [MemberData(nameof(ForJurisdiction), TipoNorma.Decreto)]
    public async Task CanEnumerateAllJurisdictions(TipoNorma tipo, Provincia provincia)
    {
        var client = CreateClient(output);
        await foreach (var doc in client.SearchAsync(tipo, Jurisdiccion.Provincial, provincia))
        {
            Assert.Equal(tipo, doc.Query.Tipo);

            var full = await WriteAsync(client, doc, $@"..\..\..\Argentina\SaijSamples\{tipo}\{provincia}");
            // Some documents don't have a timestamp, so we can't compare. We'd need fetching the full doc.
            // in these cases. Would be slower to sync too.
            if (doc.Timestamp != null)
                Assert.Equal(doc.Timestamp, full.Timestamp);

            return;
        }

        Assert.Fail("Did not get at least one document of the specified type");
    }

    // Ley Bases
    [Theory]
    [InlineData("LNS0007682")]
    [InlineData("123456789-0abc-defg-g28-67000scanyel")]
    public async Task CanFetchByIdOrUuidAndCheckReferences(string id)
    {
        var client = CreateClient(output);
        var doc = await client.LoadAsync(id);
        Assert.NotNull(doc);

        Assert.NotNull(doc.References);
        Assert.NotEmpty(doc.References.Repeals.To);
        Assert.NotEmpty(doc.References.Remarks.To);
        Assert.NotEmpty(doc.References.Ammends.To);

        await WriteAsync(client, doc, "../../../Argentina/SaijSamples");
    }

    // Ley Bases
    [Theory]
    [InlineData("123456789-0abc-defg-g28-67000scanyel")]
    public async Task CanConvertFromMarkdown(string id)
    {
        var client = CreateClient(output);
        var doc = await client.LoadAsync(id);
        var markdown = doc.ToMarkdown(true);

        var data = DictionaryConverter.FromMarkdown(markdown);

        Assert.NotNull(data);
        Assert.Contains("SAIJ", data.Keys);
    }

    [LocalTheory(Skip = "No errors for now. Activate as needed.")]
    [MemberData(nameof(LoadErrorData), 10)]
    public async Task CanLoadFormerErrors(string id)
    {
        var client = CreateClient(output);
        var doc = await client.LoadAsync(id);
        Assert.NotNull(doc);
        output.WriteLine(doc.Timestamp.ToString());
    }

    public static TheoryData<string> LoadErrorData(int count) => new TheoryData<string>(LoadErrorIds().Take(count));

    static IEnumerable<string> LoadErrorIds()
    {
        var dir = new DirectoryInfo("../../../Argentina/SaijSamples/errors").FullName;
        if (!Directory.Exists(dir))
            yield break;

        foreach (var error in Directory.EnumerateFiles("../../../Argentina/SaijSamples/errors", "*.yml"))
        {
            var yaml = File.ReadAllText(error);
            var data = DictionaryConverter.FromYaml(yaml);
            yield return (string)((Dictionary<object, object?>)data["Item"]!)["Id"]!;
        }
    }

    static async Task<Document> WriteAsync(SaijClient client, SearchResult item, string directory)
    {
        var doc = await WriteAsync(client, await client.LoadAsync(item), directory);
        Assert.NotNull(doc);
#if DEBUG
        await File.WriteAllTextAsync(@$"{directory}\{item.Id}-idx.json",
            item.Json,
            System.Text.Encoding.UTF8);
#endif
        return doc;
    }

    static async Task<Document> WriteAsync(SaijClient client, Document doc, string directory)
    {
#if DEBUG
        Directory.CreateDirectory(directory);

        await File.WriteAllTextAsync(@$"{directory}\{doc.Id}.json", doc.Json, System.Text.Encoding.UTF8);

        var markdown = doc.ToMarkdown();

        await File.WriteAllTextAsync(@$"{directory}\{doc.Id}.md", markdown, System.Text.Encoding.UTF8);

        var yaml = DictionaryConverter.ToYaml(doc.Data);

        await File.WriteAllTextAsync(@$"{directory}\{doc.Id}.yml", yaml, System.Text.Encoding.UTF8);

        var pdf = new Markdown2Pdf.Markdown2PdfConverter();
        await pdf.Convert(@$"{directory}\{doc.Id}.md");
#endif
        return doc;
    }

    public static IEnumerable<object[]> ForJurisdiction(TipoNorma tipo)
    {
        foreach (var jurisdiccion in Enum.GetValues<Provincia>())
            yield return new object[] { tipo, jurisdiccion };
    }
}