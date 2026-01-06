using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Dapper;
using PosPokemon.App.Data;
using PosPokemon.App.Models;

namespace PosPokemon.App.Repositories;

public sealed class ProductRepository
{
    private readonly Db _db;

    public ProductRepository(Db db) => _db = db;

    public async Task<long> CreateAsync(Product p)
    {
        var now = DateTime.UtcNow.ToString("O");
        p.CreatedUtc = now;
        p.UpdatedUtc = now;

        const string sql = @"
INSERT INTO products (sku, name, category, tcg, set_name, rarity, language, cost, price, stock, created_utc, updated_utc)
VALUES (@Sku, @Name, @Category, @Tcg, @SetName, @Rarity, @Language, @Cost, @Price, @Stock, @CreatedUtc, @UpdatedUtc);
SELECT last_insert_rowid();";

        using var conn = _db.OpenConnection();
        return await conn.ExecuteScalarAsync<long>(sql, p);
    }

    public async Task UpdateAsync(Product p)
    {
        p.UpdatedUtc = DateTime.UtcNow.ToString("O");

        const string sql = @"
UPDATE products
SET sku = @Sku,
    name = @Name,
    category = @Category,
    tcg = @Tcg,
    set_name = @SetName,
    rarity = @Rarity,
    language = @Language,
    cost = @Cost,
    price = @Price,
    stock = @Stock,
    updated_utc = @UpdatedUtc
WHERE id = @Id;";

        using var conn = _db.OpenConnection();
        await conn.ExecuteAsync(sql, p);
    }

    public async Task DeleteAsync(long productId)
    {
        const string sql = "DELETE FROM products WHERE id = @productId;";

        using var conn = _db.OpenConnection();
        await conn.ExecuteAsync(sql, new { productId });
    }

    public async Task<List<Product>> SearchAsync(string query)
    {
        const string sql = @"
SELECT * FROM products
WHERE name LIKE @q OR sku LIKE @q OR category LIKE @q OR tcg LIKE @q
ORDER BY updated_utc DESC
LIMIT 100;";

        using var conn = _db.OpenConnection();
        var res = await conn.QueryAsync<Product>(sql, new { q = $"%{query}%" });
        return res.AsList();
    }

    public async Task<Product?> GetByIdAsync(long id)
    {
        const string sql = "SELECT * FROM products WHERE id=@id LIMIT 1;";
        using var conn = _db.OpenConnection();
        return await conn.QueryFirstOrDefaultAsync<Product>(sql, new { id });
    }

    public async Task<Product?> GetBySkuAsync(string sku)
    {
        const string sql = "SELECT * FROM products WHERE sku=@sku LIMIT 1;";
        using var conn = _db.OpenConnection();
        return await conn.QueryFirstOrDefaultAsync<Product>(sql, new { sku });
    }

    public async Task UpdateStockAsync(long productId, int newStock)
    {
        const string sql = @"
UPDATE products
SET stock=@newStock, updated_utc=@now
WHERE id=@productId;";

        using var conn = _db.OpenConnection();
        await conn.ExecuteAsync(sql, new { productId, newStock, now = DateTime.UtcNow.ToString("O") });
    }

    public async Task<int> GetTotalCountAsync()
    {
        const string sql = "SELECT COUNT(*) FROM products;";
        using var conn = _db.OpenConnection();
        return await conn.ExecuteScalarAsync<int>(sql);
    }

    public async Task<int> GetLowStockCountAsync(int threshold = 5)
    {
        const string sql = "SELECT COUNT(*) FROM products WHERE stock <= @threshold;";
        using var conn = _db.OpenConnection();
        return await conn.ExecuteScalarAsync<int>(sql, new { threshold });
    }
}