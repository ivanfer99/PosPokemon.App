using PosPokemon.App.Models;

namespace PosPokemon.App.Repositories;

public interface ICategoryRepository
{
    Task<List<Category>> GetAllAsync();
    Task<List<Category>> GetAllActiveAsync();
    Task<Category?> GetByIdAsync(long id);
    Task<Category?> GetByNameAsync(string name);  // ✅ ESTE ES CRÍTICO
    Task<Category> CreateAsync(Category category);
    Task<bool> UpdateAsync(Category category);
    Task<bool> DeleteAsync(long id);
    Task<bool> ActivateAsync(long id);
}