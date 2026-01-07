using System.Collections.Generic;

namespace PosPokemon.App.Models;

/// <summary>
/// Modelo completo para generar un ticket de venta
/// </summary>
public sealed class SaleTicket
{
    public string SaleNumber { get; set; } = "";
    public string Date { get; set; } = "";
    public string Time { get; set; } = "";
    public string Cashier { get; set; } = "";

    public List<SaleTicketItem> Items { get; set; } = new();

    public decimal Subtotal { get; set; }
    public decimal Discount { get; set; }
    public decimal Total { get; set; }

    public string PaymentMethod { get; set; } = "";
    public decimal AmountReceived { get; set; }
    public decimal Change { get; set; }

    public string? Note { get; set; }

    // Información de la tienda
    public string StoreName { get; set; } = "POS POKÉMON TCG";
    public string StoreAddress { get; set; } = "Lima, Perú";
    public string StorePhone { get; set; } = "";
    public string StoreRuc { get; set; } = "";
}

public sealed class SaleTicketItem
{
    public string ProductName { get; set; } = "";
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal LineTotal { get; set; }
}