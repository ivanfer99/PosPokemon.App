using System.Threading.Tasks;
using PosPokemon.App.Models;
using PosPokemon.App.Repositories;

namespace PosPokemon.App.Services;

public sealed class AuthService
{
    private readonly UserRepository _userRepo;
    private readonly PasswordHasher _passwordHasher;

    public AuthService(UserRepository userRepo)
    {
        _userRepo = userRepo;
        _passwordHasher = new PasswordHasher();
    }

    public async Task<User?> LoginAsync(string username, string password)
    {
        var user = await _userRepo.GetByUsernameAsync(username);

        if (user == null || user.IsActive == 0)
            return null;

        if (!_passwordHasher.Verify(password, user.PasswordHash))
            return null;

        return user;
    }
}