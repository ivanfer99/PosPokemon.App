using PosPokemon.App.Models;

namespace PosPokemon.App.Repositories;

public interface IProductRepository
{
    Task<long> CreateAsync(Product p);
    Task UpdateAsync(Product p);
    Task DeleteAsync(long productId);
    Task<List<Product>> SearchAsync(string query);
    Task<Product?> GetByIdAsync(long id);
    Task<Product?> GetBySkuAsync(string sku);  // ✅ ESTE ES CRÍTICO
    Task UpdateStockAsync(long productId, int newStock);
    Task<int> GetTotalCountAsync();
    Task<int> GetLowStockCountAsync(int threshold = 5);
}