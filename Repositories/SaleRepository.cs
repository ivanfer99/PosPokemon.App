using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using PosPokemon.App.Data;
using PosPokemon.App.Models;

namespace PosPokemon.App.Repositories;

public sealed class SaleRepository
{
    private readonly Db _db;

    public SaleRepository(Db db) => _db = db;

    public async Task CreateSaleAsync(Sale sale, List<SaleItem> items)
    {
        var now = DateTime.UtcNow.ToString("O");
        sale.CreatedUtc = now;
        sale.UpdatedUtc = now;

        using var conn = _db.OpenConnection();
        using var tx = conn.BeginTransaction();

        try
        {
            // Insertar venta
            const string sqlSale = @"
INSERT INTO sales (sale_number, user_id, subtotal, discount, total, payment_method, note, created_utc, updated_utc)
VALUES (@SaleNumber, @UserId, @Subtotal, @Discount, @Total, @PaymentMethod, @Note, @CreatedUtc, @UpdatedUtc);
SELECT last_insert_rowid();";

            var saleId = await conn.ExecuteScalarAsync<long>(sqlSale, sale, tx);

            // Insertar items
            const string sqlItem = @"
INSERT INTO sale_items (sale_id, product_id, qty, unit_price)
VALUES (@SaleId, @ProductId, @Qty, @UnitPrice);";

            foreach (var item in items)
            {
                item.SaleId = saleId;
                await conn.ExecuteAsync(sqlItem, item, tx);

                // Actualizar stock del producto
                const string sqlUpdateStock = @"
UPDATE products 
SET stock = stock - @Qty, updated_utc = @UpdatedUtc
WHERE id = @ProductId;";

                await conn.ExecuteAsync(sqlUpdateStock, new
                {
                    item.Qty,
                    item.ProductId,
                    UpdatedUtc = now
                }, tx);
            }

            tx.Commit();
        }
        catch
        {
            tx.Rollback();
            throw;
        }
    }

    public async Task<List<SaleWithDetails>> GetSalesByDateRangeAsync(string? startUtc, string? endUtc)
    {
        var sql = @"
SELECT 
    s.id as Id,
    s.sale_number as SaleNumber,
    s.user_id as UserId,
    s.subtotal as Subtotal,
    s.discount as Discount,
    s.total as Total,
    s.payment_method as PaymentMethod,
    s.note as Note,
    s.created_utc as CreatedUtc,
    s.updated_utc as UpdatedUtc,
    u.username as Username,
    COUNT(si.id) as TotalItems
FROM sales s
LEFT JOIN users u ON s.user_id = u.id
LEFT JOIN sale_items si ON s.id = si.sale_id
WHERE 1=1";

        var parameters = new DynamicParameters();

        if (!string.IsNullOrEmpty(startUtc))
        {
            sql += " AND s.created_utc >= @startUtc";
            parameters.Add("startUtc", startUtc);
        }

        if (!string.IsNullOrEmpty(endUtc))
        {
            sql += " AND s.created_utc < @endUtc";
            parameters.Add("endUtc", endUtc);
        }

        sql += @"
GROUP BY s.id
ORDER BY s.created_utc DESC
LIMIT 1000;";

        using var conn = _db.OpenConnection();
        var result = await conn.QueryAsync<SaleWithDetails>(sql, parameters);
        return result.AsList();
    }

    public async Task<SaleWithDetails?> GetBySaleNumberAsync(string saleNumber)
    {
        const string sql = @"
SELECT 
    s.id as Id,
    s.sale_number as SaleNumber,
    s.user_id as UserId,
    s.subtotal as Subtotal,
    s.discount as Discount,
    s.total as Total,
    s.payment_method as PaymentMethod,
    s.note as Note,
    s.created_utc as CreatedUtc,
    s.updated_utc as UpdatedUtc,
    u.username as Username,
    COUNT(si.id) as TotalItems
FROM sales s
LEFT JOIN users u ON s.user_id = u.id
LEFT JOIN sale_items si ON s.id = si.sale_id
WHERE s.sale_number = @saleNumber
GROUP BY s.id
LIMIT 1;";

        using var conn = _db.OpenConnection();
        return await conn.QueryFirstOrDefaultAsync<SaleWithDetails>(sql, new { saleNumber });
    }

    public async Task<List<SaleItemWithProduct>> GetSaleItemsAsync(long saleId)
    {
        const string sql = @"
SELECT 
    si.id as Id,
    si.sale_id as SaleId,
    si.product_id as ProductId,
    si.qty as Qty,
    si.unit_price as UnitPrice,
    p.name as ProductName
FROM sale_items si
LEFT JOIN products p ON si.product_id = p.id
WHERE si.sale_id = @saleId
ORDER BY si.id;";

        using var conn = _db.OpenConnection();
        var result = await conn.QueryAsync<SaleItemWithProduct>(sql, new { saleId });
        return result.AsList();
    }

    public async Task<decimal> GetTotalSalesByDateRangeAsync(string? startUtc, string? endUtc)
    {
        var sql = "SELECT COALESCE(SUM(total), 0) FROM sales WHERE 1=1";
        var parameters = new DynamicParameters();

        if (!string.IsNullOrEmpty(startUtc))
        {
            sql += " AND created_utc >= @startUtc";
            parameters.Add("startUtc", startUtc);
        }

        if (!string.IsNullOrEmpty(endUtc))
        {
            sql += " AND created_utc < @endUtc";
            parameters.Add("endUtc", endUtc);
        }

        using var conn = _db.OpenConnection();
        return await conn.ExecuteScalarAsync<decimal>(sql, parameters);
    }
}
