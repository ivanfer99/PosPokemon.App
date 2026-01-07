using System;
using System.Threading.Tasks;
using Dapper;
using PosPokemon.App.Data;

namespace PosPokemon.App.Repositories;

public sealed class SettingsRepository
{
    private readonly Db _db;
    public SettingsRepository(Db db) => _db = db;

    public async Task<string?> GetAsync(string key)
    {
        const string sql = "SELECT value FROM app_settings WHERE key = @key LIMIT 1;";
        using var conn = _db.OpenConnection();
        return await conn.ExecuteScalarAsync<string?>(sql, new { key });
    }

    public async Task SetAsync(string key, string value)
    {
        const string sql = @"
INSERT INTO app_settings (key, value, updated_utc)
VALUES (@Key, @Value, @Utc)
ON CONFLICT(key) DO UPDATE SET
  value = excluded.value,
  updated_utc = excluded.updated_utc;
";
        using var conn = _db.OpenConnection();
        await conn.ExecuteAsync(sql, new { Key = key, Value = value, Utc = DateTime.UtcNow.ToString("O") });
    }
}
