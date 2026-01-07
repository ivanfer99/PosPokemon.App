using System.Windows;
using System.Windows.Controls;

namespace PosPokemon.App.Views;

public partial class SalesView : UserControl
{
    public SalesView()
    {
        InitializeComponent();
    }

    private void OnToggleDiscountPanel(object sender, RoutedEventArgs e)
    {
        if (DiscountPanel.Visibility == Visibility.Collapsed)
        {
            DiscountPanel.Visibility = Visibility.Visible;
            BtnToggleDiscount.Content = "➖ Ocultar Descuento";
            BtnToggleDiscount.Background = new System.Windows.Media.SolidColorBrush(
                (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#E91E63"));
        }
        else
        {
            DiscountPanel.Visibility = Visibility.Collapsed;
            BtnToggleDiscount.Content = "➕ Agregar Descuento";
            BtnToggleDiscount.Background = new System.Windows.Media.SolidColorBrush(
                (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#9C27B0"));
        }
    }
}