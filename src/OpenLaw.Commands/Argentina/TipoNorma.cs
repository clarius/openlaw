using System.ComponentModel;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Clarius.OpenLaw.Argentina;

[TypeConverter(typeof(TipoNormaEnumConverter))]
[JsonConverter(typeof(TipoNormaConverter))]
public enum TipoNorma
{
    Ley = 1,
    [DisplayValue("Decreto")]
    [DisplayValue("DEC")]
    [DisplayValue("DNU")]
    Decreto = 2,
    [DisplayValue("Resolución")]
    [DisplayValue("REA")]
    [DisplayValue("RES")]
    Resolucion = 3,
    [DisplayValue("Disposición")]
    [DisplayValue("DIS")]
    Disposicion = 4,
    [DisplayValue("Decisión")]
    [DisplayValue("Decisión Administrativa")]
    [DisplayValue("DAN")]
    Decision = 5,
    [DisplayValue("Acordada")]
    [DisplayValue("ACO")]
    Acordada = 6,
}

public class TipoNormaEnumConverter : EnumConverter
{
    public TipoNormaEnumConverter() : base(typeof(TipoNorma)) { }

    public override object? ConvertFrom(ITypeDescriptorContext? context, CultureInfo? culture, object value)
    {
        if (value is string raw)
        {
            if (DisplayValue.TryParse<TipoNorma>(raw, true, out var result))
                return result;
            if (raw.StartsWith("RES", StringComparison.Ordinal))
                return TipoNorma.Resolucion;
            throw new FormatException($"Unable to convert \"{value}\" to {nameof(TipoNorma)}.");
        }
        return base.ConvertFrom(context, culture, value);
    }
}

public class TipoNormaConverter : JsonConverter<TipoNorma>
{
    public override TipoNorma Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var raw = reader.GetString() ?? "";
        if (DisplayValue.TryParse<TipoNorma>(raw, true, out var value))
            return value;

        if (raw.StartsWith("RES", StringComparison.Ordinal))
            return TipoNorma.Resolucion;

        throw new JsonException($"Unable to convert \"{value}\" to {nameof(TipoNorma)}.");
    }

    public override void Write(Utf8JsonWriter writer, TipoNorma value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.ToString());
    }
}