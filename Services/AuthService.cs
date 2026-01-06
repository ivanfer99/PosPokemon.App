using System.Threading.Tasks;
using PosPokemon.App.Models;
using PosPokemon.App.Repositories;

namespace PosPokemon.App.Services;

public sealed class AuthService
{
    private readonly UserRepository _users;

    public AuthService(UserRepository users) => _users = users;

    public async Task<User?> LoginAsync(string username, string password)
    {
        var u = await _users.GetByUsernameAsync(username.Trim());
        if (u == null) return null;
        if (u.IsActive != 1) return null;

        var ok = PasswordHasher.Verify(password, u.PasswordHash);
        return ok ? u : null;
    }
}
