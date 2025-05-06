using System.ClientModel;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using Azure.Messaging.EventGrid;
using Azure.Storage;
using Azure.Storage.Blobs;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OpenAI;
using OpenAI.Files;

namespace Clarius.OpenLaw;

public class BlobStorage(ILogger<BlobStorage> log, VectorStoreService storeService,
    [FromKeyedServices("oai")] OpenAIClient oai, IConfiguration configuration,
    IHttpClientFactory httpFactory)
{
    static readonly JsonSerializerOptions options = new(JsonSerializerDefaults.Web)
    {
        Converters =
        {
            new JsonStringEnumConverter(),
        },
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        WriteIndented = true,
    };

    public record EventData(string Api, string Url);

    [Function("event_triggered")]
    public async Task RunEventAsync(
#if DEBUG
        [HttpTrigger][FromBody] EventGridEvent e
#else
        [EventGridTrigger] EventGridEvent e
#endif
        )
    {
#if DEBUG
        log.LogTrace(
            """
                Got event: 
                    Id: {Id} @ {Time}
                    Subject: {Subject}
                    Topic: {Topic}
                    Type: {Type}
                    Data: {Data}
                """, e.Id, e.EventTime, e.Subject, e.Topic, e.EventType, e.Data.ToString());
#endif

        var data = e.Data.ToObjectFromJson<EventData>(options);
        if (data is null)
        {
            log.LogWarning("Invalid event data: {Data}", e.Data.ToString());
            return;
        }

        var blob = CreateBlobClient(data);
        using var http = httpFactory.CreateClient();
        var response = await http.SendAsync(new HttpRequestMessage(HttpMethod.Get, data.Url));
        if (!response.IsSuccessStatusCode)
        {
            log.LogWarning("Failed to get {Url}: {Status}", data.Url, response.StatusCode);
            return;
        }

        // convert to case-insensitive dictionary the headers that start with "x-ms-meta-" and remove the prefix
        // this is how blob storage represents custom file metadata
        var metadata = response.Headers
            .Where(h => h.Key.StartsWith("x-ms-meta-", StringComparison.OrdinalIgnoreCase))
            .ToDictionary(h => h.Key[10..], h => h.Value.FirstOrDefault(), StringComparer.OrdinalIgnoreCase);

        // In OpenAI, we need to just delete the existing and add a new file.
        if (metadata.TryGetValue("FileId", out var fileId) &&
            metadata.TryGetValue("StoreId", out var storeId))
        {
            log.LogInformation("Deleting older file {File} from store", fileId);

            await oai.GetVectorStoreClient().RemoveFileFromStoreAsync(storeId, fileId);
            await oai.GetOpenAIFileClient().DeleteFileAsync(fileId);

            // clear the two fields from the blob so we don't try to delete it again
            metadata.Remove("FileId");
            metadata.Remove("StoreId");
            await blob.SetMetadataAsync(metadata);
        }
        else
        {
            log.LogInformation("No existing vector store info found");
        }

        var content = await response.Content.ReadAsStringAsync();
        // TODO: if any of the metadata is missing from the front-matter, we should fallback to fetching the JSON.
        var frontMatter = DictionaryConverter.FromMarkdown(content);

        if (!frontMatter.TryGetValue("Estado", out var estado) || !string.Equals("Vigente", estado?.ToString(), StringComparison.OrdinalIgnoreCase))
        {
            // NOTE: we have already deleted the file and store association if there was one, so we're done.
            log.LogInformation("Skipping norm {Url} (status: {Status})", data.Url, estado);
            return;
        }

        if (!frontMatter.TryGetValue("Fecha", out var dateMeta) || !DateOnly.TryParse(dateMeta?.ToString(), out var date))
        {
            log.LogWarning("Missing or invalid 'Fecha' front-matter in {Url}: {Fecha}", data.Url, dateMeta);
            return;
        }

        var filename = Path.GetFileName(data.Url);
        if (await storeService.GetStoreAsync(date) is not { } store)
        {
            log.LogWarning("No store found for document {Url} ({Date})", data.Url, date);
            return;
        }

        var title = frontMatter.TryGetValue("Título", out var titleMeta) ? titleMeta?.ToString() : filename;

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(content));
        var file = await oai.GetOpenAIFileClient().UploadFileAsync(stream, filename, FileUploadPurpose.Assistants);

        try
        {
            // To append metadata to the file association, we need to go lower-level since it's not supported in the SDK
            var message = oai.Pipeline.CreateMessage();

            message.Request.Method = "POST";
            message.Request.Uri = new Uri($"https://api.openai.com/v1/vector_stores/{store.Id}/files");
            message.Request.Headers.Add("OpenAI-Beta", "assistants=v2");

            var attributes = frontMatter
                .Where(x => x.Value is string or bool or int or double or float or decimal)
                .ToDictionary(x => x.Key, x => x.Value);

            if (frontMatter.TryGetValue("SAIJ", out var saijValue) && saijValue is string saij && !string.IsNullOrWhiteSpace(saij))
                attributes["original_url"] = saij;

            attributes["title"] = title;
            attributes["blob_url"] = data.Url;
            attributes["filename"] = filename;

            // Propagate other blob metadata specified by the CI/CD process (i.e. 'source').
            foreach (var blobMeta in metadata)
            {
                if (!attributes.ContainsKey(blobMeta.Key))
                    attributes[blobMeta.Key] = blobMeta.Value;
            }

            var request = JsonSerializer.Serialize(new { file_id = file.Value.Id, attributes }, options);
            message.Request.Content = BinaryContent.Create(BinaryData.FromString(request));

            await oai.Pipeline.SendAsync(message);

            if (message.Response is null || message.Response.IsError)
            {
                log.LogError("Failed to add file to vector store: {Payload}", message.Response?.Content.ToString());
                throw new InvalidOperationException($"Failed to add file to vector store: {message.Response?.Content.ToString()}");
            }
        }
        catch
        {
            await oai.GetOpenAIFileClient().DeleteFileAsync(file.Value.Id);
            throw;
        }

        // Update blob metadata.
        metadata["FileId"] = file.Value.Id;
        metadata["StoreId"] = store.Id;
        await blob.SetMetadataAsync(metadata);

        log.LogInformation("Added file {File} to store {Store} for {Title}", file.Value.Id, store.Id, title);
    }

    BlobClient CreateBlobClient(EventData data)
    {
        var uri = new Uri(data.Url);
        var account = uri.Host.Split('.')[0];
        var key = configuration["Azure:Storage:" + account] ?? throw new InvalidOperationException($"Missing access key to storage account '{account}'");
        var parts = uri.AbsolutePath.Split('/', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        var container = parts[0];
        var path = string.Join('/', parts[1..]);
        var credential = new StorageSharedKeyCredential(account, key);
        var client = new BlobServiceClient(new Uri(uri.GetComponents(UriComponents.SchemeAndServer, UriFormat.Unescaped)), credential);

        return client.GetBlobContainerClient(container).GetBlobClient(path);
    }
}
