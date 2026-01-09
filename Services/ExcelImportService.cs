using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ClosedXML.Excel;
using PosPokemon.App.Models;
using PosPokemon.App.Repositories;

namespace PosPokemon.App.Services;

public class ExcelImportService
{
    private readonly IProductRepository _productRepo;
    private readonly ICategoryRepository _categoryRepo;
    private readonly IExpansionRepository _expansionRepo;

    public ExcelImportService(
        IProductRepository productRepo,
        ICategoryRepository categoryRepo,
        IExpansionRepository expansionRepo)
    {
        _productRepo = productRepo;
        _categoryRepo = categoryRepo;
        _expansionRepo = expansionRepo;
    }

    /// <summary>
    /// Importa productos desde un archivo Excel
    /// </summary>
    public async Task<ImportResult> ImportProductsFromExcelAsync(string filePath)
    {
        var result = new ImportResult();

        try
        {
            // Leer archivo Excel
            var importData = ReadExcelFile(filePath);
            result.TotalRows = importData.Count;

            // Cargar categorías y expansiones existentes
            var existingCategories = (await _categoryRepo.GetAllAsync())
                .ToDictionary(c => c.Name.ToLower(), c => c);

            var existingExpansions = (await _expansionRepo.GetAllActiveAsync())
                .ToDictionary(e => e.Name.ToLower(), e => e);

            // Procesar cada fila
            foreach (var dto in importData)
            {
                try
                {
                    // Validar producto
                    var validationError = ValidateProduct(dto);
                    if (validationError != null)
                    {
                        result.Errors.Add(validationError);
                        result.FailureCount++;
                        continue;
                    }

                    // Verificar si ya existe por código
                    var existing = await _productRepo.GetBySkuAsync(dto.Code);
                    if (existing != null)
                    {
                        result.Errors.Add(new ImportError
                        {
                            Row = dto.RowNumber,
                            Code = dto.Code,
                            ProductName = dto.Name,
                            ErrorMessage = "Producto duplicado (código ya existe)",
                            ErrorType = "Duplicate"
                        });
                        result.SkippedCount++;
                        continue;
                    }

                    // ✅ AUTO-CREAR CATEGORÍA si no existe
                    var categoryKey = dto.Category.ToLower();
                    if (!existingCategories.ContainsKey(categoryKey))
                    {
                        var newCategory = new Category
                        {
                            Name = dto.Category,
                            IsActive = true
                        };
                        var created = await _categoryRepo.CreateAsync(newCategory);
                        existingCategories[categoryKey] = created;
                        result.NewCategoriesCreated++;
                        result.CreatedCategories.Add(dto.Category);
                    }

                    var categoryId = existingCategories[categoryKey].Id;

                    // ✅ AUTO-CREAR EXPANSIÓN si no existe y está especificada
                    long? expansionId = null;
                    if (!string.IsNullOrWhiteSpace(dto.Expansion))
                    {
                        var expansionKey = dto.Expansion.ToLower();
                        if (!existingExpansions.ContainsKey(expansionKey))
                        {
                            var newExpansion = new Expansion
                            {
                                Name = dto.Expansion,
                                IsActive = true
                            };
                            var created = await _expansionRepo.CreateAsync(newExpansion);
                            existingExpansions[expansionKey] = created;
                            result.NewExpansionsCreated++;
                            result.CreatedExpansions.Add(dto.Expansion);
                        }

                        expansionId = existingExpansions[expansionKey].Id;
                    }

                    // Parsear campo Promo-Especial (acepta: sí, si, yes, true, 1)
                    var isPromoSpecial = false;
                    if (!string.IsNullOrWhiteSpace(dto.PromoSpecial))
                    {
                        var promo = dto.PromoSpecial.Trim().ToLower();
                        isPromoSpecial = promo == "sí" || promo == "si" || promo == "yes" ||
                                        promo == "true" || promo == "1";
                    }

                    // Crear producto
                    var product = new Product
                    {
                        Code = dto.Code.Trim(),
                        Name = dto.Name.Trim(),
                        CategoryId = categoryId,
                        Module = string.IsNullOrWhiteSpace(dto.Module) ? null : dto.Module.Trim(),
                        IsPromoSpecial = isPromoSpecial,
                        ExpansionId = expansionId,
                        Language = string.IsNullOrWhiteSpace(dto.Language) ? null : dto.Language.Trim(),
                        Rarity = string.IsNullOrWhiteSpace(dto.Rarity) ? null : dto.Rarity.Trim(),
                        Finish = string.IsNullOrWhiteSpace(dto.Finish) ? null : dto.Finish.Trim(),
                        Price = dto.Price,
                        SalePrice = dto.SalePrice > 0 ? dto.SalePrice : null,
                        Stock = dto.Stock,
                        Description = string.IsNullOrWhiteSpace(dto.Description) ? null : dto.Description.Trim(),
                        IsActive = true
                    };

                    await _productRepo.CreateAsync(product);
                    result.SuccessCount++;
                }
                catch (Exception ex)
                {
                    result.Errors.Add(new ImportError
                    {
                        Row = dto.RowNumber,
                        Code = dto.Code,
                        ProductName = dto.Name,
                        ErrorMessage = $"Error al crear producto: {ex.Message}",
                        ErrorType = "Database"
                    });
                    result.FailureCount++;
                }
            }
        }
        catch (Exception ex)
        {
            result.Errors.Add(new ImportError
            {
                Row = 0,
                Code = "",
                ProductName = "",
                ErrorMessage = $"Error al leer archivo: {ex.Message}",
                ErrorType = "File"
            });
            result.FailureCount = result.TotalRows;
        }

        return result;
    }

    /// <summary>
    /// Lee el archivo Excel y extrae los datos
    /// </summary>
    private List<ProductImportDto> ReadExcelFile(string filePath)
    {
        var products = new List<ProductImportDto>();

        using var workbook = new XLWorkbook(filePath);
        var worksheet = workbook.Worksheet(1);
        var rows = worksheet.RowsUsed().Skip(1); // Saltar encabezado

        int rowNumber = 2; // Empezar desde fila 2 (fila 1 es encabezado)

        foreach (var row in rows)
        {
            try
            {
                // Verificar si la fila está completamente vacía
                if (row.IsEmpty())
                    continue;

                var dto = new ProductImportDto
                {
                    RowNumber = rowNumber,

                    // ✅ CORREGIDO: Leer por ÍNDICE de columna (A=1, B=2, C=3...)
                    Code = GetCellValue(row, 1),              // Columna A
                    Name = GetCellValue(row, 2),              // Columna B
                    Module = GetCellValue(row, 3),            // Columna C
                    Category = GetCellValue(row, 4),          // Columna D
                    PromoSpecial = GetCellValue(row, 5),      // Columna E
                    Expansion = GetCellValue(row, 6),         // Columna F
                    Language = GetCellValue(row, 7),          // Columna G
                    Rarity = GetCellValue(row, 8),            // Columna H
                    Finish = GetCellValue(row, 9),            // Columna I
                    Price = ParseDecimal(GetCellValue(row, 10)),        // Columna J
                    SalePrice = ParseDecimal(GetCellValue(row, 11)),    // Columna K
                    Stock = ParseInt(GetCellValue(row, 12)),            // Columna L
                    Description = GetCellValue(row, 13)       // Columna M
                };

                products.Add(dto);
            }
            catch (Exception ex)
            {
                // Si hay error leyendo la fila, agregar error pero continuar
                products.Add(new ProductImportDto
                {
                    RowNumber = rowNumber,
                    Code = $"ERROR_ROW_{rowNumber}",
                    Name = $"Error al leer fila: {ex.Message}"
                });
            }

            rowNumber++;
        }

        return products;
    }

    // ✅ NUEVO: Método helper para leer celdas de forma segura
    private string GetCellValue(IXLRow row, int columnIndex)
    {
        try
        {
            var cell = row.Cell(columnIndex);
            if (cell == null || cell.IsEmpty())
                return string.Empty;

            return cell.GetString().Trim();
        }
        catch
        {
            return string.Empty;
        }
    }

    /// <summary>
    /// Valida los datos de un producto
    /// </summary>
    private ImportError? ValidateProduct(ProductImportDto dto)
    {
        // Código requerido
        if (string.IsNullOrWhiteSpace(dto.Code))
            return new ImportError
            {
                Row = dto.RowNumber,
                Code = dto.Code,
                ProductName = dto.Name,
                ErrorMessage = "El código es obligatorio",
                ErrorType = "Validation"
            };

        if (dto.Code.Length > 50)
            return new ImportError
            {
                Row = dto.RowNumber,
                Code = dto.Code,
                ProductName = dto.Name,
                ErrorMessage = "El código no puede exceder 50 caracteres",
                ErrorType = "Validation"
            };

        // Nombre requerido
        if (string.IsNullOrWhiteSpace(dto.Name))
            return new ImportError
            {
                Row = dto.RowNumber,
                Code = dto.Code,
                ProductName = dto.Name,
                ErrorMessage = "El nombre es obligatorio",
                ErrorType = "Validation"
            };

        if (dto.Name.Length > 200)
            return new ImportError
            {
                Row = dto.RowNumber,
                Code = dto.Code,
                ProductName = dto.Name,
                ErrorMessage = "El nombre no puede exceder 200 caracteres",
                ErrorType = "Validation"
            };

        // Categoría requerida
        if (string.IsNullOrWhiteSpace(dto.Category))
            return new ImportError
            {
                Row = dto.RowNumber,
                Code = dto.Code,
                ProductName = dto.Name,
                ErrorMessage = "La categoría es obligatoria",
                ErrorType = "Validation"
            };

        // Precio debe ser mayor a 0
        if (dto.Price <= 0)
            return new ImportError
            {
                Row = dto.RowNumber,
                Code = dto.Code,
                ProductName = dto.Name,
                ErrorMessage = "El precio debe ser mayor a 0",
                ErrorType = "Validation"
            };

        // Stock debe ser >= 0
        if (dto.Stock < 0)
            return new ImportError
            {
                Row = dto.RowNumber,
                Code = dto.Code,
                ProductName = dto.Name,
                ErrorMessage = "El stock no puede ser negativo",
                ErrorType = "Validation"
            };

        // Descripción máximo 500 caracteres
        if (!string.IsNullOrWhiteSpace(dto.Description) && dto.Description.Length > 500)
            return new ImportError
            {
                Row = dto.RowNumber,
                Code = dto.Code,
                ProductName = dto.Name,
                ErrorMessage = "La descripción no puede exceder 500 caracteres",
                ErrorType = "Validation"
            };

        return null;
    }

    /// <summary>
    /// Exporta una plantilla Excel de ejemplo
    /// </summary>
    public void ExportTemplateAsync(string savePath)
    {
        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add("Productos");

        // Encabezados
        var headers = new[]
        {
            "Código", "Nombre", "Módulo", "Categoría", "Promo-Especial",
            "Expansión", "Idioma", "Rareza", "Acabado", "Precio",
            "Venta", "Stock", "Descripción"
        };

        for (int i = 0; i < headers.Length; i++)
        {
            var cell = worksheet.Cell(1, i + 1);
            cell.Value = headers[i];
            cell.Style.Font.Bold = true;
            cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#3498DB");
            cell.Style.Font.FontColor = XLColor.White;
        }

        // Ejemplos
        worksheet.Cell(2, 1).Value = "PKM-001";
        worksheet.Cell(2, 2).Value = "Pikachu ex";
        worksheet.Cell(2, 3).Value = "Base";
        worksheet.Cell(2, 4).Value = "Single";
        worksheet.Cell(2, 5).Value = "no";
        worksheet.Cell(2, 6).Value = "Escarlata y Púrpura";
        worksheet.Cell(2, 7).Value = "Español";
        worksheet.Cell(2, 8).Value = "Ultra Rara";
        worksheet.Cell(2, 9).Value = "Holo";
        worksheet.Cell(2, 10).Value = 25.50;
        worksheet.Cell(2, 11).Value = 30.00;
        worksheet.Cell(2, 12).Value = 5;
        worksheet.Cell(2, 13).Value = "Carta promocional de Pikachu";

        worksheet.Cell(3, 1).Value = "PKM-002";
        worksheet.Cell(3, 2).Value = "Charizard";
        worksheet.Cell(3, 3).Value = "Base";
        worksheet.Cell(3, 4).Value = "Single";
        worksheet.Cell(3, 5).Value = "sí";
        worksheet.Cell(3, 6).Value = "151";
        worksheet.Cell(3, 7).Value = "Español";
        worksheet.Cell(3, 8).Value = "Rara";
        worksheet.Cell(3, 9).Value = "Regular";
        worksheet.Cell(3, 10).Value = 15.00;
        worksheet.Cell(3, 11).Value = 20.00;
        worksheet.Cell(3, 12).Value = 3;
        worksheet.Cell(3, 13).Value = "";

        worksheet.Cell(4, 1).Value = "PKM-003";
        worksheet.Cell(4, 2).Value = "Mewtwo V";
        worksheet.Cell(4, 3).Value = "V";
        worksheet.Cell(4, 4).Value = "Single";
        worksheet.Cell(4, 5).Value = "no";
        worksheet.Cell(4, 6).Value = "Obsidiana Flamígera";
        worksheet.Cell(4, 7).Value = "Inglés";
        worksheet.Cell(4, 8).Value = "Ultra Rara";
        worksheet.Cell(4, 9).Value = "Full Art";
        worksheet.Cell(4, 10).Value = 45.00;
        worksheet.Cell(4, 11).Value = 55.00;
        worksheet.Cell(4, 12).Value = 2;
        worksheet.Cell(4, 13).Value = "Mewtwo versión V Full Art";

        // Ajustar ancho de columnas
        worksheet.Columns().AdjustToContents();

        workbook.SaveAs(savePath);
    }

    // Helpers
    private decimal ParseDecimal(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return 0;

        return decimal.TryParse(value.Replace(",", "."), out var result) ? result : 0;
    }

    private int ParseInt(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return 0;

        return int.TryParse(value, out var result) ? result : 0;
    }
}