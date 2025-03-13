using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using YamlDotNet.Serialization;

namespace Clarius.OpenLaw.Argentina;

public static partial class DictionaryConverter
{
    static readonly Lock sync = new();

    static readonly JsonSerializerOptions options = new()
    {
        Converters = { new JsonDictionaryConverter() },
    };


    static readonly ISerializer serializer = new SerializerBuilder()
        .WithTypeConverter(new YamlDictionaryConverter())
        .WithTypeConverter(new YamlListConverter())
        .WithTypeConverter(new YamlDateOnlyConverter())
        .ConfigureDefaultValuesHandling(DefaultValuesHandling.OmitNull | DefaultValuesHandling.OmitEmptyCollections)
        .Build();

    static readonly IDeserializer deserializer = new DeserializerBuilder().Build();

    public static void Convert(string jsonFile, bool yaml, bool pdf, bool markdown, bool overwrite)
    {
        var yamlDir = Path.Combine(Path.GetDirectoryName(jsonFile) ?? "", "yaml");
        var yamlFile = Path.Combine(yamlDir, Path.ChangeExtension(Path.GetFileName(jsonFile), ".yaml"));
        if (yaml)
            Directory.CreateDirectory(yamlDir);

        var mdDir = Path.Combine(Path.GetDirectoryName(jsonFile) ?? "", "md");
        var mdFile = Path.Combine(mdDir, Path.ChangeExtension(Path.GetFileName(jsonFile), ".md"));
        if (markdown)
            Directory.CreateDirectory(mdDir);

        var pdfDir = Path.Combine(Path.GetDirectoryName(jsonFile) ?? "", "pdf");
        var pdfFile = Path.Combine(pdfDir, Path.ChangeExtension(Path.GetFileName(jsonFile), ".pdf"));
        if (pdf)
            Directory.CreateDirectory(pdfDir);

        Dictionary<string, object?>? dictionary = null;
        var writeTime = File.GetLastWriteTimeUtc(jsonFile);

        if (overwrite || !File.Exists(yamlFile) ||
            File.Exists(yamlFile) && File.GetLastWriteTimeUtc(yamlFile) != writeTime)
        {
            dictionary = Parse(File.ReadAllText(jsonFile));
            if (dictionary is null)
                return;

            File.WriteAllText(yamlFile, dictionary.ToYaml(), Encoding.UTF8);
        }

        // Always ensure write time matches source json file
        File.SetLastWriteTimeUtc(yamlFile, writeTime);

        if (overwrite || !File.Exists(mdFile) ||
            File.Exists(mdFile) && File.GetLastWriteTimeUtc(mdFile) != writeTime)
        {
            dictionary ??= Parse(File.ReadAllText(jsonFile));
            if (dictionary is null)
                return;

            File.WriteAllText(mdFile, dictionary.ToMarkdown(), Encoding.UTF8);
        }

        // Always ensure write time matches source json file
        File.SetLastWriteTimeUtc(mdFile, writeTime);

        if (overwrite || !File.Exists(pdfFile) ||
            File.Exists(pdfFile) && File.GetLastWriteTimeUtc(pdfFile) != writeTime)
        {
            dictionary ??= Parse(File.ReadAllText(jsonFile));
            if (dictionary is null)
                return;

            if (!File.Exists(mdFile))
            {
                File.WriteAllText(mdFile, dictionary.ToMarkdown(), Encoding.UTF8);
                File.SetLastWriteTimeUtc(mdFile, writeTime);
            }

            // single-threaded usage of the markdown2pdf converter
            string? converted = null;
            lock (sync)
            {
                // Force sync execution since we can't lock and await.
                // Should work ok because it's a console app.
                converted = new Markdown2Pdf.Markdown2PdfConverter().Convert(mdFile).Result;
            }
            File.Move(converted, pdfFile, overwrite: true);
        }

        // Always ensure write time matches source json file
        File.SetLastWriteTimeUtc(pdfFile, writeTime);
    }

    public static Dictionary<string, object?>? Parse(string json)
        => JsonSerializer.Deserialize<Dictionary<string, object?>>(json, options);

    public static string ToYaml(this object? value)
    {
        if (value is null)
            return string.Empty;

        return serializer.Serialize(value).Trim();
    }

    public static Dictionary<string, object?> FromYaml(string yaml)
        => deserializer.Deserialize<Dictionary<string, object?>>(yaml);

    public static Dictionary<string, object?> FromMarkdown(string markdown)
    {
        var yaml = YamlExpr().Match(markdown);
        if (!yaml.Success)
            return [];

        return FromYaml(yaml.Groups["front"].Value + yaml.Groups["back"].Value);
    }

    public static string ToMarkdown(this Dictionary<string, object?> dictionary, bool renderLinks = true)
        => dictionary.ToMarkdown(out _, renderLinks);

    public static string ToMarkdown(this Dictionary<string, object?> dictionary, out List<Link> links, bool renderLinks = true)
    {
        var output = new StringBuilder();
        links = new List<Link>();
        ProcessDictionary(0, dictionary, output, links);

        if (renderLinks && links.Count > 0)
        {
            output.AppendLine().AppendLine("<a id=\"attachments\"></a>").AppendLine("---");

            if (links.Count > 1)
                output.AppendLine("**Archivos adjuntos**: ");
            else
                output.AppendLine("**Archivo adjunto**: ");

            foreach (var link in links)
            {
                output.AppendLine($"[{link.Name}]({link.Url})");
            }
        }

        return output.ToString().Trim();
    }

    [GeneratedRegex("(---(?<front>.*?)---)?.*?(\\<!--(?<back>.*?)--\\>)?", RegexOptions.Singleline)]
    private static partial Regex YamlExpr();

    static void ProcessObject(int depth, object? obj, StringBuilder output, List<Link> links)
    {
        if (obj is Dictionary<string, object?> dictionary)
        {
            ProcessDictionary(depth, dictionary, output, links);
        }
        else if (obj is List<object?> list)
        {
            foreach (var item in list)
            {
                ProcessObject(depth, item, output, links);
            }
        }
    }

    static void ProcessDictionary(int depth, Dictionary<string, object?> dictionary, StringBuilder output, List<Link> links)
    {
        var title = dictionary
            .Where(x => x.Key.StartsWith("titulo-", StringComparison.OrdinalIgnoreCase))
            .FirstOrDefault().Value;

        if (title is not null)
        {
            depth++;
            output.AppendLine().AppendLine($"{new string('#', depth)} {title}");
        }

        foreach (var kvp in dictionary)
        {
            var key = kvp.Key;
            var value = kvp.Value;
            if (value is null)
                continue;

            if (key == "texto" &&
                // We may have section title with text without an article #
                (dictionary.ContainsKey("numero-articulo") || title is not null))
            {
                output.AppendLine();

                if (dictionary.TryGetValue("numero-articulo", out var number))
                    output.AppendLine($"<a id=\"{number}\"></a>");

                output.AppendLine(value.ToString());
            }
            else if (key == "d_link" && value is Dictionary<string, object?> values &&
                values.TryGetValue("filename", out var fnObj) &&
                fnObj is string filename &&
                values.TryGetValue("uuid", out var uuidObj) &&
                uuidObj is string uuid)
            {
                links.Add(new Link($"https://www.saij.gob.ar/descarga-archivo?guid={uuid}&name={filename}", filename));
            }
            else
            {
                ProcessObject(depth, value, output, links);
            }
        }

        if (title is not null)
        {
            depth--;
        }
    }
}
