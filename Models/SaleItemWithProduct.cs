namespace PosPokemon.App.Models;

public class SaleItemWithProduct
{
    public long Id { get; set; }
    public long SaleId { get; set; }
    public long ProductId { get; set; }
    public int Qty { get; set; }
    public decimal UnitPrice { get; set; }

    // Propiedad adicional para la vista
    public string ProductName { get; set; } = "";
}