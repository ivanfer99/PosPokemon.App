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

        this.Title = product == null ? "➕ Nuevo Producto" : "✏️ Editar Producto";
    }

    private void OnSaveClick(object sender, RoutedEventArgs e)
    {
        // ✅ VALIDACIONES ACTUALIZADAS
        if (string.IsNullOrWhiteSpace(_vm.Code))
        {
            MessageBox.Show("El código es obligatorio.", "Validación", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (string.IsNullOrWhiteSpace(_vm.Name))
        {
            MessageBox.Show("El nombre es obligatorio.", "Validación", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (_vm.CategoryId <= 0)
        {
            MessageBox.Show("Debes seleccionar una categoría.", "Validación", MessageBoxButton.OK, MessageBoxImage.Warning);
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

        // ✅ CREAR O ACTUALIZAR PRODUCTO (ESTRUCTURA ACTUALIZADA)
        var product = _vm.ExistingProduct ?? new Product();

        product.Code = _vm.Code.Trim();
        product.Name = _vm.Name.Trim();
        product.CategoryId = _vm.CategoryId;
        product.Module = string.IsNullOrWhiteSpace(_vm.Module) ? null : _vm.Module.Trim();
        product.IsPromoSpecial = _vm.IsPromoSpecial;
        product.ExpansionId = _vm.ExpansionId;
        product.Language = string.IsNullOrWhiteSpace(_vm.Language) ? null : _vm.Language.Trim();
        product.Rarity = string.IsNullOrWhiteSpace(_vm.Rarity) ? null : _vm.Rarity.Trim();
        product.Finish = string.IsNullOrWhiteSpace(_vm.Finish) ? null : _vm.Finish.Trim();
        product.Price = _vm.Price;
        product.SalePrice = _vm.SalePrice > 0 ? _vm.SalePrice : null;
        product.Stock = _vm.Stock;
        product.MinStock = _vm.MinStock;
        product.Description = string.IsNullOrWhiteSpace(_vm.Description) ? null : _vm.Description.Trim();
        product.IsActive = true;

        ProductSaved?.Invoke(product);
        Close();
    }

    private void OnCancelClick(object sender, RoutedEventArgs e)
    {
        Close();
    }
}