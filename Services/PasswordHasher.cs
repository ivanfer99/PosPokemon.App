using System;
using System.Security.Cryptography;
using System.Text;

namespace PosPokemon.App.Services;

public sealed class PasswordHasher
{
    public string Hash(string password)
    {
        using var sha256 = SHA256.Create();
        var bytes = Encoding.UTF8.GetBytes(password);
        var hash = sha256.ComputeHash(bytes);
        return Convert.ToBase64String(hash);
    }

    public bool Verify(string password, string hash)
    {
        var computed = Hash(password);
        return computed == hash;
    }
}