using System;
using System.Windows;
using PosPokemon.App.Models;

namespace PosPokemon.App;

public partial class ProductFormWindow : Window
{
    private readonly Product? _existingProduct;

    public event Action<Product>? ProductSaved;

    public string Title => _existingProduct == null ? "➕ Nuevo Producto" : "✏️ Editar Producto";
    public string Sku { get; set; } = "";
    public string Name { get; set; } = "";
    public string Category { get; set; } = "Single";
    public string Tcg { get; set; } = "Pokemon";
    public string? SetName { get; set; }
    public string? Rarity { get; set; }
    public string Language { get; set; } = "ES";
    public decimal Cost { get; set; }
    public decimal Price { get; set; }
    public int Stock { get; set; }

    public ProductFormWindow(Product? product)
    {
        InitializeComponent();

        _existingProduct = product;

        // Si es edición, cargar datos existentes
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

        DataContext = this;
    }

    private void OnSaveClick(object sender, RoutedEventArgs e)
    {
        // Validaciones
        if (string.IsNullOrWhiteSpace(Sku))
        {
            MessageBox.Show("El SKU es obligatorio.", "Validación", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (string.IsNullOrWhiteSpace(Name))
        {
            MessageBox.Show("El nombre es obligatorio.", "Validación", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (Price <= 0)
        {
            MessageBox.Show("El precio debe ser mayor a 0.", "Validación", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (Stock < 0)
        {
            MessageBox.Show("El stock no puede ser negativo.", "Validación", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        // Crear o actualizar producto
        var product = _existingProduct ?? new Product();

        product.Sku = Sku.Trim();
        product.Name = Name.Trim();
        product.Category = Category;
        product.Tcg = Tcg;
        product.SetName = string.IsNullOrWhiteSpace(SetName) ? null : SetName.Trim();
        product.Rarity = string.IsNullOrWhiteSpace(Rarity) ? null : Rarity.Trim();
        product.Language = Language;
        product.Cost = Cost;
        product.Price = Price;
        product.Stock = Stock;

        // Notificar al ViewModel
        ProductSaved?.Invoke(product);

        Close();
    }

    private void OnCancelClick(object sender, RoutedEventArgs e)
    {
        Close();
    }
}