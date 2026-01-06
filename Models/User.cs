namespace PosPokemon.App.Models;

public sealed class User
{
    public long Id { get; set; }
    public string Username { get; set; } = "";
    public string PasswordHash { get; set; } = "";
    public string Role { get; set; } = "SELLER"; // ADMIN / SELLER
    public int IsActive { get; set; } = 1;
    public string CreatedUtc { get; set; } = "";
}
