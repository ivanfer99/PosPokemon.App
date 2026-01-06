using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PosPokemon.App.Models;
using PosPokemon.App.Views;
using PosPokemon.App.Repositories;
using PosPokemon.App.Data;
using System.Windows;
using System.Windows.Controls;
using System.IO;
using System.Text.Json;

namespace PosPokemon.App.ViewModels;

public partial class ShellViewModel : ObservableObject
{
    public User CurrentUser { get; }

    public bool IsAdmin => CurrentUser.Role == "ADMIN";
    public bool IsSeller => CurrentUser.Role == "SELLER";

    [ObservableProperty] private decimal _salesToday = 0;
    [ObservableProperty] private int _totalProducts = 0;
    [ObservableProperty] private int _lowStockItems = 0;
    [ObservableProperty] private decimal _salesThisMonth = 0;

    // Para navegación
    [ObservableProperty] private UserControl? _currentView;

    private readonly ProductRepository _productRepo;
    private readonly SaleRepository _saleRepo;

    public ShellViewModel(User user)
    {
        CurrentUser = user;

        // Inicializar repositorios
        var settingsPath = Path.Combine(System.AppDomain.CurrentDomain.BaseDirectory, "appsettings.json");
        var json = File.ReadAllText(settingsPath);
        using var doc = JsonDocument.Parse(json);
        var dbFile = doc.RootElement.GetProperty("Database").GetProperty("FileName").GetString() ?? "pospokemon.sqlite";

        var db = new Db(dbFile);
        _productRepo = new ProductRepository(db);
        _saleRepo = new SaleRepository(db);

        LoadDashboardStats();
    }

    private async void LoadDashboardStats()
    {
        try
        {
            TotalProducts = await _productRepo.GetTotalCountAsync();
            LowStockItems = await _productRepo.GetLowStockCountAsync(5);
            // TODO: Implementar ventas del día y del mes
        }
        catch { }
    }

    [RelayCommand]
    private void OpenSales()
    {
        var viewModel = new SalesViewModel(_productRepo, _saleRepo);

        // Suscribirse al evento de volver al dashboard
        viewModel.BackToDashboardRequested += () => CurrentView = null;

        var view = new SalesView { DataContext = viewModel };
        CurrentView = view;
    }

    [RelayCommand]
    private void OpenInventory()
    {
        var viewModel = new InventoryViewModel(_productRepo);

        // Suscribirse al evento de volver al dashboard
        viewModel.BackToDashboardRequested += () => CurrentView = null;

        var view = new InventoryView { DataContext = viewModel };
        CurrentView = view;
    }

    [RelayCommand]
    private void OpenHistory()
    {
        if (!IsAdmin) return;
        MessageBox.Show("Módulo de Historial en desarrollo...", "Info");
    }

    [RelayCommand]
    private void OpenReports()
    {
        if (!IsAdmin) return;
        MessageBox.Show("Módulo de Reportes en desarrollo...", "Info");
    }

    [RelayCommand]
    private void OpenUsers()
    {
        if (!IsAdmin) return;
        MessageBox.Show("Módulo de Usuarios en desarrollo...", "Info");
    }

    [RelayCommand]
    private void OpenSettings()
    {
        if (!IsAdmin) return;
        MessageBox.Show("Módulo de Configuración en desarrollo...", "Info");
    }

    [RelayCommand]
    private void Logout()
    {
        var result = MessageBox.Show(
            "¿Estás seguro de que deseas cerrar sesión?",
            "Cerrar Sesión",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question
        );

        if (result == MessageBoxResult.Yes)
        {
            // Cerrar la ventana actual
            Application.Current.Windows
                .OfType<ShellWindow>()
                .FirstOrDefault()
                ?.Close();

            // Abrir LoginWindow
            var loginWindow = new LoginWindow();
            loginWindow.Show();
        }
    }
}