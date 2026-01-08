using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Dapper;
using PosPokemon.App.Data;
using PosPokemon.App.Models;

namespace PosPokemon.App.Repositories;

public sealed class ProductRepository : IProductRepository
{
    private readonly Db _db;

    public ProductRepository(Db db) => _db = db;

    public async Task<long> CreateAsync(Product p)
    {
        var now = DateTime.UtcNow.ToString("O");
        p.CreatedUtc = now;
        p.UpdatedUtc = now;

        const string sql = @"
INSERT INTO products (
    code, name, category_id, module, is_promo_special, 
    expansion_id, language, rarity, finish, price, 
    sale_price, stock, description, min_stock, is_active, 
    created_utc, updated_utc
)
VALUES (
    @Code, @Name, @CategoryId, @Module, @IsPromoSpecial, 
    @ExpansionId, @Language, @Rarity, @Finish, @Price, 
    @SalePrice, @Stock, @Description, @MinStock, @IsActive, 
    @CreatedUtc, @UpdatedUtc
);
SELECT last_insert_rowid();";

        using var conn = _db.OpenConnection();
        return await conn.ExecuteScalarAsync<long>(sql, p);
    }

    public async Task UpdateAsync(Product p)
    {
        p.UpdatedUtc = DateTime.UtcNow.ToString("O");

        const string sql = @"
UPDATE products
SET code = @Code,
    name = @Name,
    category_id = @CategoryId,
    module = @Module,
    is_promo_special = @IsPromoSpecial,
    expansion_id = @ExpansionId,
    language = @Language,
    rarity = @Rarity,
    finish = @Finish,
    price = @Price,
    sale_price = @SalePrice,
    stock = @Stock,
    description = @Description,
    min_stock = @MinStock,
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
SELECT 
    p.id as Id,
    p.code as Code,
    p.name as Name,
    p.category_id as CategoryId,
    p.module as Module,
    p.is_promo_special as IsPromoSpecial,
    p.expansion_id as ExpansionId,
    p.language as Language,
    p.rarity as Rarity,
    p.finish as Finish,
    p.price as Price,
    p.sale_price as SalePrice,
    p.stock as Stock,
    p.min_stock as MinStock,
    p.description as Description,
    p.is_active as IsActive,
    p.created_utc as CreatedUtc,
    p.updated_utc as UpdatedUtc,
    c.name as CategoryName,
    e.name as ExpansionName
FROM products p
LEFT JOIN categories c ON p.category_id = c.id
LEFT JOIN expansions e ON p.expansion_id = e.id
WHERE p.name LIKE @q 
   OR p.code LIKE @q 
   OR c.name LIKE @q
   OR p.rarity LIKE @q
   OR p.language LIKE @q
ORDER BY p.updated_utc DESC
LIMIT 100;";

        using var conn = _db.OpenConnection();
        var res = await conn.QueryAsync<Product>(sql, new { q = $"%{query}%" });
        return res.AsList();
    }

    public async Task<Product?> GetByIdAsync(long id)
    {
        const string sql = @"
SELECT 
    p.id as Id,
    p.code as Code,
    p.name as Name,
    p.category_id as CategoryId,
    p.module as Module,
    p.is_promo_special as IsPromoSpecial,
    p.expansion_id as ExpansionId,
    p.language as Language,
    p.rarity as Rarity,
    p.finish as Finish,
    p.price as Price,
    p.sale_price as SalePrice,
    p.stock as Stock,
    p.min_stock as MinStock,
    p.description as Description,
    p.is_active as IsActive,
    p.created_utc as CreatedUtc,
    p.updated_utc as UpdatedUtc,
    c.name as CategoryName,
    e.name as ExpansionName
FROM products p
LEFT JOIN categories c ON p.category_id = c.id
LEFT JOIN expansions e ON p.expansion_id = e.id
WHERE p.id = @id 
LIMIT 1;";

        using var conn = _db.OpenConnection();
        return await conn.QueryFirstOrDefaultAsync<Product>(sql, new { id });
    }

    public async Task<Product?> GetBySkuAsync(string sku)
    {
        const string sql = @"
SELECT 
    p.id as Id,
    p.code as Code,
    p.name as Name,
    p.category_id as CategoryId,
    p.module as Module,
    p.is_promo_special as IsPromoSpecial,
    p.expansion_id as ExpansionId,
    p.language as Language,
    p.rarity as Rarity,
    p.finish as Finish,
    p.price as Price,
    p.sale_price as SalePrice,
    p.stock as Stock,
    p.min_stock as MinStock,
    p.description as Description,
    p.is_active as IsActive,
    p.created_utc as CreatedUtc,
    p.updated_utc as UpdatedUtc,
    c.name as CategoryName,
    e.name as ExpansionName
FROM products p
LEFT JOIN categories c ON p.category_id = c.id
LEFT JOIN expansions e ON p.expansion_id = e.id
WHERE p.code = @sku 
LIMIT 1;";

        using var conn = _db.OpenConnection();
        return await conn.QueryFirstOrDefaultAsync<Product>(sql, new { sku });
    }

    public async Task UpdateStockAsync(long productId, int newStock)
    {
        const string sql = @"
UPDATE products
SET stock = @newStock, 
    updated_utc = @now
WHERE id = @productId;";

        using var conn = _db.OpenConnection();
        await conn.ExecuteAsync(sql, new
        {
            productId,
            newStock,
            now = DateTime.UtcNow.ToString("O")
        });
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