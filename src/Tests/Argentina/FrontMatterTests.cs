using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Clarius.OpenLaw.Argentina;

public class FrontMatterTests(ITestOutputHelper output)
{
    [LocalFact]
    public async Task CanRoundtripFrontMatter()
    {
        var client = SaijClientTests.CreateClient(output);
        var doc = await client.FetchDocumentAsync("123456789-0abc-defg-g23-85000scanyel");
        Assert.NotNull(doc);

        var raw = await client.FetchRawAsync(doc.Id);
        Assert.NotNull(raw);

        var dictionary = DictionaryConverter.Parse(raw);
        Assert.NotNull(dictionary);

        var markdown = DictionaryConverter.ToMarkdown(dictionary);
        var contents =
            $"""
            ---
            {doc.ToFrontMatter()}
            ---
            {markdown}
            <!-- 
            {doc.ToYaml()}
            -->            
            """;

        output.WriteLine(contents);
    }
}
