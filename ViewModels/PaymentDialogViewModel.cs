using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PosPokemon.App.Models;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Channels;

namespace PosPokemon.App.ViewModels;

public partial class PaymentDialogViewModel : ObservableObject
{
    public decimal TotalToPay { get; }

    [ObservableProperty] private string _selectedPaymentMethod = "Efectivo";
    [ObservableProperty] private decimal _amountReceived = 0;
    [ObservableProperty] private decimal _change = 0;
    [ObservableProperty] private string _errorMessage = "";

    public ObservableCollection<string> PaymentMethods { get; } = new()
    {
        "Efectivo",
        "Tarjeta",
        "Yape",
        "Plin",
        "Transferencia"
    };

    public event Action? PaymentConfirmed;
    public event Action? PaymentCancelled;

    public PaymentDialogViewModel(decimal totalToPay)
    {
        TotalToPay = totalToPay;
        AmountReceived = totalToPay; // Por defecto, monto exacto
        CalculateChange();
    }

    partial void OnAmountReceivedChanged(decimal value)
    {
        CalculateChange();
    }

    partial void OnSelectedPaymentMethodChanged(string value)
    {
        // Si no es efectivo, el monto recibido es exacto (no hay vuelto)
        if (value != "Efectivo")
        {
            AmountReceived = TotalToPay;
            Change = 0;
        }
        else
        {
            CalculateChange();
        }
    }

    private void CalculateChange()
    {
        if (SelectedPaymentMethod == "Efectivo")
        {
            Change = AmountReceived - TotalToPay;
        }
        else
        {
            Change = 0;
        }
    }

    [RelayCommand]
    private void ConfirmPayment()
    {
        ErrorMessage = "";

        // Validaciones
        if (SelectedPaymentMethod == "Efectivo")
        {
            if (AmountReceived < TotalToPay)
            {
                ErrorMessage = "❌ El monto recibido no puede ser menor al total.";
                return;
            }

            if (AmountReceived <= 0)
            {
                ErrorMessage = "❌ Ingresa un monto válido.";
                return;
            }
        }

        PaymentConfirmed?.Invoke();
    }

    [RelayCommand]
    private void Cancel()
    {
        PaymentCancelled?.Invoke();
    }

    // Método para agregar rápido montos comunes (solo efectivo)
    [RelayCommand]
    private void SetAmount(string amount)
    {
        if (decimal.TryParse(amount, out var value))
        {
            AmountReceived = value;
        }
    }
}