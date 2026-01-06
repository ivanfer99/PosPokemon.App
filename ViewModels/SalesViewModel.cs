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

public partial class SalesViewModel : ObservableObject
{
    private readonly ProductRepository _productRepo;
    private readonly SaleRepository _saleRepo;

    [ObservableProperty] private string _searchText = "";
    [ObservableProperty] private ObservableCollection<Product> _searchResults = new();
    [ObservableProperty] private ObservableCollection<CartItemViewModel> _cartItems = new();
    [ObservableProperty] private decimal _subtotal = 0;
    [ObservableProperty] private decimal _discount = 0;
    [ObservableProperty] private decimal _total = 0;

    public int CartItemCount => CartItems.Count;

    // Evento para volver al dashboard
    public event Action? BackToDashboardRequested;

    public SalesViewModel(ProductRepository productRepo, SaleRepository saleRepo)
    {
        _productRepo = productRepo;
        _saleRepo = saleRepo;
    }

    [RelayCommand]
    private async Task SearchProducts()
    {
        if (string.IsNullOrWhiteSpace(SearchText))
        {
            SearchResults.Clear();
            return;
        }

        var products = await _productRepo.SearchAsync(SearchText);

        SearchResults.Clear();
        foreach (var p in products)
        {
            SearchResults.Add(p);
        }
    }

    [RelayCommand]
    private void AddToCart(Product product)
    {
        if (product.Stock <= 0)
        {
            MessageBox.Show("Producto sin stock disponible.", "Stock", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var existing = CartItems.FirstOrDefault(x => x.ProductId == product.Id);
        if (existing != null)
        {
            if (existing.Quantity >= product.Stock)
            {
                MessageBox.Show($"No hay más stock disponible. Stock actual: {product.Stock}", "Stock", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            existing.Quantity++;
        }
        else
        {
            CartItems.Add(new CartItemViewModel
            {
                ProductId = product.Id,
                ProductName = product.Name,
                UnitPrice = product.Price,
                Quantity = 1,
                MaxStock = product.Stock
            });
        }

        RecalculateTotals();
    }

    [RelayCommand]
    private void RemoveFromCart(CartItemViewModel item)
    {
        CartItems.Remove(item);
        RecalculateTotals();
    }

    [RelayCommand]
    private void IncreaseQuantity(CartItemViewModel item)
    {
        if (item.Quantity >= item.MaxStock)
        {
            MessageBox.Show($"No hay más stock disponible. Stock actual: {item.MaxStock}", "Stock", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        item.Quantity++;
        RecalculateTotals();
    }

    [RelayCommand]
    private void DecreaseQuantity(CartItemViewModel item)
    {
        if (item.Quantity > 1)
        {
            item.Quantity--;
            RecalculateTotals();
        }
    }

    [RelayCommand]
    private void ClearCart()
    {
        var result = MessageBox.Show(
            "¿Estás seguro de limpiar el carrito?",
            "Limpiar Carrito",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question
        );

        if (result == MessageBoxResult.Yes)
        {
            CartItems.Clear();
            RecalculateTotals();
        }
    }

    [RelayCommand]
    private async Task ProcessSale()
    {
        if (CartItems.Count == 0)
        {
            MessageBox.Show("El carrito está vacío.", "Venta", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var result = MessageBox.Show(
            $"¿Confirmar venta por S/ {Total:N2}?",
            "Procesar Venta",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question
        );

        if (result != MessageBoxResult.Yes) return;

        try
        {
            var sale = new Sale
            {
                SaleNumber = GenerateSaleNumber(),
                Subtotal = Subtotal,
                Discount = Discount,
                Total = Total,
                PaymentMethod = "CASH",
                Note = null
            };

            var saleItems = CartItems.Select(ci => new SaleItem
            {
                ProductId = ci.ProductId,
                Qty = ci.Quantity,
                UnitPrice = ci.UnitPrice
            }).ToList();

            await _saleRepo.CreateSaleAsync(sale, saleItems);

            MessageBox.Show(
                $"✅ Venta registrada exitosamente!\n\nNúmero: {sale.SaleNumber}\nTotal: S/ {Total:N2}",
                "Venta Exitosa",
                MessageBoxButton.OK,
                MessageBoxImage.Information
            );

            // Limpiar carrito
            CartItems.Clear();
            SearchResults.Clear();
            SearchText = "";
            RecalculateTotals();
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"Error al procesar la venta:\n{ex.Message}",
                "Error",
                MessageBoxButton.OK,
                MessageBoxImage.Error
            );
        }
    }

    [RelayCommand]
    private void BackToDashboard()
    {
        BackToDashboardRequested?.Invoke();
    }

    private void RecalculateTotals()
    {
        Subtotal = CartItems.Sum(x => x.LineTotal);
        Total = Subtotal - Discount;
        OnPropertyChanged(nameof(CartItemCount));
    }

    private string GenerateSaleNumber()
    {
        var now = DateTime.UtcNow;
        return $"V{now:yyyyMMdd}-{now:HHmmss}";
    }
}

// ViewModel auxiliar para items del carrito
public partial class CartItemViewModel : ObservableObject
{
    [ObservableProperty] private long _productId;
    [ObservableProperty] private string _productName = "";
    [ObservableProperty] private decimal _unitPrice;
    [ObservableProperty] private int _quantity;
    [ObservableProperty] private int _maxStock;

    public decimal LineTotal => UnitPrice * Quantity;

    partial void OnQuantityChanged(int value)
    {
        OnPropertyChanged(nameof(LineTotal));
    }
}