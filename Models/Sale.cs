namespace PosPokemon.App.Models;

public class Sale
{
    public long Id { get; set; }
    public string SaleNumber { get; set; } = "";
    public long UserId { get; set; }
    public long? CustomerId { get; set; }
    public decimal Subtotal { get; set; }
    public decimal Discount { get; set; }
    public decimal Total { get; set; }

    // ✅ NUEVOS CAMPOS
    public string PaymentMethod { get; set; } = "Efectivo"; // Guardamos como string en BD
    public decimal AmountReceived { get; set; } // Monto recibido del cliente
    public decimal Change { get; set; } // Vuelto

    public string? Note { get; set; }
    public string CreatedUtc { get; set; } = "";
    public string UpdatedUtc { get; set; } = "";
}