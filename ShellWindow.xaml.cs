using System.Windows;
using System.Windows.Input;
using PosPokemon.App.ViewModels;

namespace PosPokemon.App;

public partial class ShellWindow : Window
{
    private ShellViewModel ViewModel => (ShellViewModel)DataContext;

    public ShellWindow()
    {
        InitializeComponent();
    }

    private void Card_Sales_Click(object sender, MouseButtonEventArgs e)
    {
        ViewModel.OpenSalesCommand.Execute(null);
    }

    private void Card_Inventory_Click(object sender, MouseButtonEventArgs e)
    {
        ViewModel.OpenInventoryCommand.Execute(null);
    }

    private void Card_History_Click(object sender, MouseButtonEventArgs e)
    {
        ViewModel.OpenHistoryCommand.Execute(null);
    }

    private void Card_Reports_Click(object sender, MouseButtonEventArgs e)
    {
        ViewModel.OpenReportsCommand.Execute(null);
    }

    private void Card_Users_Click(object sender, MouseButtonEventArgs e)
    {
        ViewModel.OpenUsersCommand.Execute(null);
    }

    private void Card_Settings_Click(object sender, MouseButtonEventArgs e)
    {
        ViewModel.OpenSettingsCommand.Execute(null);
    }
    // ✅ NUEVO: Navegación a Descuentos
    private void Card_Discounts_Click(object sender, MouseButtonEventArgs e)
    {
        if (DataContext is ShellViewModel vm)
        {
            vm.OpenDiscountCampaignsCommand.Execute(null);
        }
    }
    private async void Card_Customers_Click(object sender, MouseButtonEventArgs e)
    {
        if (DataContext is ShellViewModel vm)
        {
            await vm.OpenCustomersCommand.ExecuteAsync(null);
        }
    }
}