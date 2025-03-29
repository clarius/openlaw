﻿using System.ComponentModel;
using System.Reflection;

namespace Clarius.OpenLaw;

class DisplayValueDescriptionAttribute<TEnum>(string description, bool parenthesize = true, bool lowerCase = false)
    : DescriptionAttribute(GetDescription(description, parenthesize, lowerCase))
    where TEnum : struct, Enum
{
    static readonly List<string> original = [.. Enum
        .GetNames<TEnum>()
        .Select(name => typeof(TEnum).GetField(name))
        .Where(field => field != null)
        .Select(field =>
            // This attribute allows multiple.
            field!.GetCustomAttributes<DisplayValueAttribute>().Select(x => x.Value).FirstOrDefault() ??
            field!.GetCustomAttribute<DescriptionAttribute>()?.Description ??
            field!.Name)];

    static readonly List<string> lowerCased = [.. original.Select(x => x.ToLowerInvariant())];

    static string GetDescription(string description, bool parenthesize, bool lowerCase) => description.Trim() + " " +
        (parenthesize ? $"({string.Join(", ", lowerCase ? lowerCased : original)})" : string.Join(", ", lowerCase ? lowerCased : original));
}
