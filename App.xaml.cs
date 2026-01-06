using System;
using System.IO;
using System.Windows;
using Dapper;
using PosPokemon.App.Data;
using PosPokemon.App.Models;
using PosPokemon.App.Repositories;
using PosPokemon.App.Services;
using PosPokemon.App.ViewModels;
using PosPokemon.App.Views;

namespace PosPokemon.App
{
    public partial class App : Application
    {
        protected override async void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // Ruta REAL del SQLite que usa la app
            var dbFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "pospokemon.sqlite");

            try
            {
                var db = new Db(dbFile);

                db.InitSchema();
                await db.SeedAsync();

                await EnsureAdminAsync(db);

                var userRepo = new UserRepository(db);
                var authService = new AuthService(userRepo);

                var loginVm = new LoginViewModel(authService);

                var loginWindow = new LoginWindow
                {
                    DataContext = loginVm
                };

                loginVm.LoginSucceeded += (User user) =>
                {
                    var shell = new ShellWindow
                    {
                        DataContext = new ShellViewModel(user)
                    };

                    shell.Show();
                    loginWindow.Close();
                };

                loginWindow.Show();

                // ✅ Opcional: mostrar la ruta exacta de la BD (solo para debug)
                // Comenta esta línea cuando ya todo funcione
                MessageBox.Show($"BD en uso:\n{dbFile}", "Debug DB Path");
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Error iniciando la aplicación.\n\nBD en uso:\n{dbFile}\n\nDetalle:\n{ex}",
                    "PosPokemon.App",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error
                );

                Shutdown();
            }
        }

        private static async System.Threading.Tasks.Task EnsureAdminAsync(Db db)
        {
            using var conn = db.OpenConnection();

            var hasher = new PasswordHasher();
            var adminHash = hasher.Hash("admin");

            // Si existe admin -> actualizar hash y activar
            // Si no existe -> insertarlo
            var exists = await conn.ExecuteScalarAsync<int>(
                "SELECT COUNT(*) FROM users WHERE username = 'admin';"
            );

            if (exists == 0)
            {
                const string insertSql = @"
INSERT INTO users (username, password_hash, role, is_active, created_utc)
VALUES (@Username, @PasswordHash, @Role, @IsActive, @CreatedUtc);
";
                await conn.ExecuteAsync(insertSql, new
                {
                    Username = "admin",
                    PasswordHash = adminHash,
                    Role = "ADMIN",
                    IsActive = 1,
                    CreatedUtc = DateTime.UtcNow.ToString("O")
                });
            }
            else
            {
                const string updateSql = @"
UPDATE users
SET password_hash = @PasswordHash,
    is_active = 1,
    role = 'ADMIN'
WHERE username = 'admin';
";
                await conn.ExecuteAsync(updateSql, new
                {
                    PasswordHash = adminHash
                });
            }
        }
    }
}
