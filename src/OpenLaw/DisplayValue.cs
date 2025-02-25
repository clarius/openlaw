namespace Clarius.OpenLaw;

public static class DisplayValue
{
    public static string ToString<T>(this T value) where T : struct, Enum
        => DisplayValue<T>.GetString(value);

    public static T Parse<T>(string value, bool ignoreCase = false) where T : struct, Enum
        => DisplayValue<T>.Parse(value, ignoreCase);

    public static bool TryParse<T>(string value, out T result, bool ignoreCase = false) where T : struct, Enum
        => DisplayValue<T>.TryParse(value, out result, ignoreCase);
}
