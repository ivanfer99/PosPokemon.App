using System;
using System.Threading.Tasks;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using PosPokemon.App.Repositories;

namespace PosPokemon.App.ViewModels;

public sealed partial class SettingsViewModel : ObservableObject
{
    private readonly SettingsRepository _repo;

    public event Action? BackToDashboardRequested;

    // ✅ CONFIGURACIÓN GENERAL
    [ObservableProperty] private string _currencySymbol = "S/";
    [ObservableProperty] private int _lowStockThreshold = 5;
    [ObservableProperty] private string _defaultSaleNote = "";

    // ✅ CONFIGURACIÓN DE TIENDA
    [ObservableProperty] private string _storeName = "POS POKÉMON TCG";
    [ObservableProperty] private string _storeAddress = "Lima, Perú";
    [ObservableProperty] private string _storePhone = "";
    [ObservableProperty] private string _storeRuc = "";
    [ObservableProperty] private string _storeLogoPath = "";

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

        // Cargar configuración general
        CurrencySymbol = await _repo.GetAsync("currency.symbol") ?? CurrencySymbol;
        DefaultSaleNote = await _repo.GetAsync("sale.default_note") ?? DefaultSaleNote;

        var lowStock = await _repo.GetAsync("stock.low_threshold");
        if (int.TryParse(lowStock, out var t)) LowStockThreshold = t;

        // ✅ CARGAR CONFIGURACIÓN DE TIENDA
        StoreName = await _repo.GetAsync("store.name") ?? StoreName;
        StoreAddress = await _repo.GetAsync("store.address") ?? StoreAddress;
        StorePhone = await _repo.GetAsync("store.phone") ?? StorePhone;
        StoreRuc = await _repo.GetAsync("store.ruc") ?? StoreRuc;
        StoreLogoPath = await _repo.GetAsync("store.logo_path") ?? StoreLogoPath;
    }

    [RelayCommand]
    public async Task SaveAsync()
    {
        StatusMessage = "";

        // Validaciones
        if (string.IsNullOrWhiteSpace(StoreName))
        {
            StatusMessage = "❌ El nombre de la tienda no puede estar vacío.";
            return;
        }

        if (LowStockThreshold < 0) LowStockThreshold = 0;

        // Guardar configuración general
        await _repo.SetAsync("currency.symbol", CurrencySymbol.Trim());
        await _repo.SetAsync("stock.low_threshold", LowStockThreshold.ToString());
        await _repo.SetAsync("sale.default_note", DefaultSaleNote ?? "");

        // ✅ GUARDAR CONFIGURACIÓN DE TIENDA
        await _repo.SetAsync("store.name", StoreName.Trim());
        await _repo.SetAsync("store.address", StoreAddress.Trim());
        await _repo.SetAsync("store.phone", StorePhone.Trim());
        await _repo.SetAsync("store.ruc", StoreRuc.Trim());
        await _repo.SetAsync("store.logo_path", StoreLogoPath.Trim());

        StatusMessage = "✅ Configuración guardada correctamente.";
    }

    // ✅ NUEVO: Seleccionar logo
    [RelayCommand]
    private void SelectLogo()
    {
        var dialog = new OpenFileDialog
        {
            Title = "Seleccionar Logo",
            Filter = "Imágenes|*.png;*.jpg;*.jpeg;*.bmp|Todos los archivos|*.*",
            Multiselect = false
        };

        if (dialog.ShowDialog() == true)
        {
            StoreLogoPath = dialog.FileName;
            StatusMessage = "✅ Logo seleccionado. No olvides guardar los cambios.";
        }
    }

    // ✅ NUEVO: Limpiar logo
    [RelayCommand]
    private void ClearLogo()
    {
        StoreLogoPath = "";
        StatusMessage = "✅ Logo eliminado. No olvides guardar los cambios.";
    }
}