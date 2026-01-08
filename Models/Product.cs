namespace PosPokemon.App.Models;

public class Product
{
    public long Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public long CategoryId { get; set; }

    // ✅ CAMPOS ADICIONALES PARA CARTAS POKÉMON
    public string? Module { get; set; }
    public bool IsPromoSpecial { get; set; }
    public long? ExpansionId { get; set; }
    public string? Language { get; set; }
    public string? Rarity { get; set; }
    public string? Finish { get; set; }

    public decimal Price { get; set; }
    public decimal? SalePrice { get; set; }
    public int Stock { get; set; }
    public int MinStock { get; set; }
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;

    // ✅ CAMBIAR A STRING (como Sale, Customer, etc.)
    public string CreatedUtc { get; set; } = "";
    public string UpdatedUtc { get; set; } = "";

    // Propiedades de navegación
    public string? CategoryName { get; set; }
    public string? ExpansionName { get; set; }
}