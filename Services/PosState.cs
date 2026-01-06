using System.Collections.ObjectModel;
using PosPokemon.App.Models;

namespace PosPokemon.App.Services;

public sealed class PosState
{
    public ObservableCollection<Product> SearchResults { get; } = new();
}
