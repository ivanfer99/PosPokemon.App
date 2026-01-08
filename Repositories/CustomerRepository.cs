using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using PosPokemon.App.Data;
using PosPokemon.App.Models;

namespace PosPokemon.App.Repositories;

public sealed class CustomerRepository
{
    private readonly Db _db;

    public CustomerRepository(Db db) => _db = db;

    /// <summary>
    /// Crear cliente
    /// </summary>
    public async Task<long> CreateAsync(Customer customer)
    {
        var now = DateTime.UtcNow.ToString("O");
        customer.CreatedUtc = now;
        customer.UpdatedUtc = now;

        const string sql = @"
INSERT INTO customers (document_type, document_number, name, phone, email, address, notes, is_active, created_utc, updated_utc)
VALUES (@DocumentType, @DocumentNumber, @Name, @Phone, @Email, @Address, @Notes, @IsActive, @CreatedUtc, @UpdatedUtc);
SELECT last_insert_rowid();";

        using var conn = _db.OpenConnection();
        var id = await conn.ExecuteScalarAsync<long>(sql, customer);
        return id;
    }

    /// <summary>
    /// Obtener todos los clientes activos
    /// </summary>
    public async Task<List<Customer>> GetAllActiveAsync()
    {
        const string sql = @"
SELECT 
    id as Id,
    document_type as DocumentType,
    document_number as DocumentNumber,
    name as Name,
    phone as Phone,
    email as Email,
    address as Address,
    notes as Notes,
    is_active as IsActive,
    created_utc as CreatedUtc,
    updated_utc as UpdatedUtc
FROM customers
WHERE is_active = 1
ORDER BY name ASC;";

        using var conn = _db.OpenConnection();
        var result = await conn.QueryAsync<Customer>(sql);
        return result.AsList();
    }

    /// <summary>
    /// Obtener todos los clientes (incluidos inactivos)
    /// </summary>
    public async Task<List<Customer>> GetAllAsync()
    {
        const string sql = @"
SELECT 
    id as Id,
    document_type as DocumentType,
    document_number as DocumentNumber,
    name as Name,
    phone as Phone,
    email as Email,
    address as Address,
    notes as Notes,
    is_active as IsActive,
    created_utc as CreatedUtc,
    updated_utc as UpdatedUtc
FROM customers
ORDER BY name ASC;";

        using var conn = _db.OpenConnection();
        var result = await conn.QueryAsync<Customer>(sql);
        return result.AsList();
    }

    /// <summary>
    /// Buscar clientes por nombre o documento
    /// </summary>
    public async Task<List<Customer>> SearchAsync(string query)
    {
        if (string.IsNullOrWhiteSpace(query))
            return await GetAllActiveAsync();

        const string sql = @"
SELECT 
    id as Id,
    document_type as DocumentType,
    document_number as DocumentNumber,
    name as Name,
    phone as Phone,
    email as Email,
    address as Address,
    notes as Notes,
    is_active as IsActive,
    created_utc as CreatedUtc,
    updated_utc as UpdatedUtc
FROM customers
WHERE is_active = 1
  AND (name LIKE @query OR document_number LIKE @query)
ORDER BY name ASC
LIMIT 20;";

        using var conn = _db.OpenConnection();
        var result = await conn.QueryAsync<Customer>(sql, new { query = $"%{query}%" });
        return result.AsList();
    }

    /// <summary>
    /// Obtener cliente por ID
    /// </summary>
    public async Task<Customer?> GetByIdAsync(long id)
    {
        const string sql = @"
SELECT 
    id as Id,
    document_type as DocumentType,
    document_number as DocumentNumber,
    name as Name,
    phone as Phone,
    email as Email,
    address as Address,
    notes as Notes,
    is_active as IsActive,
    created_utc as CreatedUtc,
    updated_utc as UpdatedUtc
FROM customers 
WHERE id = @id;";

        using var conn = _db.OpenConnection();
        return await conn.QueryFirstOrDefaultAsync<Customer>(sql, new { id });
    }

    /// <summary>
    /// Obtener cliente por documento
    /// </summary>
    public async Task<Customer?> GetByDocumentAsync(string documentNumber)
    {
        const string sql = @"
SELECT 
    id as Id,
    document_type as DocumentType,
    document_number as DocumentNumber,
    name as Name,
    phone as Phone,
    email as Email,
    address as Address,
    notes as Notes,
    is_active as IsActive,
    created_utc as CreatedUtc,
    updated_utc as UpdatedUtc
FROM customers 
WHERE document_number = @documentNumber;";

        using var conn = _db.OpenConnection();
        return await conn.QueryFirstOrDefaultAsync<Customer>(sql, new { documentNumber });
    }

    /// <summary>
    /// Actualizar cliente
    /// </summary>
    public async Task UpdateAsync(Customer customer)
    {
        customer.UpdatedUtc = DateTime.UtcNow.ToString("O");

        const string sql = @"
UPDATE customers
SET document_type = @DocumentType,
    document_number = @DocumentNumber,
    name = @Name,
    phone = @Phone,
    email = @Email,
    address = @Address,
    notes = @Notes,
    is_active = @IsActive,
    updated_utc = @UpdatedUtc
WHERE id = @Id;";

        using var conn = _db.OpenConnection();
        await conn.ExecuteAsync(sql, customer);
    }

    /// <summary>
    /// Eliminar cliente (soft delete)
    /// </summary>
    public async Task DeleteAsync(long id)
    {
        const string sql = @"
UPDATE customers
SET is_active = 0, updated_utc = @now
WHERE id = @id;";

        using var conn = _db.OpenConnection();
        await conn.ExecuteAsync(sql, new { id, now = DateTime.UtcNow.ToString("O") });
    }

    /// <summary>
    /// Activar cliente
    /// </summary>
    public async Task ActivateAsync(long id)
    {
        const string sql = @"
UPDATE customers
SET is_active = 1, updated_utc = @now
WHERE id = @id;";

        using var conn = _db.OpenConnection();
        await conn.ExecuteAsync(sql, new { id, now = DateTime.UtcNow.ToString("O") });
    }

    /// <summary>
    /// Obtener top clientes por compras
    /// </summary>
    public async Task<List<CustomerWithStats>> GetTopCustomersAsync(int limit = 10)
    {
        const string sql = @"
SELECT 
    c.id as Id,
    c.document_type as DocumentType,
    c.document_number as DocumentNumber,
    c.name as Name,
    c.phone as Phone,
    c.email as Email,
    c.address as Address,
    c.notes as Notes,
    c.is_active as IsActive,
    c.created_utc as CreatedUtc,
    c.updated_utc as UpdatedUtc,
    COUNT(s.id) as TotalPurchases,
    COALESCE(SUM(s.total), 0) as TotalSpent,
    MAX(s.created_utc) as LastPurchaseDate
FROM customers c
LEFT JOIN sales s ON c.id = s.customer_id
WHERE c.is_active = 1
GROUP BY c.id
ORDER BY TotalSpent DESC
LIMIT @limit;";

        using var conn = _db.OpenConnection();
        var result = await conn.QueryAsync<dynamic>(sql, new { limit });

        return result.Select(r => new CustomerWithStats
        {
            Customer = new Customer
            {
                Id = (long)r.Id,
                DocumentType = (string)r.DocumentType,
                DocumentNumber = (string)r.DocumentNumber,
                Name = (string)r.Name,
                Phone = r.Phone,
                Email = r.Email,
                Address = r.Address,
                Notes = r.Notes,
                IsActive = (long)r.IsActive,
                CreatedUtc = (string)r.CreatedUtc,
                UpdatedUtc = (string)r.UpdatedUtc
            },
            TotalPurchases = (long)r.TotalPurchases,
            TotalSpent = r.TotalSpent != null ? Convert.ToDecimal((double)r.TotalSpent) : 0,
            LastPurchaseDate = r.LastPurchaseDate ?? ""
        }).ToList();
    }

    /// <summary>
    /// Obtener historial de compras de un cliente
    /// ✅ CORREGIDO: Ahora mapea correctamente todas las columnas
    /// </summary>
    public async Task<List<SaleWithDetails>> GetCustomerPurchasesAsync(long customerId)
    {
        const string sql = @"
SELECT 
    s.id as Id,
    s.sale_number as SaleNumber,
    s.user_id as UserId,
    s.customer_id as CustomerId,
    s.subtotal as Subtotal,
    s.discount as Discount,
    s.total as Total,
    s.payment_method as PaymentMethod,
    s.amount_received as AmountReceived,
    s.change as Change,
    s.note as Note,
    s.created_utc as CreatedUtc,
    s.updated_utc as UpdatedUtc,
    u.username as Username
FROM sales s
LEFT JOIN users u ON s.user_id = u.id
WHERE s.customer_id = @customerId
ORDER BY s.created_utc DESC;";

        using var conn = _db.OpenConnection();
        var sales = await conn.QueryAsync<SaleWithDetails>(sql, new { customerId });
        return sales.AsList();
    }

    /// <summary>
    /// Obtener total de clientes activos
    /// </summary>
    public async Task<long> GetTotalActiveCountAsync()
    {
        const string sql = "SELECT COUNT(*) FROM customers WHERE is_active = 1;";

        using var conn = _db.OpenConnection();
        return await conn.ExecuteScalarAsync<long>(sql);
    }
}