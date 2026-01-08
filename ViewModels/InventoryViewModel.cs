using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PosPokemon.App.Models;
using PosPokemon.App.Repositories;

namespace PosPokemon.App.ViewModels;

public partial class InventoryViewModel : ObservableObject
{
    private readonly ProductRepository _productRepo;

    [ObservableProperty] private string _searchText = "";
    [ObservableProperty] private ObservableCollection<Product> _products = new();

    // ⬇️⬇️⬇️ AGREGADO: Evento para volver al dashboard ⬇️⬇️⬇️
    public event Action? BackToDashboardRequested;

    public InventoryViewModel(ProductRepository productRepo)
    {
        _productRepo = productRepo;

        // Cargar todos los productos al inicio
        _ = LoadAllProductsAsync();
    }

    private async Task LoadAllProductsAsync()
    {
        var allProducts = await _productRepo.SearchAsync("");

        Products.Clear();
        foreach (var p in allProducts)
        {
            Products.Add(p);
        }
    }

    [RelayCommand]
    private async Task SearchProducts()
    {
        var results = await _productRepo.SearchAsync(SearchText ?? "");

        Products.Clear();
        foreach (var p in results)
        {
            Products.Add(p);
        }
    }

    [RelayCommand]
    private void OpenCreateProduct()
    {
        var dialog = new ProductFormWindow(null);
        dialog.ProductSaved += async (product) =>
        {
            try
            {
                var id = await _productRepo.CreateAsync(product);
                product.Id = id;
                Products.Insert(0, product);

                MessageBox.Show(
                    "✅ Producto creado exitosamente!",
                    "Éxito",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information
                );
            }
            catch (System.Exception ex)
            {
                MessageBox.Show(
                    $"Error al crear producto:\n{ex.Message}",
                    "Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error
                );
            }
        };

        dialog.ShowDialog();
    }

    [RelayCommand]
    private void OpenEditProduct(Product product)
    {
        var dialog = new ProductFormWindow(product);
        dialog.ProductSaved += async (updatedProduct) =>
        {
            try
            {
                await _productRepo.UpdateAsync(updatedProduct);

                // Actualizar en la lista
                var index = Products.IndexOf(product);
                if (index >= 0)
                {
                    Products[index] = updatedProduct;
                }

                MessageBox.Show(
                    "✅ Producto actualizado exitosamente!",
                    "Éxito",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information
                );
            }
            catch (System.Exception ex)
            {
                MessageBox.Show(
                    $"Error al actualizar producto:\n{ex.Message}",
                    "Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error
                );
            }
        };

        dialog.ShowDialog();
    }

    [RelayCommand]
    private void ViewProduct(Product product)
    {
        var details = $@"📦 DETALLES DEL PRODUCTO

Código: {product.Code}
Nombre: {product.Name}
Categoría ID: {product.CategoryId}
Módulo: {product.Module ?? "N/A"}
Promo/Especial: {(product.IsPromoSpecial ? "Sí" : "No")}
Expansión ID: {product.ExpansionId?.ToString() ?? "N/A"}
Idioma: {product.Language ?? "N/A"}
Rareza: {product.Rarity ?? "N/A"}
Acabado: {product.Finish ?? "N/A"}

💰 PRECIOS
Precio: S/ {product.Price:N2}
Precio Venta: {(product.SalePrice.HasValue ? $"S/ {product.SalePrice.Value:N2}" : "N/A")}

📊 STOCK
Cantidad: {product.Stock} unidades
Stock Mínimo: {product.MinStock}

📝 DESCRIPCIÓN
{product.Description ?? "Sin descripción"}

📅 FECHAS
Creado: {product.CreatedUtc}
Actualizado: {product.UpdatedUtc}";

        MessageBox.Show(
            details,
            "Información del Producto",
            MessageBoxButton.OK,
            MessageBoxImage.Information
        );
    }

    [RelayCommand]
    private async Task DeleteProduct(Product product)
    {
        var result = MessageBox.Show(
            $"¿Estás seguro de eliminar el producto?\n\n" +
            $"Código: {product.Code}\n" +
            $"Nombre: {product.Name}\n\n" +
            $"Esta acción no se puede deshacer.",
            "Confirmar Eliminación",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning
        );

        if (result != MessageBoxResult.Yes) return;

        try
        {
            await _productRepo.DeleteAsync(product.Id);
            Products.Remove(product);

            MessageBox.Show(
                "✅ Producto eliminado exitosamente!",
                "Éxito",
                MessageBoxButton.OK,
                MessageBoxImage.Information
            );
        }
        catch (System.Exception ex)
        {
            MessageBox.Show(
                $"Error al eliminar producto:\n{ex.Message}",
                "Error",
                MessageBoxButton.OK,
                MessageBoxImage.Error
            );
        }
    }

    // ⬇️⬇️⬇️ AGREGADO: Comando para volver al dashboard ⬇️⬇️⬇️
    [RelayCommand]
    private void BackToDashboard()
    {
        BackToDashboardRequested?.Invoke();
    }
}