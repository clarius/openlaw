using System.Diagnostics;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using Clarius.OpenLaw.Argentina;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PuppeteerSharp;
using Spectre.Console;
using Spectre.Console.Cli;
using static Spectre.Console.AnsiConsole;

namespace Clarius.OpenLaw;

public static class App
{
    public static CommandApp Create(out IServiceProvider services)
    {
        var collection = new ServiceCollection();

        var config = new ConfigurationBuilder()
            .AddEnvironmentVariables()
            .AddUserSecrets<TypeRegistrar>()
            .AddDotNetConfig()
            .Build();

        collection.AddSingleton(config)
            .AddSingleton<IConfiguration>(_ => config);

        collection.AddHttpClient()
            .ConfigureHttpClientDefaults(defaults => defaults.ConfigureHttpClient(http =>
                {
                    http.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue(ThisAssembly.Info.Product, ThisAssembly.Info.InformationalVersion));
                    if (Debugger.IsAttached)
                        http.Timeout = TimeSpan.FromMinutes(10);
                }).AddStandardResilienceHandler(options =>
                {
                    var retry = options.Retry.ShouldHandle;

                    static bool isFailedSearch(HttpResponseMessage? response)
                        => response is { IsSuccessStatusCode: false, RequestMessage.RequestUri.PathAndQuery: string path } && path.StartsWith("/busqueda?");

                    options.Retry.ShouldHandle = message => isFailedSearch(message.Outcome.Result) ? ValueTask.FromResult(false) : retry(message);
                }));

        var needsNewLine = false;
        collection.AddSingleton<IProgress<string>>(new Progress<string>(message =>
        {
            if (needsNewLine)
                WriteLine();

            Write(new Markup($"> 🧠 [grey]{message.EscapeMarkup()}...[/]"));
            needsNewLine = true;
        }));

        collection.AddSingleton(new JsonSerializerOptions(JsonSerializerDefaults.Web)
        {
            TypeInfoResolver = new DefaultJsonTypeInfoResolver(),
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) },
            WriteIndented = true,
            Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        });

        collection.UseArgentina();

        var registrar = new TypeRegistrar(collection);
        var app = new CommandApp(registrar);
        registrar.Services.AddSingleton<ICommandApp>(app);

        app.Configure(config =>
        {
            config.AddCommand<ConvertCommand>("convert");
            config.AddCommand<FormatCommand>("format");

            if (Environment.GetEnvironmentVariables().Contains("NO_COLOR"))
                config.Settings.HelpProviderStyles = null;
        });

        app.UseArgentina();
        services = registrar.Services.BuildServiceProvider();

        return app;
    }
}
