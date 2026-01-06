using CommunityToolkit.Mvvm.ComponentModel;
using PosPokemon.App.Models;

namespace PosPokemon.App.ViewModels;

public partial class ProductFormViewModel : ObservableObject
{
    [ObservableProperty] private string _sku = "";
    [ObservableProperty] private string _name = "";
    [ObservableProperty] private string _category = "Single";
    [ObservableProperty] private string _tcg = "Pokemon";
    [ObservableProperty] private string? _setName;
    [ObservableProperty] private string? _rarity;
    [ObservableProperty] private string _language = "ES";
    [ObservableProperty] private decimal _cost;
    [ObservableProperty] private decimal _price;
    [ObservableProperty] private int _stock;

    public string Title { get; }
    public Product? ExistingProduct { get; }

    public ProductFormViewModel(Product? product)
    {
        ExistingProduct = product;
        Title = product == null ? "➕ Nuevo Producto" : "✏️ Editar Producto";

        if (product != null)
        {
            Sku = product.Sku;
            Name = product.Name;
            Category = product.Category;
            Tcg = product.Tcg;
            SetName = product.SetName;
            Rarity = product.Rarity;
            Language = product.Language ?? "ES";
            Cost = product.Cost;
            Price = product.Price;
            Stock = product.Stock;
        }
    }
}
