using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;

namespace Clarius.OpenLaw.Argentina;

public static class JsonOptions
{
    public static JsonSerializerOptions Default { get; } = new(JsonSerializerDefaults.Web)
    {
        TypeInfoResolver = new DefaultJsonTypeInfoResolver(),
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        Converters =
        {
            new TipoNormaConverter(),
            new JsonStringEnumConverter(JsonNamingPolicy.CamelCase),
            new JsonDictionaryConverter(),
        },
        WriteIndented = true,
        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    };

    /// <summary>
    /// A simple JSON serializer options with indentation enabled, extending the <see cref="JsonSerializerDefaults.Web"/>.
    /// </summary>
    public static JsonSerializerOptions Indented { get; } = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true
    };
}
