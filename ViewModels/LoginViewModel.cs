using System;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PosPokemon.App.Models;
using PosPokemon.App.Services;

namespace PosPokemon.App.ViewModels;

public partial class LoginViewModel : ObservableObject
{
    private readonly AuthService _auth;

    [ObservableProperty] private string _username = "";
    [ObservableProperty] private string _password = "";
    [ObservableProperty] private string _errorMessage = "";

    public event Action<User>? LoginSucceeded;

    public LoginViewModel(AuthService auth) => _auth = auth;

    [RelayCommand]
    private async Task LoginAsync()
    {
        ErrorMessage = "";

        var u = Username?.Trim() ?? "";
        var p = Password ?? "";

        if (string.IsNullOrWhiteSpace(u) || string.IsNullOrWhiteSpace(p))
        {
            ErrorMessage = "Ingresa usuario y contraseña.";
            return;
        }

        var user = await _auth.LoginAsync(u, p);
        if (user == null)
        {
            ErrorMessage = "Usuario o contraseña incorrectos.";
            return;
        }

        LoginSucceeded?.Invoke(user);
    }
}
