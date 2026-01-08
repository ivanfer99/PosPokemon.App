using System;
using System.IO;
using System.Windows;
using Microsoft.Win32;
using PosPokemon.App.Services;
using PosPokemon.App.Repositories;
using PosPokemon.App.Data;

namespace PosPokemon.App.Views;

public partial class ImportProductsWindow : Window
{
    private readonly ExcelImportService _importService;
    private string? _selectedFilePath;

    public ImportProductsWindow()
    {
        InitializeComponent();

        // ✅ INICIALIZAR SERVICIO
        var dbFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "pospokemon.sqlite");
        var db = new Db(dbFile);
        var connectionString = $"Data Source={dbFile}";

        var productRepo = new ProductRepository(db);
        var categoryRepo = new CategoryRepository(connectionString);
        var expansionRepo = new ExpansionRepository(connectionString);

        _importService = new ExcelImportService(productRepo, categoryRepo, expansionRepo);
    }

    private void OnDownloadTemplate(object sender, RoutedEventArgs e)
    {
        try
        {
            var dialog = new SaveFileDialog
            {
                FileName = "Plantilla_Productos_Pokemon.xlsx",
                Filter = "Archivos Excel|*.xlsx",
                DefaultExt = ".xlsx"
            };

            if (dialog.ShowDialog() == true)
            {
                _importService.ExportTemplateAsync(dialog.FileName);

                MessageBox.Show(
                    $"✅ Plantilla generada exitosamente:\n\n{dialog.FileName}",
                    "Plantilla Generada",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information
                );

                // Abrir carpeta
                System.Diagnostics.Process.Start("explorer.exe", $"/select,\"{dialog.FileName}\"");
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"❌ Error al generar plantilla:\n{ex.Message}",
                "Error",
                MessageBoxButton.OK,
                MessageBoxImage.Error
            );
        }
    }

    private void OnSelectFile(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog
        {
            Filter = "Archivos Excel|*.xlsx;*.xls",
            Title = "Seleccionar Archivo Excel"
        };

        if (dialog.ShowDialog() == true)
        {
            _selectedFilePath = dialog.FileName;
            TxtFilePath.Text = _selectedFilePath;
            TxtResults.Text = "✅ Archivo seleccionado. Haz clic en 'Importar Productos' para continuar.";
        }
    }

    private async void OnImport(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(_selectedFilePath))
        {
            MessageBox.Show(
                "Por favor selecciona un archivo Excel primero.",
                "Archivo Requerido",
                MessageBoxButton.OK,
                MessageBoxImage.Warning
            );
            return;
        }

        try
        {
            BtnImport.IsEnabled = false;
            TxtResults.Text = "⏳ Importando productos, por favor espera...\n";

            // Ejecutar importación
            var result = await _importService.ImportProductsFromExcelAsync(_selectedFilePath);

            // Mostrar resultados
            var output = $@"
✅ IMPORTACIÓN COMPLETADA

{result.Summary}

📊 RESUMEN DETALLADO:
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
✅ Exitosos: {result.SuccessCount} productos
❌ Fallidos: {result.FailureCount} productos
⊘ Omitidos: {result.SkippedCount} productos (duplicados)
📁 Total de filas procesadas: {result.TotalRows}
";

            if (result.NewCategoriesCreated > 0)
            {
                output += $"\n⭐ NUEVAS CATEGORÍAS CREADAS ({result.NewCategoriesCreated}):\n";
                foreach (var cat in result.CreatedCategories)
                    output += $"   • {cat}\n";
            }

            if (result.NewExpansionsCreated > 0)
            {
                output += $"\n⭐ NUEVAS EXPANSIONES CREADAS ({result.NewExpansionsCreated}):\n";
                foreach (var exp in result.CreatedExpansions)
                    output += $"   • {exp}\n";
            }

            if (result.HasErrors)
            {
                output += $"\n❌ ERRORES ({result.Errors.Count}):\n";
                output += "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━\n";

                foreach (var error in result.Errors)
                {
                    output += $"\n📍 Fila {error.Row}: {error.Code} - {error.ProductName}\n";
                    output += $"   Tipo: {error.ErrorType}\n";
                    output += $"   Error: {error.ErrorMessage}\n";
                }
            }

            TxtResults.Text = output;

            if (!result.HasErrors)
            {
                MessageBox.Show(
                    $"✅ Importación completada exitosamente!\n\n{result.Summary}",
                    "Éxito",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information
                );
            }
            else
            {
                MessageBox.Show(
                    $"⚠️ Importación completada con {result.Errors.Count} error(es).\n\nRevisa los detalles en la ventana.",
                    "Importación con Errores",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning
                );
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"❌ Error crítico durante la importación:\n\n{ex.Message}",
                "Error",
                MessageBoxButton.OK,
                MessageBoxImage.Error
            );

            TxtResults.Text = $"❌ ERROR CRÍTICO:\n{ex.Message}\n\n{ex.StackTrace}";
        }
        finally
        {
            BtnImport.IsEnabled = true;
        }
    }

    private void OnClose(object sender, RoutedEventArgs e)
    {
        Close();
    }
}