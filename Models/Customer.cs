namespace PosPokemon.App.Models;

/// <summary>
/// Cliente del sistema
/// </summary>
public sealed class Customer
{
    public long Id { get; set; }
    public string DocumentType { get; set; } = "DNI";  // DNI, RUC, CE, PASSPORT
    public string DocumentNumber { get; set; } = "";
    public string Name { get; set; } = "";
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? Address { get; set; }
    public string? Notes { get; set; }
    public long IsActive { get; set; } = 1;
    public string CreatedUtc { get; set; } = "";
    public string UpdatedUtc { get; set; } = "";

    // Para mostrar en UI
    public string DisplayText => $"{Name} - {DocumentType}: {DocumentNumber}";
    public string ShortDisplay => $"{Name} ({DocumentNumber})";
}

/// <summary>
/// Cliente con estadísticas de compras
/// </summary>
public sealed class CustomerWithStats
{
    public Customer Customer { get; set; } = new();
    public long TotalPurchases { get; set; }
    public decimal TotalSpent { get; set; }
    public string LastPurchaseDate { get; set; } = "";
}