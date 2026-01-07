using System.Collections.Generic;

namespace PosPokemon.App.Models;

public sealed class SaleWithDetails
{
    public long Id { get; set; }
    public string SaleNumber { get; set; } = "";
    public long UserId { get; set; }
    public long? CustomerId { get; set; }              // ✅ NUEVO: ID del cliente (nullable)
    public string? CustomerName { get; set; }          // ✅ NUEVO: Nombre del cliente (para mostrar)
    public decimal Subtotal { get; set; }
    public decimal Discount { get; set; }
    public decimal Total { get; set; }
    public string PaymentMethod { get; set; } = "";
    public decimal AmountReceived { get; set; }
    public decimal Change { get; set; }
    public string? Note { get; set; }
    public string CreatedUtc { get; set; } = "";
    public string UpdatedUtc { get; set; } = "";

    // Propiedades adicionales para la vista
    public string Username { get; set; } = "";         // Nombre del vendedor
    public string SellerUsername { get; set; } = "";   // ✅ NUEVO: Alias para compatibilidad
    public int TotalItems { get; set; }
    public List<SaleItemDetail> Items { get; set; } = new();  // ✅ NUEVO: Detalles de productos
}

// ✅ NUEVO: Clase para items detallados de la venta
public sealed class SaleItemDetail
{
    public long Id { get; set; }
    public long ProductId { get; set; }
    public string ProductName { get; set; } = "";
    public int Qty { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal LineTotal => UnitPrice * Qty;
}