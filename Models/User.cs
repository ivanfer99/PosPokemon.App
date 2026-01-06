namespace PosPokemon.App.Models
{
    public sealed class User
    {
        public long Id { get; set; }

        public string Username { get; set; } = "";

        // ⚠️ DEBE LLAMARSE ASÍ para que Dapper mapee `password_hash`
        public string PasswordHash { get; set; } = "";

        public string Role { get; set; } = "";

        // SQLite guarda 0 / 1
        public int IsActive { get; set; }

        // ISO string (Utc)
        public string CreatedUtc { get; set; } = "";
    }
}
