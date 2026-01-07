using System.Collections.Generic;
using System.Threading.Tasks;
using Dapper;
using PosPokemon.App.Data;
using PosPokemon.App.Models;
using PosPokemon.App.Services;

namespace PosPokemon.App.Repositories
{
    public sealed class UserRepository
    {
        private readonly Db _db;

        public UserRepository(Db db) => _db = db;

        // =========================
        // LOGIN
        // =========================
        public async Task<User?> GetByUsernameAsync(string username)
        {
            const string sql = @"
SELECT
    id,
    username,
    password_hash AS PasswordHash,
    role,
    is_active     AS IsActive,
    created_utc   AS CreatedUtc
FROM users
WHERE username = @username
LIMIT 1;
";
            using var conn = _db.OpenConnection();
            return await conn.QueryFirstOrDefaultAsync<User>(sql, new { username });
        }

        public async Task<User?> GetByIdAsync(long id)
        {
            const string sql = @"
SELECT
    id,
    username,
    password_hash AS PasswordHash,
    role,
    is_active     AS IsActive,
    created_utc   AS CreatedUtc
FROM users
WHERE id = @id
LIMIT 1;
";
            using var conn = _db.OpenConnection();
            return await conn.QueryFirstOrDefaultAsync<User>(sql, new { id });
        }

        // =========================
        // ADMIN / USUARIOS
        // =========================
        public async Task<List<User>> GetAllAsync()
        {
            const string sql = @"
SELECT
    id,
    username,
    password_hash AS PasswordHash,
    role,
    is_active     AS IsActive,
    created_utc   AS CreatedUtc
FROM users
ORDER BY id DESC;
";
            using var conn = _db.OpenConnection();
            var rows = await conn.QueryAsync<User>(sql);
            return rows.AsList();
        }

        public async Task<long> CreateAsync(string username, string plainPassword, string role)
        {
            var hasher = new PasswordHasher();
            var hash = hasher.Hash(plainPassword);

            const string sql = @"
INSERT INTO users (username, password_hash, role, is_active, created_utc)
VALUES (@Username, @PasswordHash, @Role, 1, @CreatedUtc);
SELECT last_insert_rowid();
";
            using var conn = _db.OpenConnection();
            return await conn.ExecuteScalarAsync<long>(sql, new
            {
                Username = username.Trim(),
                PasswordHash = hash,
                Role = role,
                CreatedUtc = System.DateTime.UtcNow.ToString("O")
            });
        }

        public async Task SetActiveAsync(long id, int isActive)
        {
            const string sql = "UPDATE users SET is_active = @isActive WHERE id = @id;";
            using var conn = _db.OpenConnection();
            await conn.ExecuteAsync(sql, new { id, isActive });
        }

        public async Task SetRoleAsync(long id, string role)
        {
            const string sql = "UPDATE users SET role = @role WHERE id = @id;";
            using var conn = _db.OpenConnection();
            await conn.ExecuteAsync(sql, new { id, role });
        }

        public async Task ResetPasswordAsync(long id, string newPlainPassword)
        {
            var hasher = new PasswordHasher();
            var hash = hasher.Hash(newPlainPassword);

            const string sql = "UPDATE users SET password_hash = @hash WHERE id = @id;";
            using var conn = _db.OpenConnection();
            await conn.ExecuteAsync(sql, new { id, hash });
        }
    }
}

