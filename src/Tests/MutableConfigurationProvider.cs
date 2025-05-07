using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;

namespace Clarius.OpenLaw;

public class InMemoryConfigurationProvider : ConfigurationProvider
{
    readonly Dictionary<string, string?> _data;

    public InMemoryConfigurationProvider(Dictionary<string, string?> initialData)
    {
        _data = new Dictionary<string, string?>(initialData, StringComparer.OrdinalIgnoreCase);
    }

    public override void Set(string key, string? value)
    {
        _data[key] = value;
        OnReload();
    }

    public override void Load() { }

    public override bool TryGet(string key, out string? value)
    {
        return _data.TryGetValue(key, out value);
    }
}

public class InMemoryConfigurationSource : IConfigurationSource
{
    readonly Dictionary<string, string?> _initialData;

    public InMemoryConfigurationSource(Dictionary<string, string?> initialData)
    {
        _initialData = initialData;
    }

    public IConfigurationProvider Build(IConfigurationBuilder builder)
    {
        return new InMemoryConfigurationProvider(_initialData);
    }
}