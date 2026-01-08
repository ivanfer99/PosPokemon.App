using System.Data;
using Dapper;
using Microsoft.Data.Sqlite;
using PosPokemon.App.Models;

namespace PosPokemon.App.Repositories;

public class ExpansionRepository : IExpansionRepository
{
    private readonly string _connectionString;

    public ExpansionRepository(string connectionString)
    {
        _connectionString = connectionString;
    }

    private IDbConnection CreateConnection() => new SqliteConnection(_connectionString);

    public async Task<List<Expansion>> GetAllActiveAsync()
    {
        using var conn = CreateConnection();
        const string sql = @"
            SELECT 
                id as Id,
                name as Name,
                code as Code,
                release_date as ReleaseDate,
                is_active as IsActive,
                created_utc as CreatedUtc,
                updated_utc as UpdatedUtc
            FROM expansions
            WHERE is_active = 1
            ORDER BY name;";

        var result = await conn.QueryAsync<Expansion>(sql);
        return result.AsList();
    }

    public async Task<Expansion?> GetByNameAsync(string name)
    {
        using var conn = CreateConnection();
        const string sql = @"
            SELECT 
                id as Id,
                name as Name,
                code as Code,
                release_date as ReleaseDate,
                is_active as IsActive,
                created_utc as CreatedUtc,
                updated_utc as UpdatedUtc
            FROM expansions
            WHERE LOWER(name) = LOWER(@name)
            LIMIT 1;";

        return await conn.QueryFirstOrDefaultAsync<Expansion>(sql, new { name });
    }

    public async Task<Expansion> CreateAsync(Expansion expansion)
    {
        using var conn = CreateConnection();
        expansion.CreatedUtc = DateTime.UtcNow;
        expansion.UpdatedUtc = DateTime.UtcNow;

        const string sql = @"
            INSERT INTO expansions (name, code, release_date, is_active, created_utc, updated_utc)
            VALUES (@Name, @Code, @ReleaseDate, @IsActive, @CreatedUtc, @UpdatedUtc);
            
            SELECT last_insert_rowid();";

        var id = await conn.ExecuteScalarAsync<long>(sql, expansion);
        expansion.Id = id;
        return expansion;
    }
}