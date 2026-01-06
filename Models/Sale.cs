namespace PosPokemon.App.Models;

public sealed class Sale
{
    public long Id { get; set; }
    public string SaleNumber { get; set; } = "";
    public string CreatedUtc { get; set; } = "";
    public decimal Subtotal { get; set; }
    public decimal Discount { get; set; }
    public decimal Total { get; set; }
    public string PaymentMethod { get; set; } = "CASH";
    public string? Note { get; set; }
}
