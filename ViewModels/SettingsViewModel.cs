using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PosPokemon.App.Repositories;
using System;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;

namespace PosPokemon.App.ViewModels;

public sealed partial class SettingsViewModel : ObservableObject
{
    private readonly SettingsRepository _repo;

    public event Action? BackToDashboardRequested;

    [ObservableProperty] private string _storeName = "Cartón Fino Perú";
    [ObservableProperty] private string _currencySymbol = "S/";
    [ObservableProperty] private int _lowStockThreshold = 5;
    [ObservableProperty] private string _defaultSaleNote = "";

    [ObservableProperty] private string _statusMessage = "";

    public SettingsViewModel(SettingsRepository repo)
    {
        _repo = repo;
    }

    [RelayCommand]
    private void BackToDashboard()
    {
        BackToDashboardRequested?.Invoke();
    }

    [RelayCommand]
    public async Task LoadAsync()
    {
        StatusMessage = "";

        StoreName = await _repo.GetAsync("store.name") ?? StoreName;
        CurrencySymbol = await _repo.GetAsync("currency.symbol") ?? CurrencySymbol;

        var lowStock = await _repo.GetAsync("stock.low_threshold");
        if (int.TryParse(lowStock, out var t)) LowStockThreshold = t;

        DefaultSaleNote = await _repo.GetAsync("sale.default_note") ?? DefaultSaleNote;
    }

    [RelayCommand]
    public async Task SaveAsync()
    {
        StatusMessage = "";

        if (string.IsNullOrWhiteSpace(StoreName))
        {
            StatusMessage = "❌ El nombre de la tienda no puede estar vacío.";
            return;
        }

        if (LowStockThreshold < 0) LowStockThreshold = 0;

        await _repo.SetAsync("store.name", StoreName.Trim());
        await _repo.SetAsync("currency.symbol", CurrencySymbol.Trim());
        await _repo.SetAsync("stock.low_threshold", LowStockThreshold.ToString());
        await _repo.SetAsync("sale.default_note", DefaultSaleNote ?? "");

        StatusMessage = "✅ Configuración guardada correctamente.";
    }
}
