using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using PosPokemon.App.Data;
using PosPokemon.App.Models;

namespace PosPokemon.App.Repositories;

public sealed class DiscountCampaignRepository
{
    private readonly Db _db;

    public DiscountCampaignRepository(Db db) => _db = db;

    /// <summary>
    /// Crear campaña de descuento
    /// </summary>
    public async Task<long> CreateCampaignAsync(DiscountCampaign campaign, List<long> productIds)
    {
        var now = DateTime.UtcNow.ToString("O");
        campaign.CreatedUtc = now;
        campaign.UpdatedUtc = now;

        using var conn = _db.OpenConnection();
        conn.Open();
        using var tx = conn.BeginTransaction();

        try
        {
            // Insertar campaña
            const string sqlCampaign = @"
INSERT INTO discount_campaigns (name, discount_percentage, start_date, end_date, is_active, created_by, created_utc, updated_utc)
VALUES (@Name, @DiscountPercentage, @StartDate, @EndDate, @IsActive, @CreatedBy, @CreatedUtc, @UpdatedUtc);
SELECT last_insert_rowid();";

            var campaignId = await conn.ExecuteScalarAsync<long>(sqlCampaign, campaign, tx);

            // Insertar productos
            const string sqlProduct = @"
INSERT INTO discount_campaign_products (campaign_id, product_id)
VALUES (@CampaignId, @ProductId);";

            foreach (var productId in productIds)
            {
                await conn.ExecuteAsync(sqlProduct, new { CampaignId = campaignId, ProductId = productId }, tx);
            }

            tx.Commit();
            return campaignId;
        }
        catch
        {
            tx.Rollback();
            throw;
        }
    }

    /// <summary>
    /// Obtener todas las campañas
    /// </summary>
    public async Task<List<DiscountCampaign>> GetAllCampaignsAsync()
    {
        const string sql = @"
SELECT 
    dc.id as Id,
    dc.name as Name,
    dc.discount_percentage as DiscountPercentage,
    dc.start_date as StartDate,
    dc.end_date as EndDate,
    dc.is_active as IsActive,
    dc.created_by as CreatedBy,
    dc.created_utc as CreatedUtc,
    dc.updated_utc as UpdatedUtc,
    u.username as CreatorUsername,
    COUNT(dcp.id) as ProductCount
FROM discount_campaigns dc
LEFT JOIN users u ON dc.created_by = u.id
LEFT JOIN discount_campaign_products dcp ON dc.id = dcp.campaign_id
GROUP BY dc.id
ORDER BY dc.created_utc DESC;";

        using var conn = _db.OpenConnection();
        var result = await conn.QueryAsync<DiscountCampaign>(sql);
        return result.AsList();
    }

    /// <summary>
    /// Obtener campaña por ID con productos
    /// </summary>
    public async Task<(DiscountCampaign? campaign, List<Product> products)> GetCampaignWithProductsAsync(long campaignId)
    {
        using var conn = _db.OpenConnection();

        const string sqlCampaign = @"
SELECT 
    dc.id as Id,
    dc.name as Name,
    dc.discount_percentage as DiscountPercentage,
    dc.start_date as StartDate,
    dc.end_date as EndDate,
    dc.is_active as IsActive,
    dc.created_by as CreatedBy,
    dc.created_utc as CreatedUtc,
    dc.updated_utc as UpdatedUtc,
    u.username as CreatorUsername
FROM discount_campaigns dc
LEFT JOIN users u ON dc.created_by = u.id
WHERE dc.id = @campaignId;";

        var campaign = await conn.QueryFirstOrDefaultAsync<DiscountCampaign>(sqlCampaign, new { campaignId });

        if (campaign == null)
            return (null, new List<Product>());

        const string sqlProducts = @"
SELECT p.*
FROM products p
INNER JOIN discount_campaign_products dcp ON p.id = dcp.product_id
WHERE dcp.campaign_id = @campaignId;";

        var products = await conn.QueryAsync<Product>(sqlProducts, new { campaignId });

        return (campaign, products.AsList());
    }

    /// <summary>
    /// Verificar si un producto tiene descuento activo
    /// </summary>
    public async Task<ProductWithDiscount> GetProductWithDiscountAsync(long productId)
    {
        const string sql = @"
SELECT 
    p.id as Id,
    p.sku as Sku,
    p.name as Name,
    p.category as Category,
    p.tcg as Tcg,
    p.set_name as SetName,
    p.rarity as Rarity,
    p.language as Language,
    p.cost as Cost,
    p.price as Price,
    p.stock as Stock,
    p.created_utc as CreatedUtc,
    p.updated_utc as UpdatedUtc,
    dc.name as CampaignName,
    dc.discount_percentage as DiscountPercentage
FROM products p
LEFT JOIN discount_campaign_products dcp ON p.id = dcp.product_id
LEFT JOIN discount_campaigns dc ON dcp.campaign_id = dc.id
    AND dc.is_active = 1
    AND date('now') BETWEEN date(dc.start_date) AND date(dc.end_date)
WHERE p.id = @productId
LIMIT 1;";

        using var conn = _db.OpenConnection();

        try
        {
            var result = await conn.QueryFirstOrDefaultAsync<dynamic>(sql, new { productId });

            if (result == null)
            {
                return new ProductWithDiscount { HasActiveDiscount = false };
            }

            var product = new Product
            {
                Id = (long)result.Id,
                Sku = (string)result.Sku ?? "",
                Name = (string)result.Name ?? "",
                Category = (string)result.Category ?? "",
                Tcg = (string)result.Tcg ?? "",
                SetName = result.SetName != null ? (string)result.SetName : "",
                Rarity = result.Rarity != null ? (string)result.Rarity : "",
                Language = result.Language != null ? (string)result.Language : "",
                Cost = Convert.ToDecimal((double)result.Cost),
                Price = Convert.ToDecimal((double)result.Price),
                Stock = (long)result.Stock,
                CreatedUtc = (string)result.CreatedUtc ?? "",
                UpdatedUtc = (string)result.UpdatedUtc ?? ""
            };

            // ✅ VERIFICAR SI HAY DESCUENTO (puede ser null)
            var hasDiscount = result.DiscountPercentage != null && result.CampaignName != null;
            var discountPercentage = hasDiscount ? Convert.ToDecimal((double)result.DiscountPercentage) : 0m;
            var discountedPrice = hasDiscount ? product.Price * (1 - discountPercentage / 100) : product.Price;

            return new ProductWithDiscount
            {
                Product = product,
                OriginalPrice = product.Price,
                DiscountPercentage = discountPercentage,
                DiscountedPrice = discountedPrice,
                CampaignName = hasDiscount ? (string)result.CampaignName : "",
                HasActiveDiscount = hasDiscount
            };
        }
        catch (System.Exception)
        {
            // Si hay cualquier error, devolver producto sin descuento
            return new ProductWithDiscount { HasActiveDiscount = false };
        }
    }

    /// <summary>
    /// Desactivar campaña
    /// </summary>
    public async Task DeactivateCampaignAsync(long campaignId)
    {
        const string sql = @"
UPDATE discount_campaigns
SET is_active = 0, updated_utc = @now
WHERE id = @campaignId;";

        using var conn = _db.OpenConnection();
        await conn.ExecuteAsync(sql, new { campaignId, now = DateTime.UtcNow.ToString("O") });
    }

    /// <summary>
    /// Activar campaña
    /// </summary>
    public async Task ActivateCampaignAsync(long campaignId)
    {
        const string sql = @"
UPDATE discount_campaigns
SET is_active = 1, updated_utc = @now
WHERE id = @campaignId;";

        using var conn = _db.OpenConnection();
        await conn.ExecuteAsync(sql, new { campaignId, now = DateTime.UtcNow.ToString("O") });
    }

    /// <summary>
    /// Eliminar campaña
    /// </summary>
    public async Task DeleteCampaignAsync(long campaignId)
    {
        const string sql = "DELETE FROM discount_campaigns WHERE id = @campaignId;";

        using var conn = _db.OpenConnection();
        await conn.ExecuteAsync(sql, new { campaignId });
    }

    /// <summary>
    /// Actualizar campaña
    /// </summary>
    public async Task UpdateCampaignAsync(DiscountCampaign campaign, List<long> productIds)
    {
        campaign.UpdatedUtc = DateTime.UtcNow.ToString("O");

        using var conn = _db.OpenConnection();
        conn.Open();
        using var tx = conn.BeginTransaction();

        try
        {
            // Actualizar campaña
            const string sqlUpdate = @"
UPDATE discount_campaigns
SET name = @Name,
    discount_percentage = @DiscountPercentage,
    start_date = @StartDate,
    end_date = @EndDate,
    updated_utc = @UpdatedUtc
WHERE id = @Id;";

            await conn.ExecuteAsync(sqlUpdate, campaign, tx);

            // Eliminar productos anteriores
            const string sqlDelete = "DELETE FROM discount_campaign_products WHERE campaign_id = @Id;";
            await conn.ExecuteAsync(sqlDelete, new { campaign.Id }, tx);

            // Insertar nuevos productos
            const string sqlInsert = @"
INSERT INTO discount_campaign_products (campaign_id, product_id)
VALUES (@CampaignId, @ProductId);";

            foreach (var productId in productIds)
            {
                await conn.ExecuteAsync(sqlInsert, new { CampaignId = campaign.Id, ProductId = productId }, tx);
            }

            tx.Commit();
        }
        catch
        {
            tx.Rollback();
            throw;
        }
    }
}