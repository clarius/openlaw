using Azure.Data.Tables;
using Devlooped;
using Microsoft.Extensions.DependencyInjection;

namespace Clarius.OpenLaw;

/// <summary>
/// Represents an identifier within a given system.
/// </summary>
public record SystemId(string System, string Id)
{
    public static implicit operator SystemId((string System, string Id) tuple) => new(tuple.System, tuple.Id);
    public static implicit operator (string System, string Id)(SystemId mapping) => (mapping.System, mapping.Id);
}

public interface ISystemIdMapper
{
    Task<string?> FindAsync(SystemId from, string system);
    Task MapAsync(SystemId first, SystemId second, CancellationToken cancellation = default);
}

/// <summary>
/// Maps IDs across different systems.
/// </summary>
[Service]
public class SystemIdMapper : ISystemIdMapper
{
    readonly ITableRepository<TableEntity> repo;

    public SystemIdMapper(CloudStorageAccount storage)
        : this(TableRepository.Create(storage, "SystemId"))
    {
    }

    internal SystemIdMapper(ITableRepository<TableEntity> repo)
    {
        this.repo = repo ?? throw new ArgumentNullException(nameof(repo));
    }

    /// <summary>
    /// Creates a bidirectional mapping between the two identifiers.
    /// </summary>
    /// <param name="first">First System-scoped identifier.</param>
    /// <param name="second">Second System-scoped identifier.</param>
    /// <param name="cancellation">Optional cancellation token.</param>
    public async Task MapAsync(SystemId first, SystemId second, CancellationToken cancellation = default)
    {
        await repo.PutAsync(new TableEntity($"{first.System}-{first.Id}", $"{second.System}-{second.Id}"), cancellation);
        await repo.PutAsync(new TableEntity($"{second.System}-{second.Id}", $"{first.System}-{first.Id}"), cancellation);
    }

    /// <summary>
    /// Tries to find the identifier in the target <paramref name="system"/> that matches the given 
    /// source <paramref name="from"/> identifier and system.
    /// </summary>
    /// <param name="from">The system identifier to map from.</param>
    /// <param name="system">The target system to find the map for.</param>
    /// <returns>The mapped identifier or <see langword="null"/> if no mapping exists.</returns>
    public async Task<string?> FindAsync(SystemId from, string system)
    {
        var key = $"{from.System}-{from.Id}";
        var query = from item in repo.CreateQuery()
                    where item.PartitionKey == key && item.RowKey.CompareTo(system) >= 0
                    select item;

        try
        {
            query = query.Take(1);
            if (query is IAsyncEnumerable<TableEntity>)
            {
                await foreach (var item in query)
                    return item.RowKey[(system.Length + 1)..];
            }
            else
            {
                foreach (var item in query)
                    return item.RowKey[(system.Length + 1)..];
            }
        }
        catch (HttpRequestException re) when (re.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            // no value found in this case.
        }

        return default;
    }
}
