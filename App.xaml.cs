using System;
using System.IO;
using System.Text.Json;
using System.Windows;
using PosPokemon.App.Data;

namespace PosPokemon.App;

public partial class App : Application
{
    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        try
        {
            // Leer configuración
            var settingsPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "appsettings.json");

            if (!File.Exists(settingsPath))
            {
                MessageBox.Show(
                    "No se encontró el archivo appsettings.json",
                    "Error de Configuración",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error
                );
                Shutdown();
                return;
            }

            var json = File.ReadAllText(settingsPath);
            using var doc = JsonDocument.Parse(json);
            var dbFile = doc.RootElement.GetProperty("Database").GetProperty("FileName").GetString() ?? "pospokemon.sqlite";

            // Inicializar base de datos
            var db = new Db(dbFile);
            db.InitSchema();
            await db.SeedAsync();

            // Mostrar ventana de login
            var loginWindow = new LoginWindow();
            loginWindow.Show();
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"Error al inicializar la aplicación:\n\n{ex.Message}",
                "Error Fatal",
                MessageBoxButton.OK,
                MessageBoxImage.Error
            );
            Shutdown();
        }
    }
}