using System.Text;
using System.Text.Json;
using static Clarius.OpenLaw.DictionaryConverter;

namespace Clarius.OpenLaw.Argentina;

/// <summary>
/// Converts from the JSON data format in SAIJ to various output formats.
/// </summary>
public class JsonDataConverter
{
#if NET9_0_OR_GREATER
    static readonly Lock sync = new();
#else
    static readonly object sync = new();
#endif

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

}
