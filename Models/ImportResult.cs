namespace PosPokemon.App.Models;

public class ImportResult
{
    public int TotalRows { get; set; }
    public int SuccessCount { get; set; }
    public int FailureCount { get; set; }
    public int SkippedCount { get; set; }
    public int NewExpansionsCreated { get; set; }
    public int NewCategoriesCreated { get; set; }
    public List<string> CreatedExpansions { get; set; } = new();
    public List<string> CreatedCategories { get; set; } = new();
    public List<ImportError> Errors { get; set; } = new();

    public bool HasErrors => Errors.Count > 0;
    public string Summary =>
        $"Total: {TotalRows} | ✓ Exitosos: {SuccessCount} | ✗ Fallidos: {FailureCount} | ⊘ Omitidos: {SkippedCount}" +
        (NewExpansionsCreated > 0 ? $" | ⭐ Expansiones nuevas: {NewExpansionsCreated}" : "") +
        (NewCategoriesCreated > 0 ? $" | ⭐ Categorías nuevas: {NewCategoriesCreated}" : "");
}

public class ImportError
{
    public int Row { get; set; }
    public string Code { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public string ErrorMessage { get; set; } = string.Empty;
    public string ErrorType { get; set; } = string.Empty; // "Validation", "Duplicate", "Database", "File"
}

public class ProductImportDto
{
    // Columnas del Excel
    public string Code { get; set; } = string.Empty;              // Columna 1
    public string Name { get; set; } = string.Empty;              // Columna 2
    public string Module { get; set; } = string.Empty;            // Columna 3
    public string Category { get; set; } = string.Empty;          // Columna 4
    public string PromoSpecial { get; set; } = string.Empty;      // Columna 5
    public string Expansion { get; set; } = string.Empty;         // Columna 6
    public string Language { get; set; } = string.Empty;          // Columna 7
    public string Rarity { get; set; } = string.Empty;            // Columna 8
    public string Finish { get; set; } = string.Empty;            // Columna 9
    public decimal Price { get; set; }                            // Columna 10
    public decimal SalePrice { get; set; }                        // Columna 11
    public int Stock { get; set; }                                // Columna 12
    public string? Description { get; set; }                      // Columna 13

    // Para tracking
    public int RowNumber { get; set; }
}