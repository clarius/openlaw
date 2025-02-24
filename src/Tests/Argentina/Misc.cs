using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using NuGet.Versioning;
using SharpYaml;
using Xunit;
using Xunit.Abstractions;
using YamlDotNet.Serialization;

namespace Clarius.OpenLaw.Argentina;

public partial class Misc(ITestOutputHelper output)
{
    [LocalTheory]
    [InlineData("Argentina/SaijSamples/123456789-0abc-defg-g23-85000scanyel.json")]
    [InlineData("Argentina/SaijSamples/123456789-0abc-defg-g81-87000tcanyel.json")]
    [InlineData("Argentina/SaijSamples/123456789-0abc-defg-g56-95000scanyel.json")]
    public void ConvertJsonToYaml(string jsonFile)
    {
        jsonFile = Path.Combine(ThisAssembly.Project.MSBuildProjectDirectory, jsonFile);
        var json = File.ReadAllText(jsonFile).ReplaceLineEndings();

        var dictionary = DictionaryConverter.Parse(json);
        Assert.NotNull(dictionary);

        var yaml = DictionaryConverter.ToYaml(dictionary);

        // Save the YAML to a file
        var yamlFile = Path.ChangeExtension(jsonFile, ".yaml");
        File.WriteAllText(yamlFile, yaml);
    }

    [LocalTheory]
    [InlineData("Argentina/SaijSamples/123456789-0abc-defg-g23-85000scanyel.json")]
    [InlineData("Argentina/SaijSamples/123456789-0abc-defg-g56-95000scanyel.json")]
    public void ConvertJsonToMarkdown(string jsonFile)
    {
        jsonFile = Path.Combine(ThisAssembly.Project.MSBuildProjectDirectory, jsonFile);
        var json = File.ReadAllText(jsonFile).ReplaceLineEndings();

        var dictionary = DictionaryConverter.Parse(json);
        Assert.NotNull(dictionary);

        var markdown = DictionaryConverter.ToMarkdown(dictionary);

        // Save the Markdown to a file
        var markdownFile = Path.ChangeExtension(jsonFile, ".md");
        File.WriteAllText(markdownFile, markdown);
    }

    [LocalTheory]
    [InlineData("Argentina/SaijSamples/123456789-0abc-defg-g23-85000scanyel.md")]
    [InlineData("Argentina/SaijSamples/123456789-0abc-defg-g56-95000scanyel.md")]
    public async Task ConvertJsonToPdfAsync(string markdownFile)
    {
        markdownFile = Path.Combine(ThisAssembly.Project.MSBuildProjectDirectory, markdownFile);
        var pdf = new Markdown2Pdf.Markdown2PdfConverter();
        var path = await pdf.Convert(markdownFile);

        output.WriteLine($"PDF file: {path}");
    }
}
