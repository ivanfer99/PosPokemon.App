using PosPokemon.App.Models;

namespace PosPokemon.App.Repositories;

public interface IExpansionRepository
{
    Task<List<Expansion>> GetAllActiveAsync();
    Task<Expansion?> GetByNameAsync(string name);
    Task<Expansion> CreateAsync(Expansion expansion);
}