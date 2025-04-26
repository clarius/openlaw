using System.ComponentModel;
using System.Globalization;

namespace Clarius.OpenLaw;

class DisplayValueConverter<TEnum> : EnumConverter where TEnum : struct, Enum
{
    public DisplayValueConverter() : base(typeof(TEnum)) { }

    public override bool CanConvertFrom(ITypeDescriptorContext? context, Type sourceType)
        => sourceType == typeof(string) || base.CanConvertFrom(context, sourceType);

    public override object? ConvertFrom(ITypeDescriptorContext? context, CultureInfo? culture, object value)
        => value.ToString() is string display && DisplayValue.TryParse<TEnum>(display, true, out var result) ? result
        : base.ConvertFrom(context, culture, value);
}
