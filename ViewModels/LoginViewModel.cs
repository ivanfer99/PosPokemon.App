using System;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PosPokemon.App.Models;
using PosPokemon.App.Services;

namespace PosPokemon.App.ViewModels
{
    public sealed partial class LoginViewModel : ObservableObject
    {
        private readonly AuthService _auth;

        [ObservableProperty]
        private string _username = "";

        // OJO: PasswordBox no bindea directo. Se setea desde LoginWindow.xaml.cs
        [ObservableProperty]
        private string _password = "";

        [ObservableProperty]
        private string _errorMessage = "";

        public event Action<User>? LoginSucceeded;

        public LoginViewModel(AuthService auth)
        {
            _auth = auth;
        }

        [RelayCommand]
        private async Task LoginAsync()
        {
            try
            {
                ErrorMessage = "";

                var u = (Username ?? "").Trim();
                var p = (Password ?? "").Trim();

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

                // Login OK
                LoginSucceeded?.Invoke(user);
            }
            catch (Exception ex)
            {
                // Evita crashear UI si algo falla (db, conexión, schema, etc.)
                ErrorMessage = "Ocurrió un error al iniciar sesión: " + ex.Message;
            }
        }

        [RelayCommand]
        private void ClearError()
        {
            ErrorMessage = "";
        }
    }
}
