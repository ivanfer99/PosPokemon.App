using CommunityToolkit.Mvvm.ComponentModel;
using PosPokemon.App.Models;

namespace PosPokemon.App.ViewModels;

public partial class ProductFormViewModel : ObservableObject
{
    // ✅ PROPIEDADES ACTUALIZADAS PARA EL NUEVO MODELO
    [ObservableProperty] private string _code = "";
    [ObservableProperty] private string _name = "";
    [ObservableProperty] private long _categoryId = 0;
    [ObservableProperty] private string? _module;
    [ObservableProperty] private bool _isPromoSpecial;
    [ObservableProperty] private long? _expansionId;
    [ObservableProperty] private string? _language;
    [ObservableProperty] private string? _rarity;
    [ObservableProperty] private string? _finish;
    [ObservableProperty] private decimal _price;
    [ObservableProperty] private decimal _salePrice;
    [ObservableProperty] private int _stock;
    [ObservableProperty] private int _minStock;
    [ObservableProperty] private string? _description;

    public string Title { get; }
    public Product? ExistingProduct { get; }

    public ProductFormViewModel(Product? product)
    {
        ExistingProduct = product;
        Title = product == null ? "➕ Nuevo Producto" : "✏️ Editar Producto";

        if (product != null)
        {
            Code = product.Code;
            Name = product.Name;
            CategoryId = product.CategoryId;
            Module = product.Module;
            IsPromoSpecial = product.IsPromoSpecial;
            ExpansionId = product.ExpansionId;
            Language = product.Language;
            Rarity = product.Rarity;
            Finish = product.Finish;
            Price = product.Price;
            SalePrice = product.SalePrice ?? 0;
            Stock = product.Stock;
            MinStock = product.MinStock;
            Description = product.Description;
        }
    }
}