using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using Clarius.OpenLaw;
using NuGet.Configuration;
using NuGet.Protocol.Core.Types;
using NuGet.Versioning;
using Spectre.Console;
using Spectre.Console.Cli;

// Some users reported not getting emoji on Windows, so we force UTF-8 encoding.
// This not great, but I couldn't find a better way to do it.
if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
    Console.InputEncoding = Console.OutputEncoding = Encoding.UTF8;

#if DEBUG
if (args.Contains("--debug"))
{
    Debugger.Launch();
    args = [.. args.Where(x => x != "--debug")];
}
#endif

var app = App.Create(out var services);

#if DEBUG
app.Configure(c => c.PropagateExceptions());
#else
if (args.Contains("--exceptions"))
{
    app.Configure(c => c.PropagateExceptions());
    args = args.Where(x => x != "--exceptions").ToArray();
}
#endif

if (args.Contains("-?"))
    args = [.. args.Select(x => x == "-?" ? "-h" : x)];

app.Configure(config => config.SetApplicationName(ThisAssembly.Project.ToolCommandName));

if (args.Contains("--version"))
{
    AnsiConsole.MarkupLine($"{ThisAssembly.Project.ToolCommandName} version [lime]{ThisAssembly.Project.Version}[/] ({ThisAssembly.Project.BuildDate})");
    AnsiConsole.MarkupLine($"[link]{ThisAssembly.Git.Url}/releases/tag/{ThisAssembly.Project.BuildRef}[/]");

    foreach (var message in await CheckUpdates(args))
        AnsiConsole.MarkupLine(message);

    return 0;
}

var updates = Task.Run(() => CheckUpdates(args));
var exit = app.Run(args);

if (await updates is { Length: > 0 } messages)
{
    foreach (var message in messages)
        AnsiConsole.MarkupLine(message);
}

return exit;

static async Task<string[]> CheckUpdates(string[] args)
{
    if (args.Contains("-u") || args.Contains("--unattended"))
        return [];

#if DEBUG
    return [];
#endif

    var civersion = ThisAssembly.Project.VersionPrefix.StartsWith("42.42.");

    var providers = Repository.Provider.GetCoreV3();
    var repository = new SourceRepository(new PackageSource(
        // use CI feed rather than production feed depending on which version we're using
        civersion ?
        ThisAssembly.Project.SLEET_FEED_URL :
        "https://api.nuget.org/v3/index.json"), providers);
    var resource = await repository.GetResourceAsync<PackageMetadataResource>();
    var localVersion = new NuGetVersion(ThisAssembly.Project.Version);
    // Only update to stable versions, not pre-releases
    var metadata = await resource.GetMetadataAsync(ThisAssembly.Project.PackageId, includePrerelease: false, false,
        new SourceCacheContext
        {
            NoCache = true,
            RefreshMemoryCache = true,
        },
        NuGet.Common.NullLogger.Instance, CancellationToken.None);

    var update = metadata
        .Select(x => x.Identity)
        .Where(x => x.Version > localVersion)
        .OrderByDescending(x => x.Version)
        .Select(x => x.Version)
        .FirstOrDefault();

    if (update != null)
    {
        return [
            $"Hay una nueva version de [yellow]{ThisAssembly.Project.PackageId}[/]: [dim]v{localVersion.ToNormalizedString()}[/] -> [lime]v{update.ToNormalizedString()}[/]",
            $"Actualizar con: [yellow]dotnet[/] tool update -g {ThisAssembly.Project.PackageId}" +
            (civersion ? " --source " + ThisAssembly.Project.SLEET_FEED_URL : ""),
        ];
    }

    return [];
}