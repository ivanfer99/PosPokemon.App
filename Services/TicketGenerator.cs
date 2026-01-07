using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using PosPokemon.App.Data;
using PosPokemon.App.Models;
using PosPokemon.App.Repositories;

namespace PosPokemon.App.Services;

public sealed class TicketGenerator
{
    private readonly string _outputFolder;

    public TicketGenerator()
    {
        // Configurar licencia de QuestPDF (Community - gratis)
        QuestPDF.Settings.License = LicenseType.Community;

        // Carpeta donde se guardan los tickets
        _outputFolder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
            "PosPokemon",
            "Tickets"
        );

        // Crear carpeta si no existe
        Directory.CreateDirectory(_outputFolder);
    }

    /// <summary>
    /// Genera un ticket en formato PDF con configuración de tienda (VERSIÓN ASYNC)
    /// </summary>
    public async Task<string> GeneratePdfAsync(SaleTicket ticket)
    {
        // ✅ CARGAR CONFIGURACIÓN DE LA TIENDA (construye dbFile internamente)
        var dbFile = GetDefaultDbPath();
        var storeSettings = await LoadStoreSettingsAsync(dbFile);

        // Aplicar configuración al ticket
        ticket.StoreName = storeSettings.Name;
        ticket.StoreAddress = storeSettings.Address;
        ticket.StorePhone = storeSettings.Phone;
        ticket.StoreRuc = storeSettings.Ruc;

        var fileName = $"Ticket_{ticket.SaleNumber.Replace("/", "-").Replace(":", "")}.pdf";
        var fullPath = Path.Combine(_outputFolder, fileName);

        Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(80, 297, Unit.Millimetre);
                page.Margin(5, Unit.Millimetre);

                page.Content().Column(column =>
                {
                    column.Spacing(3);

                    // ✅ LOGO (si existe)
                    if (storeSettings.HasValidLogo())
                    {
                        BuildLogo(column, storeSettings.LogoPath);
                    }

                    // ENCABEZADO
                    BuildHeader(column, ticket);

                    // LÍNEA SEPARADORA
                    column.Item().PaddingVertical(2).LineHorizontal(1).LineColor(Colors.Grey.Lighten2);

                    // INFORMACIÓN DE VENTA
                    BuildSaleInfo(column, ticket);

                    // LÍNEA SEPARADORA
                    column.Item().PaddingVertical(2).LineHorizontal(1).LineColor(Colors.Grey.Lighten2);

                    // PRODUCTOS
                    BuildItemsTable(column, ticket);

                    // LÍNEA SEPARADORA
                    column.Item().PaddingVertical(2).LineHorizontal(1).LineColor(Colors.Grey.Lighten2);

                    // TOTALES
                    BuildTotals(column, ticket);

                    // LÍNEA SEPARADORA
                    column.Item().PaddingVertical(2).LineHorizontal(1).LineColor(Colors.Grey.Lighten2);

                    // INFORMACIÓN DE PAGO
                    BuildPaymentInfo(column, ticket);

                    // NOTA (si existe)
                    if (!string.IsNullOrWhiteSpace(ticket.Note))
                    {
                        column.Item().PaddingVertical(2).LineHorizontal(1).LineColor(Colors.Grey.Lighten2);
                        BuildNote(column, ticket);
                    }

                    // PIE DE PÁGINA
                    column.Item().PaddingTop(10).Text("¡Gracias por su compra!")
                        .FontSize(9)
                        .Bold()
                        .AlignCenter();

                    column.Item().Text("www.cartonfinoperu.com")
                        .FontSize(7)
                        .AlignCenter();
                });
            });
        }).GeneratePdf(fullPath);

        return fullPath;
    }

    /// <summary>
    /// Versión síncrona para compatibilidad (DEPRECATED - usar GeneratePdfAsync)
    /// </summary>
    public string GeneratePdf(SaleTicket ticket)
    {
        return GeneratePdfAsync(ticket).Result;
    }

    private string GetDefaultDbPath()
    {
        return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "pospokemon.sqlite");
    }

    private async Task<StoreSettings> LoadStoreSettingsAsync(string dbFile)
    {
        try
        {
            var db = new Db(dbFile);
            var repo = new SettingsRepository(db);

            return new StoreSettings
            {
                Name = await repo.GetAsync("store.name") ?? "POS POKÉMON TCG",
                Address = await repo.GetAsync("store.address") ?? "Lima, Perú",
                Phone = await repo.GetAsync("store.phone") ?? "",
                Ruc = await repo.GetAsync("store.ruc") ?? "",
                LogoPath = await repo.GetAsync("store.logo_path") ?? ""
            };
        }
        catch
        {
            // Si falla, devolver valores por defecto
            return new StoreSettings
            {
                Name = "POS POKÉMON TCG",
                Address = "Lima, Perú",
                Phone = "",
                Ruc = "",
                LogoPath = ""
            };
        }
    }

    // ✅ AGREGAR LOGO
    private void BuildLogo(ColumnDescriptor column, string logoPath)
    {
        try
        {
            column.Item()
                .AlignCenter()
                .Height(40, Unit.Millimetre)
                .Image(logoPath);

            column.Item().PaddingBottom(3);
        }
        catch
        {
            // Si falla la carga del logo, continuar sin él
        }
    }

    private void BuildHeader(ColumnDescriptor column, SaleTicket ticket)
    {
        column.Item().Text(ticket.StoreName)
            .FontSize(14)
            .Bold()
            .AlignCenter();

        if (!string.IsNullOrWhiteSpace(ticket.StoreAddress))
        {
            column.Item().Text(ticket.StoreAddress)
                .FontSize(8)
                .AlignCenter();
        }

        if (!string.IsNullOrWhiteSpace(ticket.StorePhone))
        {
            column.Item().Text($"Tel: {ticket.StorePhone}")
                .FontSize(8)
                .AlignCenter();
        }

        if (!string.IsNullOrWhiteSpace(ticket.StoreRuc))
        {
            column.Item().Text($"RUC: {ticket.StoreRuc}")
                .FontSize(8)
                .AlignCenter();
        }
    }

    private void BuildSaleInfo(ColumnDescriptor column, SaleTicket ticket)
    {
        column.Item().Row(row =>
        {
            row.RelativeItem().Text($"Ticket: {ticket.SaleNumber}").FontSize(8);
        });

        column.Item().Row(row =>
        {
            row.RelativeItem().Text($"Fecha: {ticket.Date}").FontSize(8);
            row.RelativeItem().Text($"Hora: {ticket.Time}").FontSize(8).AlignRight();
        });

        column.Item().Text($"Cajero: {ticket.Cashier}").FontSize(8);
    }

    private void BuildItemsTable(ColumnDescriptor column, SaleTicket ticket)
    {
        // Encabezados
        column.Item().Row(row =>
        {
            row.RelativeItem(4).Text("Producto").FontSize(8).Bold();
            row.RelativeItem(1).Text("Cant").FontSize(8).Bold().AlignCenter();
            row.RelativeItem(2).Text("P.Unit").FontSize(8).Bold().AlignRight();
            row.RelativeItem(2).Text("Total").FontSize(8).Bold().AlignRight();
        });

        column.Item().PaddingVertical(1).LineHorizontal(0.5f).LineColor(Colors.Grey.Lighten3);

        // Items
        foreach (var item in ticket.Items)
        {
            column.Item().Row(row =>
            {
                row.RelativeItem(4).Text(item.ProductName).FontSize(8);
                row.RelativeItem(1).Text(item.Quantity.ToString()).FontSize(8).AlignCenter();
                row.RelativeItem(2).Text($"S/ {item.UnitPrice:N2}").FontSize(8).AlignRight();
                row.RelativeItem(2).Text($"S/ {item.LineTotal:N2}").FontSize(8).AlignRight();
            });
        }
    }

    private void BuildTotals(ColumnDescriptor column, SaleTicket ticket)
    {
        column.Item().Row(row =>
        {
            row.RelativeItem().Text("Subtotal:").FontSize(9);
            row.RelativeItem().Text($"S/ {ticket.Subtotal:N2}").FontSize(9).AlignRight();
        });

        if (ticket.Discount > 0)
        {
            column.Item().Row(row =>
            {
                row.RelativeItem().Text("Descuento:").FontSize(9);
                row.RelativeItem().Text($"-S/ {ticket.Discount:N2}").FontSize(9).AlignRight();
            });
        }

        column.Item().PaddingTop(3).Row(row =>
        {
            row.RelativeItem().Text("TOTAL:").FontSize(11).Bold();
            row.RelativeItem().Text($"S/ {ticket.Total:N2}").FontSize(11).Bold().AlignRight();
        });
    }

    private void BuildPaymentInfo(ColumnDescriptor column, SaleTicket ticket)
    {
        column.Item().Text($"Método de Pago: {ticket.PaymentMethod}")
            .FontSize(9)
            .Bold();

        if (ticket.PaymentMethod == "Efectivo")
        {
            column.Item().Row(row =>
            {
                row.RelativeItem().Text("Recibido:").FontSize(9);
                row.RelativeItem().Text($"S/ {ticket.AmountReceived:N2}").FontSize(9).AlignRight();
            });

            if (ticket.Change > 0)
            {
                column.Item().Row(row =>
                {
                    row.RelativeItem().Text("Vuelto:").FontSize(10).Bold();
                    row.RelativeItem().Text($"S/ {ticket.Change:N2}").FontSize(10).Bold().AlignRight();
                });
            }
        }
    }

    private void BuildNote(ColumnDescriptor column, SaleTicket ticket)
    {
        column.Item().Text("Nota:").FontSize(8).Bold();
        column.Item().Text(ticket.Note!).FontSize(8).Italic();
    }

    /// <summary>
    /// Abre el PDF generado con el visor predeterminado del sistema
    /// </summary>
    public void OpenPdf(string filePath)
    {
        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = filePath,
                UseShellExecute = true
            });
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"No se pudo abrir el PDF: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Obtiene el directorio de salida de tickets
    /// </summary>
    public string GetOutputDirectory()
    {
        return _outputFolder;
    }
}