namespace PosPokemon.App.Models;

public class SaleWithDetails
{
    public long Id { get; set; }
    public string SaleNumber { get; set; } = "";
    public long UserId { get; set; }
    public decimal Subtotal { get; set; }
    public decimal Discount { get; set; }
    public decimal Total { get; set; }
    public string PaymentMethod { get; set; } = "";
    public string? Note { get; set; }
    public string CreatedUtc { get; set; } = "";
    public string UpdatedUtc { get; set; } = "";

    // Propiedades adicionales para la vista
    public string Username { get; set; } = "";
    public int TotalItems { get; set; }
}