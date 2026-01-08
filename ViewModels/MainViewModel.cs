using System.Threading.Tasks;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PosPokemon.App.Repositories;
using PosPokemon.App.Services;

namespace PosPokemon.App.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private readonly ProductRepository _products;
    private readonly PosState _state;

    [ObservableProperty] private string _searchText = "";

    public PosState State => _state;

    public MainViewModel(ProductRepository products, PosState state)
    {
        _products = products;
        _state = state;
    }

    [RelayCommand]
    private async Task SearchAsync()
    {
        _state.SearchResults.Clear();

        var q = SearchText?.Trim() ?? "";
        if (q.Length == 0) return;

        var list = await _products.SearchAsync(q);
        foreach (var p in list) _state.SearchResults.Add(p);
    }

    [RelayCommand]
    private async Task SeedExampleAsync()
    {
        // Crea un producto demo si no existe
        var code = "PKM-001";
        var existing = await _products.GetBySkuAsync(code);
        if (existing != null)
        {
            MessageBox.Show("Ya existe PKM-001");
            return;
        }

        await _products.CreateAsync(new Models.Product
        {
            Code = code,
            Name = "Pikachu - Illustration Rare",
            CategoryId = 1, // ⚠️ Asegúrate de que esta categoría exista
            Module = "Base",
            IsPromoSpecial = false,
            ExpansionId = null,
            Language = "Español",
            Rarity = "Illustration Rare",
            Finish = "Holo",
            Price = 15.00m,
            SalePrice = null,
            Stock = 3,
            MinStock = 1,
            Description = "Carta promocional de Pikachu con arte especial",
            IsActive = true
        });

        MessageBox.Show("Producto demo creado.");
    }
}
