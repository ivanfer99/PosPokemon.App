using System;
using System.Data;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using Dapper;
using PosPokemon.App.Services;

namespace PosPokemon.App.Data;

public sealed class Db
{
    private readonly string _connectionString;

    public Db(string dbFile)
    {
        _connectionString = $"Data Source={dbFile}";
    }

    public IDbConnection OpenConnection() => new SqliteConnection(_connectionString);

    public void InitSchema()
    {
        var schemaPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data", "Schema.sqlite.sql");
        if (!File.Exists(schemaPath))
            throw new FileNotFoundException($"Schema file not found: {schemaPath}");

        var sql = File.ReadAllText(schemaPath);

        using var conn = OpenConnection();
        conn.Execute(sql);
    }

    public async Task SeedAsync()
    {
        using var conn = OpenConnection();

        const string checkSql = "SELECT COUNT(*) FROM users WHERE username = 'admin'";
        var exists = await conn.ExecuteScalarAsync<int>(checkSql);

        if (exists == 0)
        {
            var passwordHasher = new PasswordHasher();
            var adminHash = passwordHasher.Hash("admin");
            var sellerHash = passwordHasher.Hash("seller");

            const string insertSql = "INSERT INTO users (username, password_hash, role, is_active, created_utc) VALUES (@Username, @PasswordHash, @Role, @IsActive, @CreatedUtc)";

            await conn.ExecuteAsync(insertSql, new
            {
                Username = "admin",
                PasswordHash = adminHash,
                Role = "ADMIN",
                IsActive = 1,
                CreatedUtc = DateTime.UtcNow.ToString("O")
            });

            await conn.ExecuteAsync(insertSql, new
            {
                Username = "seller",
                PasswordHash = sellerHash,
                Role = "SELLER",
                IsActive = 1,
                CreatedUtc = DateTime.UtcNow.ToString("O")
            });
        }
    }
}