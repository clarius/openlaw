
namespace Clarius.OpenLaw
{
    public interface IVectorStoreService
    {
        Task<VectorStore?> GetStoreAsync(DateOnly documentDate, CancellationToken cancellation = default);
        Task<IEnumerable<VectorStore>> GetStores(CancellationToken cancellation = default);
    }
}