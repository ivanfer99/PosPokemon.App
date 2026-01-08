using System.Data;
using Dapper;
using Microsoft.Data.Sqlite;
using PosPokemon.App.Models;

namespace PosPokemon.App.Repositories;

public class CategoryRepository : ICategoryRepository
{
    private readonly string _connectionString;

    public CategoryRepository(string connectionString)
    {
        _connectionString = connectionString;
    }

    private IDbConnection CreateConnection() => new SqliteConnection(_connectionString);

    public async Task<List<Category>> GetAllAsync()
    {
        using var conn = CreateConnection();
        const string sql = @"
            SELECT 
                id as Id,
                name as Name,
                description as Description,
                is_active as IsActive,
                created_utc as CreatedUtc,
                updated_utc as UpdatedUtc
            FROM categories
            ORDER BY name;";

        var result = await conn.QueryAsync<Category>(sql);
        return result.AsList();
    }

    public async Task<List<Category>> GetAllActiveAsync()
    {
        using var conn = CreateConnection();
        const string sql = @"
            SELECT 
                id as Id,
                name as Name,
                description as Description,
                is_active as IsActive,
                created_utc as CreatedUtc,
                updated_utc as UpdatedUtc
            FROM categories
            WHERE is_active = 1
            ORDER BY name;";

        var result = await conn.QueryAsync<Category>(sql);
        return result.AsList();
    }

    public async Task<Category?> GetByIdAsync(long id)
    {
        using var conn = CreateConnection();
        const string sql = @"
            SELECT 
                id as Id,
                name as Name,
                description as Description,
                is_active as IsActive,
                created_utc as CreatedUtc,
                updated_utc as UpdatedUtc
            FROM categories
            WHERE id = @id
            LIMIT 1;";

        return await conn.QueryFirstOrDefaultAsync<Category>(sql, new { id });
    }

    public async Task<Category?> GetByNameAsync(string name)
    {
        using var conn = CreateConnection();
        const string sql = @"
            SELECT 
                id as Id,
                name as Name,
                description as Description,
                is_active as IsActive,
                created_utc as CreatedUtc,
                updated_utc as UpdatedUtc
            FROM categories
            WHERE LOWER(name) = LOWER(@name)
            LIMIT 1;";

        return await conn.QueryFirstOrDefaultAsync<Category>(sql, new { name });
    }

    public async Task<Category> CreateAsync(Category category)
    {
        using var conn = CreateConnection();
        category.CreatedUtc = DateTime.UtcNow;
        category.UpdatedUtc = DateTime.UtcNow;

        const string sql = @"
            INSERT INTO categories (name, description, is_active, created_utc, updated_utc)
            VALUES (@Name, @Description, @IsActive, @CreatedUtc, @UpdatedUtc);
            
            SELECT last_insert_rowid();";

        var id = await conn.ExecuteScalarAsync<long>(sql, category);
        category.Id = id;
        return category;
    }

    public async Task<bool> UpdateAsync(Category category)
    {
        using var conn = CreateConnection();
        category.UpdatedUtc = DateTime.UtcNow;

        const string sql = @"
            UPDATE categories
            SET name = @Name,
                description = @Description,
                updated_utc = @UpdatedUtc
            WHERE id = @Id;";

        var rows = await conn.ExecuteAsync(sql, category);
        return rows > 0;
    }

    public async Task<bool> DeleteAsync(long id)
    {
        using var conn = CreateConnection();
        const string sql = @"
            UPDATE categories
            SET is_active = 0,
                updated_utc = @updatedUtc
            WHERE id = @id;";

        var rows = await conn.ExecuteAsync(sql, new { id, updatedUtc = DateTime.UtcNow });
        return rows > 0;
    }

    public async Task<bool> ActivateAsync(long id)
    {
        using var conn = CreateConnection();
        const string sql = @"
            UPDATE categories
            SET is_active = 1,
                updated_utc = @updatedUtc
            WHERE id = @id;";

        var rows = await conn.ExecuteAsync(sql, new { id, updatedUtc = DateTime.UtcNow });
        return rows > 0;
    }
}