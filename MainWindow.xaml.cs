using System.IO;
using System.Text.Json;
using System.Windows;
using PosPokemon.App.Data;
using PosPokemon.App.Repositories;
using PosPokemon.App.Services;
using PosPokemon.App.ViewModels;

namespace PosPokemon.App;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();

        var settingsPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "appsettings.json");
        var json = File.ReadAllText(settingsPath);
        using var doc = JsonDocument.Parse(json);

        var dbFile = doc.RootElement.GetProperty("Database").GetProperty("FileName").GetString() ?? "pospokemon.sqlite";

        var db = new Db(dbFile);
        db.EnsureCreated();

        var state = new PosState();
        var productRepo = new ProductRepository(db);

        DataContext = new MainViewModel(productRepo, state);
    }
}
