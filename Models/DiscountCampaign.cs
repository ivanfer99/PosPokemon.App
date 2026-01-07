using System.Collections.Generic;

namespace PosPokemon.App.Models;

/// <summary>
/// Campaña de descuento programado
/// </summary>
public sealed class DiscountCampaign
{
    public long Id { get; set; }
    public string Name { get; set; } = "";
    public decimal DiscountPercentage { get; set; }
    public string StartDate { get; set; } = ""; // ISO 8601
    public string EndDate { get; set; } = "";   // ISO 8601
    public long IsActive { get; set; } = 1;      // ✅ long (SQLite INTEGER)
    public long CreatedBy { get; set; }
    public string CreatedUtc { get; set; } = "";
    public string UpdatedUtc { get; set; } = "";

    // Para mostrar en UI
    public string CreatorUsername { get; set; } = "";
    public long ProductCount { get; set; }       // ✅ long (COUNT en SQLite)
}

/// <summary>
/// Relación entre campaña y productos
/// </summary>
public sealed class DiscountCampaignProduct
{
    public long Id { get; set; }
    public long CampaignId { get; set; }
    public long ProductId { get; set; }
}

/// <summary>
/// Producto con descuento aplicado (para mostrar en ventas)
/// </summary>
public sealed class ProductWithDiscount
{
    public Product Product { get; set; } = new();
    public decimal OriginalPrice { get; set; }
    public decimal DiscountPercentage { get; set; }
    public decimal DiscountedPrice { get; set; }
    public string CampaignName { get; set; } = "";
    public bool HasActiveDiscount { get; set; }
}