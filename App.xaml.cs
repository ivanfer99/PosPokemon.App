using System;
using System.IO;
using System.Text.Json;
using System.Windows;
using Dapper; // 🔴 MUY IMPORTANTE
using PosPokemon.App.Data;
using PosPokemon.App.Repositories;
using PosPokemon.App.Services;
using PosPokemon.App.ViewModels;

namespace PosPokemon.App;

public partial class App : Application
{
    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // 🔴 CLAVE ABSOLUTA:
        // Permite que Dapper mapee password_hash -> PasswordHash
        // set_name -> SetName, created_utc -> CreatedUtc, etc.
        DefaultTypeMap.MatchNamesWithUnderscores = true;

        // Leer appsettings.json
        var settingsPath = Path.Combine(
            AppDomain.CurrentDomain.BaseDirectory,
            "appsettings.json"
        );

        if (!File.Exists(settingsPath))
        {
            MessageBox.Show("No se encontró appsettings.json");
            Shutdown();
            return;
        }

        var json = File.ReadAllText(settingsPath);
        using var doc = JsonDocument.Parse(json);

        var dbFile = doc.RootElement
                        .GetProperty("Database")
                        .GetProperty("FileName")
                        .GetString()
                     ?? "pospokemon.sqlite";

        // Inicializar base de datos
        var db = new Db(dbFile);
        db.EnsureCreated();

        // Repositorios / Servicios
        var userRepo = new UserRepository(db);

        // Crear o resetear admin (modo DEV)
        await userRepo.EnsureDefaultAdminAsync();

        var authService = new AuthService(userRepo);

        // ViewModel + Window de Login
        var loginVm = new LoginViewModel(authService);
        var loginWin = new LoginWindow
        {
            DataContext = loginVm
        };

        // Evento cuando el login es exitoso
        loginVm.LoginSucceeded += user =>
        {
            var shellVm = new ShellViewModel(user);
            var shellWin = new ShellWindow
            {
                DataContext = shellVm
            };

            shellWin.Show();
            loginWin.Close();
        };

        // Mostrar Login
        loginWin.Show();
    }
}
