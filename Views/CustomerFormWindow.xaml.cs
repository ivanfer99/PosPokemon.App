using System;
using System.Windows;
using System.Windows.Controls;
using PosPokemon.App.Models;

namespace PosPokemon.App.Views;

public partial class CustomerFormWindow : Window
{
    private readonly Customer? _existingCustomer;

    public event Action<Customer>? CustomerSaved;

    public CustomerFormWindow(Customer? customer)
    {
        InitializeComponent();

        _existingCustomer = customer;

        if (customer != null)
        {
            TxtTitle.Text = "✏️ Editar Cliente";
            LoadCustomerData(customer);
        }
    }

    private void LoadCustomerData(Customer customer)
    {
        // Seleccionar tipo de documento
        foreach (ComboBoxItem item in CmbDocumentType.Items)
        {
            if (item.Content.ToString() == customer.DocumentType)
            {
                CmbDocumentType.SelectedItem = item;
                break;
            }
        }

        TxtDocumentNumber.Text = customer.DocumentNumber;
        TxtName.Text = customer.Name;
        TxtPhone.Text = customer.Phone ?? "";
        TxtEmail.Text = customer.Email ?? "";
        TxtAddress.Text = customer.Address ?? "";
        TxtNotes.Text = customer.Notes ?? "";
    }

    private void OnSave(object sender, RoutedEventArgs e)
    {
        // Validaciones
        if (string.IsNullOrWhiteSpace(TxtDocumentNumber.Text))
        {
            MessageBox.Show("El número de documento es obligatorio.", "Validación", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (string.IsNullOrWhiteSpace(TxtName.Text))
        {
            MessageBox.Show("El nombre es obligatorio.", "Validación", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        // Validación de email (si está lleno)
        if (!string.IsNullOrWhiteSpace(TxtEmail.Text))
        {
            if (!TxtEmail.Text.Contains("@"))
            {
                MessageBox.Show("El email no es válido.", "Validación", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
        }

        // Crear o actualizar cliente
        var customer = _existingCustomer ?? new Customer();

        customer.DocumentType = ((ComboBoxItem)CmbDocumentType.SelectedItem).Content.ToString() ?? "DNI";
        customer.DocumentNumber = TxtDocumentNumber.Text.Trim();
        customer.Name = TxtName.Text.Trim();
        customer.Phone = string.IsNullOrWhiteSpace(TxtPhone.Text) ? null : TxtPhone.Text.Trim();
        customer.Email = string.IsNullOrWhiteSpace(TxtEmail.Text) ? null : TxtEmail.Text.Trim();
        customer.Address = string.IsNullOrWhiteSpace(TxtAddress.Text) ? null : TxtAddress.Text.Trim();
        customer.Notes = string.IsNullOrWhiteSpace(TxtNotes.Text) ? null : TxtNotes.Text.Trim();
        customer.IsActive = 1;

        CustomerSaved?.Invoke(customer);
        Close();
    }

    private void OnCancel(object sender, RoutedEventArgs e)
    {
        Close();
    }
}