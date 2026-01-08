using System.Windows;
using System.Windows.Controls;

namespace PosPokemon.App.Views;

public partial class InventoryView : UserControl
{
    public InventoryView()
    {
        InitializeComponent();
    }

    // ✅ NUEVO: Evento para abrir la ventana de importación
    private void OnImportProducts(object sender, RoutedEventArgs e)
    {
        var importWindow = new ImportProductsWindow();
        importWindow.ShowDialog();

        // ✅ Recargar inventario después de la importación
        if (DataContext is ViewModels.InventoryViewModel vm)
        {
            _ = vm.SearchProductsCommand.ExecuteAsync(null);
        }
    }
}