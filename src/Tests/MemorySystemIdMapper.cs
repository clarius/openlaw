using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Clarius.OpenLaw;

public class MemorySystemIdMapper : ISystemIdMapper
{
    Dictionary<SystemId, SystemId> map = new();

    public Task<string?> FindAsync(SystemId from, string system)
    {
        if (map.TryGetValue(from, out var to) && to.System == system)
            return Task.FromResult<string?>(to.Id);

        return Task.FromResult<string?>(null);
    }

    public Task MapAsync(SystemId first, SystemId second, CancellationToken cancellation = default)
    {
        map[first] = second;
        map[second] = first;
        return Task.CompletedTask;
    }
}
