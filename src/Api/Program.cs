using System.ClientModel;
using System.Net.Http.Headers;
using System.Text.Json;
using Azure.Storage.Queues;
using Clarius.OpenLaw;
using Devlooped;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OpenAI;
using OpenAI.Responses;

var builder = FunctionsApplication.CreateBuilder(args);

builder.ConfigureFunctionsWebApplication();
#if DEBUG
builder.Configuration.AddUserSecrets<Program>();
#endif

#if CI
builder.Services
    .AddApplicationInsightsTelemetryWorkerService()
    .ConfigureFunctionsApplicationInsights();
#endif

builder.Services.Configure<JsonSerializerOptions>(options =>
{
    options.AllowTrailingCommas = true;
    options.PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower;
    options.PropertyNameCaseInsensitive = true;
    options.WriteIndented = true;
});

builder.Services.AddOptions<OpenAISettings>()
    .BindConfiguration("OpenAI")
    .ValidateDataAnnotations();

builder.Services.AddOptions<AgentSettings>()
    .BindConfiguration("CheBoga")
    .ValidateDataAnnotations();

builder.Services.AddSingleton(services =>
{
    var options = services.GetRequiredService<IOptions<OpenAISettings>>().Value;
    return new OpenAIClient(new ApiKeyCredential(options.Key ?? "Missing required assistant key."));
});

builder.Services.AddKeyedSingleton("oai", (services, _) =>
{
    var options = services.GetRequiredService<IOptions<OpenAISettings>>().Value;
    return new OpenAIClient(new ApiKeyCredential(options.Key ?? "Missing required assistant key."));
});

builder.Services.AddSingleton(services =>
{
    return builder.Environment.IsDevelopment() ?
        CloudStorageAccount.DevelopmentStorageAccount :
        CloudStorageAccount.TryParse(builder.Configuration["App:Storage"] ?? "", out var storage) ?
        storage :
        throw new InvalidOperationException("Missing required App:Storage connection string.");
});

builder.Services.AddMemoryCache();
builder.Services.AddSingleton(services => services.GetRequiredService<CloudStorageAccount>().CreateBlobServiceClient());
builder.Services.AddSingleton(services => services.GetRequiredService<CloudStorageAccount>().CreateTableServiceClient());
builder.Services.AddSingleton(services => new QueueServiceClient(
    services.GetRequiredService<IConfiguration>()["AzureWebJobsStorage"]!,
    new QueueClientOptions { MessageEncoding = QueueMessageEncoding.Base64 }));

builder.Services.AddServices();

builder.Services.AddChatClient(services =>
{
    var options = services.GetRequiredService<IOptions<OpenAISettings>>().Value;
    var client = new ChatModelResponseClient(options.Model, options.Key,
        [.. options.Stores.Select(id => ResponseTool.CreateFileSearchTool([id]))]);

    return client
        .AsBuilder()
        .UseLogging(services.GetRequiredService<ILoggerFactory>())
        .UseOpenTelemetry()
        .UseComplexityAssessment(services.GetRequiredService<ComplexityAssessment>())
        .Build();
});

builder.Services.AddKeyedChatClient("complexity", services =>
{
    var configuration = services.GetRequiredService<IConfiguration>();
    var client = new OpenAIClient(new ApiKeyCredential(configuration.Get("Gemini:Key")), new OpenAIClientOptions
    {
        Endpoint = new Uri("https://generativelanguage.googleapis.com/v1beta/"),
    });

    return client
        .GetChatClient(configuration.Get("Gemini:Model"))
        .AsIChatClient()
        .AsBuilder()
        .UseLogging(services.GetRequiredService<ILoggerFactory>())
        .UseOpenTelemetry()
        .Build();
});

var section = builder.Configuration.GetSection("Meta");
var found = false;

foreach (var entry in section.AsEnumerable().Where(x => x.Key.StartsWith("Meta:Numbers:") && !string.IsNullOrEmpty(x.Value)))
{
    found = true;
    // get value after "Meta:Numbers:"
    var number = entry.Key["Meta:Numbers:".Length..];

    builder.Services.AddHttpClient($"whatsapp-{number}", (services, http) =>
    {
        http.BaseAddress = new Uri($"https://graph.facebook.com/v21.0/{number}/");
        http.Timeout = TimeSpan.FromMinutes(5);
        http.DefaultRequestHeaders.TryAddWithoutValidation("Authorization", $"Bearer {entry.Value}");
        http.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
    }).AddStandardResilienceHandler();
}

if (!found)
    throw new InvalidOperationException("💬 No Meta numbers configured.");

builder.UseWhatsApp();

builder.Build().Run();