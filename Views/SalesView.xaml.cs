using System.Windows;
using System.Windows.Controls;
using PosPokemon.App.ViewModels;

namespace PosPokemon.App.Views;

public partial class SalesView : UserControl
{
    public SalesView()
    {
        InitializeComponent();
    }

    // ✅ Método para alternar el panel de descuento
    private void OnToggleDiscountPanel(object sender, RoutedEventArgs e)
    {
        if (DiscountPanel.Visibility == Visibility.Collapsed)
        {
            DiscountPanel.Visibility = Visibility.Visible;
            BtnToggleDiscount.Content = "➖ Ocultar Descuento";
        }
        else
        {
            DiscountPanel.Visibility = Visibility.Collapsed;
            BtnToggleDiscount.Content = "➕ Aplicar Descuento";
        }
    }

    // ✅ Método para cambiar el tipo de descuento
    private void OnDiscountTypeChanged(object sender, RoutedEventArgs e)
    {
        if (DataContext is not SalesViewModel vm) return;

        if (RbPercentage.IsChecked == true)
        {
            vm.DiscountType = "Porcentaje";
        }
        else if (RbFixedAmount.IsChecked == true)
        {
            vm.DiscountType = "Monto";
        }
    }
}