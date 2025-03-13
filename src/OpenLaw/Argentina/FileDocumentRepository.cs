namespace Clarius.OpenLaw.Argentina;

public class FileDocumentRepository
{
    readonly string rootDirectory;

    public FileDocumentRepository(string rootDirectory)
    {
        this.rootDirectory = rootDirectory;
        Directory.CreateDirectory(rootDirectory);
        Directory.CreateDirectory(Path.Combine(rootDirectory, "data"));
    }

    public async ValueTask<Document?> GetDocumentAsync(string id)
    {
        var file = Path.Combine(rootDirectory, "data", id + ".json");
        if (!File.Exists(file))
            return default;

        return await Document.ParseAsync(await File.ReadAllTextAsync(file));
    }

    public async ValueTask<long?> GetTimestampAsync(string id)
    {
        var file = Path.Combine(rootDirectory, "data", id + ".json");
        if (!File.Exists(file))
            return null;

        var json = await File.ReadAllTextAsync(file);
        if (await Devlooped.JQ.ExecuteAsync(json, ThisAssembly.Resources.Argentina.SaijTimestamp.Text) is { } jq &&
            long.TryParse(jq, out var timestamp))
            return timestamp;

        return null;
    }

    public async ValueTask<ContentAction> SetDocumentAsync(Document document)
    {
        var json = Path.Combine(rootDirectory, "data", document.Id + ".json");
        var md = Path.Combine(rootDirectory, document.Alias + ".md");
        var action = !File.Exists(json) || !File.Exists(md) ? ContentAction.Created : ContentAction.Updated;

        await File.WriteAllTextAsync(json, document.Json);
        await File.WriteAllTextAsync(md, document.ToMarkdown(true));

        return action;
    }
}
