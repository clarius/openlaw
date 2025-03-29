using System.ComponentModel;
using System.Reflection;

namespace Clarius.OpenLaw;

class DisplayValueDescriptionAttribute<TEnum>(string description, bool parenthesize = true)
    : DescriptionAttribute(GetDescription(description, parenthesize))
    where TEnum : struct, Enum
{
    static readonly List<string> names = [.. Enum
        .GetNames<TEnum>()
        .Select(name => typeof(TEnum).GetField(name))
        .Where(field => field != null)
        .Select(field =>
            // This attribute allows multiple.
            field!.GetCustomAttributes<DisplayValueAttribute>().Select(x => x.Value).FirstOrDefault() ??
            field!.GetCustomAttribute<DescriptionAttribute>()?.Description ??
            field!.Name.ToLowerInvariant())];

    static string GetDescription(string description, bool parenthesize) => description.Trim() + " " +
        (parenthesize ? $"({string.Join(", ", names)})" : string.Join(", ", names));
}
