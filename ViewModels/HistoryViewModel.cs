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

public partial class HistoryViewModel : ObservableObject
{
    private readonly SaleRepository _saleRepo;

    [ObservableProperty] private string _searchText = "";
    [ObservableProperty] private DateTime? _startDate = DateTime.Today.AddDays(-30);
    [ObservableProperty] private DateTime? _endDate = DateTime.Today;
    [ObservableProperty] private ObservableCollection<SaleWithDetails> _sales = new();

    // Evento para volver al dashboard
    public event Action? BackToDashboardRequested;

    public HistoryViewModel(SaleRepository saleRepo)
    {
        _saleRepo = saleRepo;

        // Cargar ventas al inicio
        _ = LoadSalesAsync();
    }

    private async Task LoadSalesAsync()
    {
        try
        {
            var startUtc = StartDate?.ToUniversalTime().ToString("O");
            var endUtc = EndDate?.AddDays(1).ToUniversalTime().ToString("O");

            var salesList = await _saleRepo.GetSalesByDateRangeAsync(startUtc, endUtc);

            Sales.Clear();
            foreach (var sale in salesList)
            {
                Sales.Add(sale);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"Error al cargar ventas:\n{ex.Message}",
                "Error",
                MessageBoxButton.OK,
                MessageBoxImage.Error
            );
        }
    }

    [RelayCommand]
    private async Task Search()
    {
        if (!string.IsNullOrWhiteSpace(SearchText))
        {
            // Buscar por número de venta
            var sale = await _saleRepo.GetBySaleNumberAsync(SearchText.Trim());

            Sales.Clear();
            if (sale != null)
            {
                Sales.Add(sale);
            }
            else
            {
                MessageBox.Show(
                    "No se encontró ninguna venta con ese número.",
                    "Búsqueda",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information
                );
            }
        }
        else
        {
            // Buscar por rango de fechas
            await LoadSalesAsync();
        }
    }

    [RelayCommand]
    private async Task Refresh()
    {
        SearchText = "";
        await LoadSalesAsync();
    }

    [RelayCommand]
    private async Task ViewDetails(SaleWithDetails sale)
    {
        try
        {
            // Cargar items de la venta
            var items = await _saleRepo.GetSaleItemsAsync(sale.Id);

            var details = $@"🛒 DETALLE DE VENTA

📋 INFORMACIÓN GENERAL
Número: {sale.SaleNumber}
Fecha: {DateTime.Parse(sale.CreatedUtc):dd/MM/yyyy HH:mm}
Usuario: {sale.Username}

💳 MÉTODO DE PAGO
{sale.PaymentMethod}";

            // Agregar detalles de efectivo si aplica
            if (sale.PaymentMethod == "Efectivo")
            {
                details += $@"
Recibido: S/ {sale.AmountReceived:N2}
Vuelto: S/ {sale.Change:N2}";
            }

            details += $@"

📦 PRODUCTOS ({items.Count} items):
";

            foreach (var item in items)
            {
                details += $"\n• {item.ProductName}";
                details += $"\n  Cantidad: {item.Qty} x S/ {item.UnitPrice:N2} = S/ {(item.Qty * item.UnitPrice):N2}";
            }

            details += $@"

💰 RESUMEN
Subtotal: S/ {sale.Subtotal:N2}
Descuento: S/ {sale.Discount:N2}
────────────────────
TOTAL: S/ {sale.Total:N2}
";

            if (!string.IsNullOrWhiteSpace(sale.Note))
            {
                details += $"\n📝 Nota: {sale.Note}";
            }

            MessageBox.Show(
                details,
                "Detalle de Venta",
                MessageBoxButton.OK,
                MessageBoxImage.Information
            );
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"Error al cargar detalles:\n{ex.Message}",
                "Error",
                MessageBoxButton.OK,
                MessageBoxImage.Error
            );
        }
    }

    [RelayCommand]
    private void Export()
    {
        MessageBox.Show(
            "Función de exportación en desarrollo.\n\nPróximamente podrás exportar a Excel/PDF.",
            "Exportar",
            MessageBoxButton.OK,
            MessageBoxImage.Information
        );
    }

    [RelayCommand]
    private void BackToDashboard()
    {
        BackToDashboardRequested?.Invoke();
    }

    [RelayCommand]
    private async Task PrintTicket(SaleWithDetails sale)
    {
        try
        {
            var ticketData = await _saleRepo.GetSaleTicketAsync(sale.Id);

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
            var pdfPath = ticketGenerator.GeneratePdf(ticketData);

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
}