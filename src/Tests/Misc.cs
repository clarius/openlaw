using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Azure.Storage;
using Azure.Storage.Blobs;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Clarius.OpenLaw;

public class Misc
{
    [Fact(Skip = "Doesn't actually work")]
    public void CanMutateSnapshotSettings()
    {
        var configuration = new ConfigurationBuilder()
            .Add(new InMemoryConfigurationSource(new()))
            .Build();

        var collection = new ServiceCollection();
        collection.AddSingleton<IConfiguration>(configuration);
        collection.Configure<AgentSettings>(configuration.GetSection("CheBoga"));
        //.BindConfiguration("CheBoga")
        //.ValidateDataAnnotations();

        var services = collection.BuildServiceProvider();
        var options = services.GetRequiredService<IOptionsSnapshot<AgentSettings>>();
        var monitor = services.GetRequiredService<IOptionsMonitor<AgentSettings>>();

        monitor.OnChange(x =>
        {
            // this is called when the configuration changes
            // we can use this to update the settings in the snapshot
            // but we need to be careful not to create a loop
            // because this will be called every time we change the settings
            x.TimeToLive = 900;
        });

        Assert.Equal(900, options.Value.TimeToLive);

        var provider = configuration.Providers.OfType<InMemoryConfigurationProvider>().FirstOrDefault();
        provider?.Set("CheBoga:TimeToLive", "1200");

        Assert.Equal(1200, options.Value.TimeToLive);
    }

    [SecretsFact("Azure:Storage:cheboga")]
    public async Task CanReadBlobMetadataAsAttributes()
    {
        var blob = CreateBlobClient("https://cheboga.blob.core.windows.net/normas/123456789-0abc-023-0000-4202soterced.json");
        using var http = new HttpClient();
        var response = await http.SendAsync(new HttpRequestMessage(HttpMethod.Get, blob.Uri));
        response.EnsureSuccessStatusCode();

        // convert to case-insensitive dictionary the headers that start with "x-ms-meta-" and remove the prefix
        // this is how blob storage represents custom file metadata
        var metadata = response.Headers
            .Where(h => h.Key.StartsWith("x-ms-meta-", StringComparison.OrdinalIgnoreCase))
            .ToDictionary(h => h.Key[10..], h => h.Value.FirstOrDefault(), StringComparer.OrdinalIgnoreCase);

        Assert.True(metadata.ContainsKey("source"));
    }


    BlobClient CreateBlobClient(string url)
    {
        var uri = new Uri(url);
        var account = uri.Host.Split('.')[0];
        var configuration = new ConfigurationBuilder().AddUserSecrets<Misc>().Build();
        var key = configuration["Azure:Storage:" + account] ?? throw new InvalidOperationException($"Missing access key to storage account '{account}'");
        var parts = uri.AbsolutePath.Split('/', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        var container = parts[0];
        var path = string.Join('/', parts[1..]);
        var credential = new StorageSharedKeyCredential(account, key);
        var client = new BlobServiceClient(new Uri(uri.GetComponents(UriComponents.SchemeAndServer, UriFormat.Unescaped)), credential);

        return client.GetBlobContainerClient(container).GetBlobClient(path);
    }
}
