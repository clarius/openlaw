using System.Diagnostics;
using System.Text.Json;
using Spectre.Console;
using Spectre.Console.Json;

namespace Clarius.OpenLaw;

public static class JsonSerializerExtensions
{
    /// <summary>
    /// Deserializes the specified JSON string to an object of type <typeparamref name="T"/>.
    /// </summary>
    /// <typeparam name="T">The type of object to deserialize.</typeparam>
    /// <param name="options">The serializer options to use.</param>
    /// <param name="json">The JSON payload.</param>
    /// <returns>The deserialized object or <see langword="null"/>if input is empty or null.</returns>
    /// <exception cref="FormatException">The input JSON cannot be formatted into the target type.</exception>
    /// <exception cref="JsonException">JSON deserialization failed.</exception>
    public static T? Deserialize<T>(this JsonSerializerOptions options, string json)
    {
        if (string.IsNullOrEmpty(json))
            return default;

        try
        {
            return JsonSerializer.Deserialize<T>(json, options);
        }
        catch (FormatException f)
        {
#if DEBUG
            AnsiConsole.WriteException(f);
            AnsiConsole.Write(new JsonText(json));
#endif
            Debugger.Launch();
            Debugger.Log(0, "", f.Message);
            throw;
        }
        catch (JsonException e)
        {
#if DEBUG
            AnsiConsole.WriteException(e);
            AnsiConsole.Write(new JsonText(json));
#endif
            Debugger.Launch();
            Debugger.Log(0, "", e.Message);
            throw;
        }
    }

    /// <summary>
    /// Tries to deserialize the specified JSON string to an object of type <typeparamref name="T"/>. 
    /// Returns <see langword="null"/> if conversion fails, rather than throwing.
    /// </summary>
    /// <typeparam name="T">The type of object to deserialize.</typeparam>
    /// <param name="options">The serializer options to use.</param>
    /// <param name="json">The JSON payload.</param>
    /// <returns>The deserialized object or <see langword="null"/>if deserialization failed for whatever reason.</returns>
    public static T? TryDeserialize<T>(this JsonSerializerOptions options, string json)
    {
        if (string.IsNullOrEmpty(json))
            return default;

        try
        {
            return JsonSerializer.Deserialize<T>(json, options);
        }
        catch (FormatException f)
        {
#if DEBUG
            AnsiConsole.WriteException(f);
            AnsiConsole.Write(new JsonText(json));
#endif
            Debugger.Launch();
            Debugger.Log(0, "", f.Message);
            return default;
        }
        catch (JsonException e)
        {
#if DEBUG
            AnsiConsole.WriteException(e);
            AnsiConsole.Write(new JsonText(json));
#endif
            Debugger.Launch();
            Debugger.Log(0, "", e.Message);
            return default;
        }
    }
}
