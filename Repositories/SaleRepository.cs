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

    public async Task<long> CreateSaleAsync(Sale sale, List<SaleItem> items)
    {
        using var conn = _db.OpenConnection();
        using var tx = conn.BeginTransaction();

        try
        {
            sale.CreatedUtc = DateTime.UtcNow.ToString("O");

            const string saleSql = @"
INSERT INTO sales (sale_number, created_utc, subtotal, discount, total, payment_method, note)
VALUES (@SaleNumber, @CreatedUtc, @Subtotal, @Discount, @Total, @PaymentMethod, @Note);
SELECT last_insert_rowid();";

            var saleId = await conn.ExecuteScalarAsync<long>(saleSql, sale, tx);

            const string itemSql = @"
INSERT INTO sale_items (sale_id, product_id, qty, unit_price, line_total)
VALUES (@SaleId, @ProductId, @Qty, @UnitPrice, @LineTotal);";

            foreach (var it in items)
            {
                it.SaleId = saleId;
                it.LineTotal = it.UnitPrice * it.Qty;
                await conn.ExecuteAsync(itemSql, it, tx);

                // bajar stock
                const string stockSql = @"
UPDATE products
SET stock = stock - @qty, updated_utc=@now
WHERE id=@productId;";
                await conn.ExecuteAsync(stockSql, new
                {
                    qty = it.Qty,
                    productId = it.ProductId,
                    now = DateTime.UtcNow.ToString("O")
                }, tx);

                const string movSql = @"
INSERT INTO stock_movements (product_id, type, qty, reason, created_utc)
VALUES (@productId, 'OUT', @qty, @reason, @now);";
                await conn.ExecuteAsync(movSql, new
                {
                    productId = it.ProductId,
                    qty = it.Qty,
                    reason = $"SALE {sale.SaleNumber}",
                    now = DateTime.UtcNow.ToString("O")
                }, tx);
            }

            tx.Commit();
            return saleId;
        }
        catch
        {
            tx.Rollback();
            throw;
        }
    }
}
