using System;
using System.Windows;
using PosPokemon.App.Models;
using PosPokemon.App.ViewModels;

namespace PosPokemon.App;

public partial class ProductFormWindow : Window
{
    private readonly ProductFormViewModel _vm;

    public event Action<Product>? ProductSaved;

    public ProductFormWindow(Product? product)
    {
        InitializeComponent();

        _vm = new ProductFormViewModel(product);
        DataContext = _vm;

        // Si tu XAML usa {Binding Title} para mostrar título dentro del formulario,
        // mantenemos el Title del Window también:
        this.Title = product == null ? "➕ Nuevo Producto" : "✏️ Editar Producto";
    }

    private void OnSaveClick(object sender, RoutedEventArgs e)
    {
        // Validaciones (ahora validamos el VM)
        if (string.IsNullOrWhiteSpace(_vm.Sku))
        {
            MessageBox.Show("El SKU es obligatorio.", "Validación", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (string.IsNullOrWhiteSpace(_vm.Name))
        {
            MessageBox.Show("El nombre es obligatorio.", "Validación", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (_vm.Price <= 0)
        {
            MessageBox.Show("El precio debe ser mayor a 0.", "Validación", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (_vm.Stock < 0)
        {
            MessageBox.Show("El stock no puede ser negativo.", "Validación", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        // Crear o actualizar producto
        var product = _vm.ExistingProduct ?? new Product();

        product.Sku = _vm.Sku.Trim();
        product.Name = _vm.Name.Trim();
        product.Category = _vm.Category;
        product.Tcg = _vm.Tcg;

        product.SetName = string.IsNullOrWhiteSpace(_vm.SetName) ? null : _vm.SetName.Trim();
        product.Rarity = string.IsNullOrWhiteSpace(_vm.Rarity) ? null : _vm.Rarity.Trim();

        product.Language = _vm.Language;
        product.Cost = _vm.Cost;
        product.Price = _vm.Price;
        product.Stock = _vm.Stock;

        ProductSaved?.Invoke(product);
        Close();
    }

    private void OnCancelClick(object sender, RoutedEventArgs e)
    {
        Close();
    }
}
