using Dapper;
using PosPokemon.App.Data;
using PosPokemon.App.Models;
using System;
using System.Threading.Tasks;
using System.Windows;

namespace PosPokemon.App.Repositories;

public sealed class UserRepository
{
    private readonly Db _db;
    public UserRepository(Db db) => _db = db;

    public async Task<User?> GetByUsernameAsync(string username)
    {
        const string sql = @"SELECT * FROM users WHERE username = @username LIMIT 1;";
        using var conn = _db.OpenConnection();
        return await conn.QueryFirstOrDefaultAsync<User>(sql, new { username });
    }

    public async Task<long> CreateAsync(string username, string passwordHash, string role)
    {
        const string sql = @"
INSERT INTO users (username, password_hash, role, is_active, created_utc)
VALUES (@username, @passwordHash, @role, 1, @now);
SELECT last_insert_rowid();";

        using var conn = _db.OpenConnection();
        return await conn.ExecuteScalarAsync<long>(sql, new
        {
            username,
            passwordHash,
            role,
            now = DateTime.UtcNow.ToString("O")
        });
    }

    public async Task EnsureDefaultAdminAsync()
    {
        var admin = await GetByUsernameAsync("admin");
        var hash = Services.PasswordHasher.Hash("admin");

        if (admin == null)
        {
            await CreateAsync("admin", hash, "ADMIN");
        }
        else
        {
            const string sqlUpdate = @"
UPDATE users
SET password_hash = @hash
WHERE lower(username) = 'admin';";

            using var conn = _db.OpenConnection();
            await conn.ExecuteAsync(sqlUpdate, new { hash });
        }

        // Verificación inmediata (infalible)
        var reread = await GetByUsernameAsync("admin");
        var ok = reread != null && Services.PasswordHasher.Verify("admin", reread.PasswordHash);

        if (!ok)
        {
            System.Windows.MessageBox.Show(
                "DEBUG: El hash guardado NO valida.\n\n" +
                $"HASH GUARDADO:\n{reread?.PasswordHash}\n\n" +
                $"LEN: {reread?.PasswordHash?.Length}"
            );
        }
    }





}
