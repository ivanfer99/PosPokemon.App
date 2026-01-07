using System.Windows;
using System.Windows.Controls;
using PosPokemon.App.ViewModels;

namespace PosPokemon.App.Views;

public partial class PaymentDialog : Window
{
    public PaymentDialogViewModel ViewModel { get; }

    public PaymentDialog(decimal totalToPay)
    {
        InitializeComponent();

        ViewModel = new PaymentDialogViewModel(totalToPay);
        DataContext = ViewModel;

        ViewModel.PaymentConfirmed += () =>
        {
            DialogResult = true;
            Close();
        };

        ViewModel.PaymentCancelled += () =>
        {
            DialogResult = false;
            Close();
        };
    }

    // ✅ Manejador para botones de montos rápidos
    private void OnQuickAmount_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.Tag is string tagValue)
        {
            if (decimal.TryParse(tagValue, out var amount))
            {
                ViewModel.AmountReceived = amount;
            }
        }
    }

    // ✅ Manejador para botón "Exacto"
    private void OnQuickAmountExact_Click(object sender, RoutedEventArgs e)
    {
        ViewModel.AmountReceived = ViewModel.TotalToPay;
    }
}