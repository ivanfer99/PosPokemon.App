namespace PosPokemon.App.Models;

public sealed class Product
{
    public long Id { get; set; }
    public string Sku { get; set; } = "";
    public string Name { get; set; } = "";
    public string Category { get; set; } = "Single"; // Single, Sealed, Accesorio, etc.
    public string Tcg { get; set; } = "Pokemon";
    public string? SetName { get; set; }
    public string? Rarity { get; set; }
    public string? Language { get; set; } = "ES";
    public decimal Cost { get; set; }
    public decimal Price { get; set; }
    public int Stock { get; set; }
    public string CreatedUtc { get; set; } = "";
    public string UpdatedUtc { get; set; } = "";
}
