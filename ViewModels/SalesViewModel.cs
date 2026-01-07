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
    private readonly DiscountCampaignRepository _campaignRepo;
    private readonly CustomerRepository _customerRepo;  // ✅ NUEVO
    private readonly User _currentUser;

    [ObservableProperty] private string _searchText = "";
    [ObservableProperty] private ObservableCollection<Product> _searchResults = new();
    [ObservableProperty] private ObservableCollection<CartItemViewModel> _cartItems = new();
    [ObservableProperty] private decimal _subtotal = 0;
    [ObservableProperty] private decimal _discount = 0;
    [ObservableProperty] private decimal _total = 0;

    // ✅ PROPIEDADES PARA DESCUENTOS MANUALES
    [ObservableProperty] private string _discountType = "Monto";
    [ObservableProperty] private decimal _discountValue = 0;
    [ObservableProperty] private string _discountNote = "";

    // ✅ PROPIEDADES PARA GESTIÓN DE CLIENTES
    [ObservableProperty] private string _customerSearchText = "";
    [ObservableProperty] private ObservableCollection<Customer> _customerSearchResults = new();
    [ObservableProperty] private Customer? _selectedCustomer;
    [ObservableProperty] private bool _isCustomerSearchVisible = false;

    public int CartItemCount => CartItems.Count;
    private long _lastSaleId = 0;

    // ✅ Verificar si es admin
    public bool IsAdmin => _currentUser.Role == "ADMIN";

    public event Action? BackToDashboardRequested;

    public SalesViewModel(
        ProductRepository productRepo,
        SaleRepository saleRepo,
        DiscountCampaignRepository campaignRepo,
        CustomerRepository customerRepo,  // ✅ NUEVO
        User currentUser)
    {
        _productRepo = productRepo;
        _saleRepo = saleRepo;
        _campaignRepo = campaignRepo;
        _customerRepo = customerRepo;  // ✅ NUEVO
        _currentUser = currentUser;
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
    private async Task AddToCart(Product product)
    {
        if (product.Stock <= 0)
        {
            MessageBox.Show("Producto sin stock disponible.", "Stock", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        // ✅ VERIFICAR SI EL PRODUCTO TIENE DESCUENTO AUTOMÁTICO
        var productWithDiscount = await _campaignRepo.GetProductWithDiscountAsync(product.Id);

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
            // ✅ USAR PRECIO CON DESCUENTO SI APLICA
            var priceToUse = productWithDiscount.HasActiveDiscount
                ? productWithDiscount.DiscountedPrice
                : product.Price;

            var cartItem = new CartItemViewModel
            {
                ProductId = product.Id,
                ProductName = product.Name,
                UnitPrice = priceToUse,
                OriginalPrice = product.Price,
                Quantity = 1,
                MaxStock = product.Stock,
                HasCampaignDiscount = productWithDiscount.HasActiveDiscount,
                CampaignDiscountPercentage = productWithDiscount.DiscountPercentage,
                CampaignName = productWithDiscount.CampaignName
            };

            CartItems.Add(cartItem);

            // ✅ MOSTRAR NOTIFICACIÓN SI HAY DESCUENTO
            if (productWithDiscount.HasActiveDiscount)
            {
                MessageBox.Show(
                    $"✨ ¡Producto en promoción!\n\n" +
                    $"Campaña: {productWithDiscount.CampaignName}\n" +
                    $"Descuento: {productWithDiscount.DiscountPercentage}%\n\n" +
                    $"Precio original: S/ {product.Price:N2}\n" +
                    $"Precio con descuento: S/ {productWithDiscount.DiscountedPrice:N2}",
                    "Descuento Aplicado",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information
                );
            }
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
            DiscountValue = 0;
            DiscountNote = "";
            RecalculateTotals();
        }
    }

    // ✅ APLICAR DESCUENTO MANUAL
    [RelayCommand]
    private void ApplyDiscount()
    {
        if (CartItems.Count == 0)
        {
            MessageBox.Show("Agrega productos al carrito primero.", "Descuento", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (DiscountValue < 0)
        {
            MessageBox.Show("El descuento no puede ser negativo.", "Descuento", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (DiscountValue == 0)
        {
            Discount = 0;
            RecalculateTotals();
            return;
        }

        decimal calculatedDiscount = 0;

        if (DiscountType == "Porcentaje")
        {
            if (DiscountValue > 100)
            {
                MessageBox.Show("El descuento no puede ser mayor a 100%.", "Descuento", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (DiscountValue > 20 && !IsAdmin)
            {
                MessageBox.Show(
                    "Solo los administradores pueden aplicar descuentos mayores a 20%.",
                    "Descuento Restringido",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning
                );
                return;
            }

            calculatedDiscount = Subtotal * (DiscountValue / 100);
        }
        else // Monto fijo
        {
            if (DiscountValue > Subtotal)
            {
                MessageBox.Show("El descuento no puede ser mayor al subtotal.", "Descuento", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var discountPercentage = (DiscountValue / Subtotal) * 100;
            if (discountPercentage > 20 && !IsAdmin)
            {
                MessageBox.Show(
                    $"Solo los administradores pueden aplicar descuentos mayores a 20% del subtotal.\n\n" +
                    $"Descuento actual: {discountPercentage:N1}%",
                    "Descuento Restringido",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning
                );
                return;
            }

            calculatedDiscount = DiscountValue;
        }

        Discount = calculatedDiscount;
        RecalculateTotals();

        MessageBox.Show(
            $"✅ Descuento aplicado: S/ {Discount:N2}\n" +
            (DiscountType == "Porcentaje" ? $"({DiscountValue}%)" : ""),
            "Descuento",
            MessageBoxButton.OK,
            MessageBoxImage.Information
        );
    }

    // ✅ LIMPIAR DESCUENTO MANUAL
    [RelayCommand]
    private void ClearDiscount()
    {
        DiscountValue = 0;
        DiscountNote = "";
        Discount = 0;
        RecalculateTotals();
    }

    // ✅ NUEVOS COMANDOS PARA CLIENTES

    [RelayCommand]
    private async Task SearchCustomers()
    {
        if (string.IsNullOrWhiteSpace(CustomerSearchText))
        {
            CustomerSearchResults.Clear();
            IsCustomerSearchVisible = false;
            return;
        }

        var customers = await _customerRepo.SearchAsync(CustomerSearchText);

        CustomerSearchResults.Clear();
        foreach (var customer in customers)
        {
            CustomerSearchResults.Add(customer);
        }

        IsCustomerSearchVisible = customers.Count > 0;
    }

    [RelayCommand]
    private void SelectCustomer(Customer customer)
    {
        SelectedCustomer = customer;
        CustomerSearchText = customer.DisplayText;
        IsCustomerSearchVisible = false;
    }

    [RelayCommand]
    private void ClearCustomer()
    {
        SelectedCustomer = null;
        CustomerSearchText = "";
        CustomerSearchResults.Clear();
        IsCustomerSearchVisible = false;
    }

    [RelayCommand]
    private void QuickAddCustomer()
    {
        var dialog = new Views.CustomerFormWindow(null);
        dialog.CustomerSaved += async (customer) =>
        {
            try
            {
                // Verificar si ya existe
                var existing = await _customerRepo.GetByDocumentAsync(customer.DocumentNumber);
                if (existing != null)
                {
                    MessageBox.Show(
                        $"Ya existe un cliente con el documento {customer.DocumentNumber}.",
                        "Documento Duplicado",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning
                    );
                    return;
                }

                await _customerRepo.CreateAsync(customer);

                // Seleccionar el cliente recién creado
                SelectedCustomer = customer;
                CustomerSearchText = customer.DisplayText;

                MessageBox.Show(
                    $"✅ Cliente '{customer.Name}' creado y seleccionado.",
                    "Éxito",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information
                );
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Error al crear cliente:\n{ex.Message}",
                    "Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error
                );
            }
        };

        dialog.ShowDialog();
    }

    [RelayCommand]
    private async Task ProcessSale()
    {
        if (CartItems.Count == 0)
        {
            MessageBox.Show("El carrito está vacío.", "Venta", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var paymentDialog = new Views.PaymentDialog(Total);
        var result = paymentDialog.ShowDialog();

        if (result != true)
        {
            return;
        }

        var paymentVm = paymentDialog.ViewModel;

        try
        {
            var sale = new Sale
            {
                SaleNumber = GenerateSaleNumber(),
                UserId = _currentUser.Id,
                CustomerId = SelectedCustomer?.Id,  // ✅ GUARDAR CLIENTE
                Subtotal = Subtotal,
                Discount = Discount,
                Total = Total,
                PaymentMethod = paymentVm.SelectedPaymentMethod,
                AmountReceived = paymentVm.AmountReceived,
                Change = paymentVm.Change,
                Note = !string.IsNullOrWhiteSpace(DiscountNote) ? $"Descuento: {DiscountNote}" : null
            };

            var saleItems = CartItems.Select(ci => new SaleItem
            {
                ProductId = ci.ProductId,
                Qty = ci.Quantity,
                UnitPrice = ci.UnitPrice
            }).ToList();

            await _saleRepo.CreateSaleAsync(sale, saleItems);

            _lastSaleId = sale.Id;

            var successMessage = $"✅ Venta registrada exitosamente!\n\n" +
                               $"Número: {sale.SaleNumber}\n";

            if (SelectedCustomer != null)
            {
                successMessage += $"Cliente: {SelectedCustomer.Name}\n";
            }

            successMessage += $"Subtotal: S/ {Subtotal:N2}\n";

            if (Discount > 0)
            {
                successMessage += $"Descuento: S/ {Discount:N2}\n";
            }

            successMessage += $"Total: S/ {Total:N2}\n" +
                            $"Método: {paymentVm.SelectedPaymentMethod}";

            if (paymentVm.SelectedPaymentMethod == "Efectivo")
            {
                successMessage += $"\n\n💵 Recibido: S/ {paymentVm.AmountReceived:N2}";
                if (paymentVm.Change > 0)
                {
                    successMessage += $"\n💰 Vuelto: S/ {paymentVm.Change:N2}";
                }
            }

            var printResult = MessageBox.Show(
                successMessage + "\n\n¿Desea imprimir el ticket?",
                "Venta Exitosa",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question
            );

            if (printResult == MessageBoxResult.Yes)
            {
                await PrintLastTicket();
            }

            // ✅ LIMPIAR TODO INCLUYENDO CLIENTE
            CartItems.Clear();
            SearchResults.Clear();
            SearchText = "";
            DiscountValue = 0;
            DiscountNote = "";
            ClearCustomer();
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
    private async Task PrintLastTicket()
    {
        if (_lastSaleId == 0)
        {
            MessageBox.Show(
                "No hay ninguna venta reciente para imprimir.",
                "Impresión",
                MessageBoxButton.OK,
                MessageBoxImage.Warning
            );
            return;
        }

        try
        {
            var ticketData = await _saleRepo.GetSaleTicketAsync(_lastSaleId);

            if (ticketData == null)
            {
                MessageBox.Show(
                    "No se pudo cargar la información de la venta.",
                    "Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error
                );
                return;
            }

            var ticketGenerator = new Services.TicketGenerator();
            var pdfPath = await ticketGenerator.GeneratePdfAsync(ticketData);

            ticketGenerator.OpenPdf(pdfPath);

            MessageBox.Show(
                $"Ticket generado exitosamente:\n\n{pdfPath}",
                "Impresión",
                MessageBoxButton.OK,
                MessageBoxImage.Information
            );
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"Error al generar el ticket:\n{ex.Message}",
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
    [ObservableProperty] private decimal _originalPrice;
    [ObservableProperty] private int _quantity;
    [ObservableProperty] private long _maxStock;

    // ✅ Para descuentos automáticos de campañas
    [ObservableProperty] private bool _hasCampaignDiscount = false;
    [ObservableProperty] private decimal _campaignDiscountPercentage = 0;
    [ObservableProperty] private string _campaignName = "";

    public decimal LineTotal => UnitPrice * Quantity;

    partial void OnQuantityChanged(int value)
    {
        OnPropertyChanged(nameof(LineTotal));
    }

    partial void OnUnitPriceChanged(decimal value)
    {
        OnPropertyChanged(nameof(LineTotal));
    }
}