using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http.Resilience;
using Spectre.Console.Cli;

namespace Clarius.OpenLaw.Argentina;

public static class ConfigurationExtensions
{
    public static IServiceCollection UseArgentina(this IServiceCollection services)
    {
        services.AddHttpClient("saij")
            .AddStandardResilienceHandler(config => config.Retry.ShouldHandle = args => new ValueTask<bool>(
                // We'll get a 403 if we hit the rate limit, so we'll consider that transient.
                (args.Outcome.Result?.StatusCode == System.Net.HttpStatusCode.Forbidden ||
                HttpClientResiliencePredicates.IsTransient(args.Outcome)) &&
                // We'll get a 500 error when enumerating past the available items too :/
                args.Outcome.Result?.StatusCode != System.Net.HttpStatusCode.InternalServerError));

        return services;
    }

    public static ICommandApp UseArgentina(this ICommandApp app)
    {
        app.Configure(config =>
        {
            config.AddCommand<SyncCommand>("sync");
            config.AddCommand<SyncItemCommand>("syncitem");
            config.AddCommand<EnumerateCommand>("enum").IsHidden();

            //config.AddBranch("ar", ar =>
            //{
            //    ar.AddCommand<DownloadCommand>("download");
            //    ar.AddCommand<SyncCommand>("sync");
            //    ar.AddCommand<EnumerateCommand>("enum").IsHidden();
            //});
        });

        return app;
    }
}
