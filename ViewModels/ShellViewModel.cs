using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PosPokemon.App.Data;
using PosPokemon.App.Models;
using PosPokemon.App.Repositories;
using PosPokemon.App.Views;

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

    // Navegación (contenido central)
    [ObservableProperty] private UserControl? _currentView;

    // DB y repos
    private readonly Db _db;
    private readonly ProductRepository _productRepo;
    private readonly SaleRepository _saleRepo;
    private readonly DiscountCampaignRepository _campaignRepo;
    private readonly CustomerRepository _customerRepo;  // ✅ AGREGAR ESTE CAMPO
    private readonly User _currentUser;

    public ShellViewModel(User user)
    {
        CurrentUser = user;
        _currentUser = user;

        // Leer appsettings.json para ubicar la DB
        var settingsPath = Path.Combine(System.AppDomain.CurrentDomain.BaseDirectory, "appsettings.json");
        var json = File.ReadAllText(settingsPath);
        using var doc = JsonDocument.Parse(json);

        var dbFile = doc.RootElement
            .GetProperty("Database")
            .GetProperty("FileName")
            .GetString() ?? "pospokemon.sqlite";

        // ✅ Guardar Db como campo para reutilizarlo
        _db = new Db(dbFile);

        _productRepo = new ProductRepository(_db);
        _saleRepo = new SaleRepository(_db);
        _campaignRepo = new DiscountCampaignRepository(_db);
        _customerRepo = new CustomerRepository(_db);  // ✅ INICIALIZAR REPOSITORIO DE CLIENTES

        LoadDashboardStats();
    }

    private async void LoadDashboardStats()
    {
        try
        {
            TotalProducts = await _productRepo.GetTotalCountAsync();
            LowStockItems = await _productRepo.GetLowStockCountAsync(5);

            // Ventas de hoy
            var todayStart = System.DateTime.Today.ToUniversalTime().ToString("O");
            var todayEnd = System.DateTime.Today.AddDays(1).ToUniversalTime().ToString("O");
            SalesToday = await _saleRepo.GetTotalSalesByDateRangeAsync(todayStart, todayEnd);

            // Ventas del mes
            var monthStart = new System.DateTime(System.DateTime.Today.Year, System.DateTime.Today.Month, 1)
                .ToUniversalTime().ToString("O");

            var monthEnd = System.DateTime.Today.AddDays(1).ToUniversalTime().ToString("O");
            SalesThisMonth = await _saleRepo.GetTotalSalesByDateRangeAsync(monthStart, monthEnd);
        }
        catch
        {
            // opcional: log
        }
    }

    // ======================
    // NAVEGACIÓN
    // ======================

    [RelayCommand]
    private void OpenSales()
    {
        // ✅ PASAR TODOS LOS REPOSITORIOS AL CONSTRUCTOR
        var viewModel = new SalesViewModel(_productRepo, _saleRepo, _campaignRepo, _customerRepo, _currentUser);

        // volver al dashboard
        viewModel.BackToDashboardRequested += () => CurrentView = null;

        var view = new SalesView { DataContext = viewModel };
        CurrentView = view;
    }

    [RelayCommand]
    private void OpenInventory()
    {
        var viewModel = new InventoryViewModel(_productRepo);

        // volver al dashboard
        viewModel.BackToDashboardRequested += () => CurrentView = null;

        var view = new InventoryView { DataContext = viewModel };
        CurrentView = view;
    }

    [RelayCommand]
    private void OpenHistory()
    {
        if (!IsAdmin) return;

        var viewModel = new HistoryViewModel(_saleRepo);

        // volver al dashboard
        viewModel.BackToDashboardRequested += () => CurrentView = null;

        var view = new HistoryView { DataContext = viewModel };
        CurrentView = view;
    }

    // ✅ REPORTES
    [RelayCommand]
    private async Task OpenReports()
    {
        if (!IsAdmin) return;

        var reportsRepo = new ReportsRepository(_db);
        var viewModel = new ReportsViewModel(reportsRepo);

        // volver al dashboard
        viewModel.BackToDashboardRequested += () => CurrentView = null;

        var view = new ReportsView { DataContext = viewModel };
        CurrentView = view;

        // Carga automática
        try
        {
            await viewModel.LoadAsync();
        }
        catch (System.Exception ex)
        {
            MessageBox.Show("Error cargando reportes:\n" + ex.Message, "Error");
        }
    }

    [RelayCommand]
    private async Task OpenUsers()
    {
        if (!IsAdmin) return;

        var userRepo = new UserRepository(_db);

        var vm = new UsersViewModel(userRepo);
        vm.BackToDashboardRequested += () => CurrentView = null;

        var view = new UsersView { DataContext = vm };
        CurrentView = view;

        await vm.LoadAsync();
    }

    [RelayCommand]
    private async Task OpenSettings()
    {
        if (!IsAdmin) return;

        var repo = new SettingsRepository(_db);
        var vm = new SettingsViewModel(repo);

        vm.BackToDashboardRequested += () => CurrentView = null;

        var view = new SettingsView { DataContext = vm };
        CurrentView = view;

        await vm.LoadAsync();
    }

    // ✅ CAMPAÑAS DE DESCUENTO
    [RelayCommand]
    private async Task OpenDiscountCampaigns()
    {
        if (!IsAdmin) return;

        var vm = new DiscountCampaignsViewModel(_campaignRepo, _productRepo, _currentUser);
        vm.BackToDashboardRequested += () => CurrentView = null;

        var view = new DiscountCampaignsView { DataContext = vm };
        CurrentView = view;

        await vm.LoadCampaignsAsync();
    }

    // ✅ NUEVO: GESTIÓN DE CLIENTES
    [RelayCommand]
    private async Task OpenCustomers()
    {
        if (!IsAdmin) return;

        var vm = new CustomersViewModel(_customerRepo);
        vm.BackToDashboardRequested += () => CurrentView = null;

        var view = new CustomersView { DataContext = vm };
        CurrentView = view;

        await vm.LoadAsync();
    }

    // ======================
    // LOGOUT
    // ======================
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
            // Cerrar Shell
            Application.Current.Windows
                .OfType<ShellWindow>()
                .FirstOrDefault()
                ?.Close();

            // Abrir Login
            var loginWindow = new LoginWindow();
            loginWindow.Show();
        }
    }
}