using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PosPokemon.App.Models;
using PosPokemon.App.Repositories;

namespace PosPokemon.App.ViewModels;

public partial class CustomersViewModel : ObservableObject
{
    private readonly CustomerRepository _customerRepo;

    public event Action? BackToDashboardRequested;

    [ObservableProperty] private ObservableCollection<Customer> _customers = new();
    [ObservableProperty] private Customer? _selectedCustomer;
    [ObservableProperty] private string _searchText = "";
    [ObservableProperty] private string _statusMessage = "";

    public CustomersViewModel(CustomerRepository customerRepo)
    {
        _customerRepo = customerRepo;
    }

    [RelayCommand]
    private void BackToDashboard()
    {
        BackToDashboardRequested?.Invoke();
    }

    [RelayCommand]
    public async Task LoadAsync()
    {
        try
        {
            StatusMessage = "Cargando clientes...";
            var customers = await _customerRepo.GetAllAsync();

            Customers.Clear();
            foreach (var customer in customers)
            {
                Customers.Add(customer);
            }

            StatusMessage = $"✅ {Customers.Count} cliente(s) cargado(s).";
        }
        catch (Exception ex)
        {
            StatusMessage = $"❌ Error: {ex.Message}";
        }
    }

    [RelayCommand]
    private async Task SearchCustomers()
    {
        try
        {
            var customers = await _customerRepo.SearchAsync(SearchText);

            Customers.Clear();
            foreach (var customer in customers)
            {
                Customers.Add(customer);
            }

            StatusMessage = $"✅ {Customers.Count} resultado(s).";
        }
        catch (Exception ex)
        {
            StatusMessage = $"❌ Error: {ex.Message}";
        }
    }

    [RelayCommand]
    private void CreateCustomer()
    {
        var dialog = new Views.CustomerFormWindow(null);
        dialog.CustomerSaved += async (customer) =>
        {
            try
            {
                // Verificar si el documento ya existe
                var existing = await _customerRepo.GetByDocumentAsync(customer.DocumentNumber);
                if (existing != null)
                {
                    MessageBox.Show(
                        $"Ya existe un cliente con el documento {customer.DocumentNumber}.",
                        "Documento Duplicado",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning
                    );
                    return;
                }

                await _customerRepo.CreateAsync(customer);
                await LoadAsync();

                MessageBox.Show(
                    $"✅ Cliente '{customer.Name}' creado exitosamente.",
                    "Éxito",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information
                );
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Error al crear cliente:\n{ex.Message}",
                    "Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error
                );
            }
        };

        dialog.ShowDialog();
    }

    [RelayCommand]
    private void EditCustomer(Customer customer)
    {
        if (customer == null) return;

        var dialog = new Views.CustomerFormWindow(customer);
        dialog.CustomerSaved += async (updatedCustomer) =>
        {
            try
            {
                // Verificar si el documento cambió y ya existe
                if (updatedCustomer.DocumentNumber != customer.DocumentNumber)
                {
                    var existing = await _customerRepo.GetByDocumentAsync(updatedCustomer.DocumentNumber);
                    if (existing != null && existing.Id != updatedCustomer.Id)
                    {
                        MessageBox.Show(
                            $"Ya existe otro cliente con el documento {updatedCustomer.DocumentNumber}.",
                            "Documento Duplicado",
                            MessageBoxButton.OK,
                            MessageBoxImage.Warning
                        );
                        return;
                    }
                }

                await _customerRepo.UpdateAsync(updatedCustomer);
                await LoadAsync();

                MessageBox.Show(
                    $"✅ Cliente '{updatedCustomer.Name}' actualizado exitosamente.",
                    "Éxito",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information
                );
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Error al actualizar cliente:\n{ex.Message}",
                    "Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error
                );
            }
        };

        dialog.ShowDialog();
    }

    [RelayCommand]
    private async Task ToggleActive(Customer customer)
    {
        if (customer == null) return;

        try
        {
            if (customer.IsActive == 1)
            {
                var result = MessageBox.Show(
                    $"¿Desactivar al cliente '{customer.Name}'?\n\nNo podrá seleccionarse en ventas.",
                    "Confirmar Desactivación",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question
                );

                if (result != MessageBoxResult.Yes) return;

                await _customerRepo.DeleteAsync(customer.Id);
                StatusMessage = $"✅ Cliente '{customer.Name}' desactivado.";
            }
            else
            {
                await _customerRepo.ActivateAsync(customer.Id);
                StatusMessage = $"✅ Cliente '{customer.Name}' activado.";
            }

            await LoadAsync();
        }
        catch (Exception ex)
        {
            StatusMessage = $"❌ Error: {ex.Message}";
        }
    }

    [RelayCommand]
    private async Task ViewPurchaseHistory(Customer customer)
    {
        if (customer == null) return;

        try
        {
            var purchases = await _customerRepo.GetCustomerPurchasesAsync(customer.Id);

            if (purchases.Count == 0)
            {
                MessageBox.Show(
                    $"El cliente '{customer.Name}' no tiene compras registradas.",
                    "Sin Historial",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information
                );
                return;
            }

            var totalSpent = purchases.Sum(p => p.Total);
            var message = $"📊 HISTORIAL DE COMPRAS\n\n";
            message += $"Cliente: {customer.Name}\n";
            message += $"Total de compras: {purchases.Count}\n";
            message += $"Total gastado: S/ {totalSpent:N2}\n\n";
            message += "Últimas 5 compras:\n";

            foreach (var purchase in purchases.Take(5))
            {
                var date = DateTime.Parse(purchase.CreatedUtc);
                message += $"\n• {purchase.SaleNumber}";
                message += $"\n  Fecha: {date:dd/MM/yyyy HH:mm}";
                message += $"\n  Total: S/ {purchase.Total:N2}";
            }

            MessageBox.Show(message, "Historial de Compras", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"Error al cargar historial:\n{ex.Message}",
                "Error",
                MessageBoxButton.OK,
                MessageBoxImage.Error
            );
        }
    }
}