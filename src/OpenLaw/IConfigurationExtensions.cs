using Microsoft.Extensions.Configuration;

namespace Clarius.OpenLaw;

public static class IConfigurationExtensions
{
    extension(IConfiguration configuration)
    {
        // TODO: remove these versions when the extension everything indexer just works: https://github.com/dotnet/roslyn/issues/78492

        public string Get(string key, bool required = true) =>
            configuration[key] is string value && !string.IsNullOrEmpty(value) ? value : required ? throw new InvalidOperationException($"Missing required configuration value '{key}'.") : null!;

        public string Get(string key, string @default) =>
            configuration[key] is var value && string.IsNullOrEmpty(value) ? @default : value;

        //public string this[string key, bool required] =>
        //    configuration[key] is string value && !string.IsNullOrEmpty(value) ? value : required ? throw new InvalidOperationException($"Missing required configuration value '{key}'.") : null!;

        //public string this[string key, string @default] =>
        //    configuration[key] is var value && string.IsNullOrEmpty(value) ? @default : value;
    }
}

//public class ConfigurationTests
//{
//    public void Check(IConfiguration configuration)
//    {
//        var required = configuration["Key", true];
//        var defaulted = configuration["Key", "Default"];

//        required = configuration.Get("Key", true);
//        defaulted = configuration.Get("Key", "Default");
//    }
//}
