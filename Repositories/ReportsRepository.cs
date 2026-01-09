using System.Collections.Generic;
using System.Threading.Tasks;
using Dapper;
using PosPokemon.App.Data;

namespace PosPokemon.App.Repositories;

public sealed class ReportsRepository
{
    private readonly Db _db;

    public ReportsRepository(Db db) => _db = db;

    // RESUMEN GENERAL
    public async Task<ReportSummary> GetSummaryAsync(string fromUtc, string toUtc)
    {
        const string sql = @"
SELECT
  COUNT(*) AS SalesCount,
  COALESCE(SUM(total),0) AS TotalSales,
  COALESCE(AVG(total),0) AS AvgTicket
FROM sales
WHERE created_utc >= @fromUtc AND created_utc < @toUtc;
";
        using var conn = _db.OpenConnection();
        return await conn.QueryFirstAsync<ReportSummary>(sql, new { fromUtc, toUtc });
    }

    // VENTAS POR USUARIO
    public async Task<List<SalesByUserRow>> GetSalesByUserAsync(string fromUtc, string toUtc)
    {
        const string sql = @"
SELECT
  u.username AS Username,
  COUNT(s.id) AS SalesCount,
  COALESCE(SUM(s.total),0) AS Total
FROM sales s
JOIN users u ON u.id = s.user_id
WHERE s.created_utc >= @fromUtc AND s.created_utc < @toUtc
GROUP BY u.username
ORDER BY Total DESC;
";
        using var conn = _db.OpenConnection();
        var rows = await conn.QueryAsync<SalesByUserRow>(sql, new { fromUtc, toUtc });
        return rows.AsList();
    }

    // TOP PRODUCTOS
    public async Task<List<TopProductRow>> GetTopProductsAsync(string fromUtc, string toUtc)
    {
        const string sql = @"
SELECT
  p.code AS Code,
  p.name AS Name,
  SUM(si.qty) AS Qty,
  SUM(si.qty * si.unit_price) AS Total
FROM sale_items si
JOIN sales s ON s.id = si.sale_id
JOIN products p ON p.id = si.product_id
WHERE s.created_utc >= @fromUtc AND s.created_utc < @toUtc
GROUP BY p.code, p.name
ORDER BY Total DESC
LIMIT 20;
";
        using var conn = _db.OpenConnection();
        var rows = await conn.QueryAsync<TopProductRow>(sql, new { fromUtc, toUtc });
        return rows.AsList();
    }
}

// MODELOS DE REPORTE
public sealed class ReportSummary
{
    public int SalesCount { get; set; }
    public decimal TotalSales { get; set; }
    public decimal AvgTicket { get; set; }
}

public sealed class SalesByUserRow
{
    public string Username { get; set; } = "";
    public int SalesCount { get; set; }
    public decimal Total { get; set; }
}

public sealed class TopProductRow
{
    public string Code { get; set; } = "";  // ✅ CAMBIADO: Sku → Code
    public string Name { get; set; } = "";
    public int Qty { get; set; }
    public decimal Total { get; set; }
}