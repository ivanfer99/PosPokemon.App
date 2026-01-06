using System.Threading.Tasks;
using Dapper;
using PosPokemon.App.Data;
using PosPokemon.App.Models;

namespace PosPokemon.App.Repositories
{
    public sealed class UserRepository
    {
        private readonly Db _db;

        public UserRepository(Db db) => _db = db;

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
    }
}
