using System;
using System.ClientModel;
using System.ComponentModel;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Moq;
using OpenAI.Responses;

#pragma warning disable OPENAI001 // Experimental OpenAI APIs

namespace Clarius.OpenLaw;

public class ConversationTests : IDisposable
{
    readonly ITestOutputHelper output;
    readonly OpenAI.OpenAIClient client;
    readonly string storeId;
    readonly string fileId;

    static readonly string Instructions = File.ReadAllText("Content/Instructions.txt");

    readonly IConfiguration configuration = new ConfigurationBuilder()
        .AddUserSecrets<ConversationTests>()
        .Build();

    public ConversationTests(ITestOutputHelper output)
    {
        this.output = output;
        this.client = new OpenAI.OpenAIClient(configuration["OpenAI:Key"]);
        var store = client.GetVectorStoreClient().CreateVectorStore();
        storeId = store.Value.Id;

        var file = client.GetOpenAIFileClient().UploadFile("Content/LNS0004592.md", OpenAI.Files.FileUploadPurpose.Assistants);
        fileId = file.Value.Id;
    }

    public void Dispose()
    {
        var client = new OpenAI.OpenAIClient(configuration["OpenAI:Key"]);
        client.GetVectorStoreClient().DeleteVectorStore(storeId);
        client.GetOpenAIFileClient().DeleteFile(fileId);
    }

    // When new message received (of any type), determine active conversation
    //  - Gets conversation for user where last message timestamp >= 24hs
    //  - If no conversation found, create new conversation
    // Conversations have a list of attachments
    // If new message is a file, it's uploaded to the files repo and added to the conversation
    // If new message is a text, it's added to the conversation

    [SecretsTheory("OpenAI:Key")]
    [InlineData("¿Cómo asegura la ley 17.454 la imparcialidad y la competencia adecuada de los jueces en los procedimientos judiciales?")]
    //[InlineData("¿Qué medidas toma el CPCCN para garantizar que los procedimientos sean accesibles y equitativos para todas las partes, especialmente para aquellos con recursos limitados?")]
    [InlineData("¿Cómo maneja el Código Procesal Civil y Comercial las situaciones de rebeldía y qué impacto tiene esto en el resultado del proceso judicial?")]
    public async Task SemanticQueryingViaGrok(string question)
    {
        await AddLawToVector();

        var chat = new OpenAI.OpenAIClient(new ApiKeyCredential(configuration["OpenAI:Key"]!),
            new OpenAI.OpenAIClientOptions
            {
                //Endpoint = new Uri("https://api.x.ai/v1"),
            }).GetChatClient("gpt-4o-mini")
            .AsIChatClient()
            .AsBuilder()
            .UseFunctionInvocation(output.AsLoggerFactory())
            //.UseLogging(output.AsLoggerFactory())
            .Build();

        var oaioptions = new OpenAISettings
        {
            Agent = "cheboga",
            Key = configuration["OpenAI:Key"]!,
        };

        var vectors = Mock.Of<IVectorStoreService>(x =>
            x.GetStores(CancellationToken.None) == Task.FromResult<IEnumerable<VectorStore>>(new[] { new VectorStore(storeId) }));

        var search = new VectorSearchCommand(vectors, Options.Create(oaioptions));
        var define = new DefineCommand(vectors, Options.Create(oaioptions));

        // Configure chat options with tools
        var options = new ChatOptions
        {
            Tools =
            [
                //AIFunctionFactory.Create(define.Execute, name: "define"),
                AIFunctionFactory.Create(search.Execute, name: "search"),
                //AIFunctionFactory.Create(read)
            ]
        };

        //No asumir la definicion de acronismos o terminos tecnicos poco comunes, pero tampoco
        //mencionar que se utilizara una herramienta para resolverlo. 

        var response = await chat.GetResponseAsync(
            [
                new ChatMessage(ChatRole.System, Instructions),
                new ChatMessage(ChatRole.User, question),
            ], options);

        //output.WriteLine(JsonSerializer.Serialize(response));
        output.WriteLine("--------------------");
        output.WriteLine(response.Text);
    }

    [SecretsTheory("OpenAI:Key")]
    [InlineData("¿Cómo maneja el Código Procesal Civil y Comercial las situaciones de rebeldía y qué impacto tiene esto en el resultado del proceso judicial?")]
    public async Task ChatResponsesAPI(string question)
    {
        await AddLawToVector();

        var responses = new ResponsesClient("gpt-4.1-mini", configuration["OpenAI:Key"]);

        var client = responses.AsIChatClient(
                ResponseTool.CreateFileSearchTool([storeId]))
            .AsBuilder()
            //.UseFunctionInvocation(output.AsLoggerFactory())
            .UseLogging(output.AsLoggerFactory())
            .Build();

        var options = new ChatOptions();
        options.Instructions = Instructions;
        //options.StoredOutputEnabled = true;
        options.EndUserId = "kzu";

        var response = await client.GetResponseAsync(question, options);

        output.WriteLine(response.Text);

        var raw = response.Messages[0].RawRepresentation;
        var item = (OpenAI.Responses.MessageResponseItem)raw!;
        var citation = item.Content[0].OutputTextAnnotations.FirstOrDefault();

        //output.WriteLine(response.GetRawResponse().BufferContent().ToString());

        //var id = response.Value.PreviousResponseId;
        //var text = response.Value.GetOutputText();

        //var citations = response.Value.OutputItems.OfType<MessageResponseItem>()
        //    .Where(x => x.Role == MessageRole.Assistant)
        //    .SelectMany(x => x.Content.SelectMany(r => r.OutputTextAnnotations))
        //    .ToList();

        //var sb = new StringBuilder();

        //foreach (var citation in citations)
        //{
        //    sb.AppendLine(citation.UriCitationUri);
        //}

        //output.WriteLine("--- citations ---");
        //output.WriteLine(sb.ToString());
        //output.WriteLine("--- end citations ---");
    }

    async Task<System.ClientModel.Primitives.PipelineMessage> AddLawToVector()
    {
        var message = client.Pipeline.CreateMessage();
        var attributes = new Dictionary<string, object>()
        {
            { "original_url", "https://www.saij.gob.ar/LNS0004592" },
            { "source_url", "https://github.com/clarius/normas/blob/main/ley/LNS0004592.md" },
            { "title", "Ley 17.454" },
            { "description", "Código Procesal Civil y Comercial de la Nación" }
        };

        message.Request.Method = "POST";
        message.Request.Uri = new Uri($"https://api.openai.com/v1/vector_stores/{storeId}/files");
        message.Request.Headers.Add("OpenAI-Beta", "assistants=v2");
        var request = JsonSerializer.Serialize(new { file_id = fileId, attributes });
        message.Request.Content = BinaryContent.Create(BinaryData.FromString(request));

        await client.Pipeline.SendAsync(message);

        Assert.NotNull(message.Response);
        Assert.False(message.Response.IsError);

        var vectors = client.GetVectorStoreClient();

        var response = await vectors.GetVectorStoreFileAsync(storeId, fileId);
        while (response.Value.Status == global::OpenAI.VectorStores.VectorStoreFileStatus.InProgress)
        {
            await Task.Delay(200);
            response = await vectors.GetVectorStoreFileAsync(storeId, fileId);
            Assert.NotNull(message.Response);
            Assert.False(message.Response.IsError);
        }

        return message;
    }

    [Description("Reads the whole text of a given norm at the given URL.")]
    public static async Task<string> read(string url, CancellationToken cancellation = default)
    {
        using var httpClient = new HttpClient();
        try
        {
            return await httpClient.GetStringAsync(url, cancellation);
        }
        catch (Exception ex)
        {
            return $"Error reading document: {ex.Message}";
        }
    }
}