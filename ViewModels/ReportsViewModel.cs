using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PosPokemon.App.Repositories;
using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;

namespace PosPokemon.App.ViewModels;

public sealed partial class ReportsViewModel : ObservableObject
{
    private readonly ReportsRepository _repo;

    // Igual que en tus otros módulos
    public event Action? BackToDashboardRequested;

    [ObservableProperty] private DateTime _fromDate = DateTime.Today;
    [ObservableProperty] private DateTime _toDate = DateTime.Today.AddDays(1);

    [ObservableProperty] private int _salesCount;
    [ObservableProperty] private decimal _totalSales;
    [ObservableProperty] private decimal _avgTicket;

    public ObservableCollection<SalesByUserRow> SalesByUser { get; } = new();
    public ObservableCollection<TopProductRow> TopProducts { get; } = new();

    public ReportsViewModel(ReportsRepository repo)
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
        // Validación simple
        if (ToDate <= FromDate)
            ToDate = FromDate.AddDays(1);

        var fromUtc = FromDate.ToUniversalTime().ToString("O");
        var toUtc = ToDate.ToUniversalTime().ToString("O");

        var summary = await _repo.GetSummaryAsync(fromUtc, toUtc);
        SalesCount = summary.SalesCount;
        TotalSales = summary.TotalSales;
        AvgTicket = summary.AvgTicket;

        SalesByUser.Clear();
        foreach (var r in await _repo.GetSalesByUserAsync(fromUtc, toUtc))
            SalesByUser.Add(r);

        TopProducts.Clear();
        foreach (var r in await _repo.GetTopProductsAsync(fromUtc, toUtc))
            TopProducts.Add(r);
    }
}
