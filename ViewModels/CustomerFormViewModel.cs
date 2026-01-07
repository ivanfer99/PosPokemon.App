using CommunityToolkit.Mvvm.ComponentModel;
using PosPokemon.App.Models;
using System.Net;
using System.Xml.Linq;

namespace PosPokemon.App.ViewModels;

public partial class CustomerFormViewModel : ObservableObject
{
    [ObservableProperty] private string _documentType = "DNI";
    [ObservableProperty] private string _documentNumber = "";
    [ObservableProperty] private string _name = "";
    [ObservableProperty] private string _phone = "";
    [ObservableProperty] private string _email = "";
    [ObservableProperty] private string _address = "";
    [ObservableProperty] private string _notes = "";

    public string Title { get; }
    public Customer? ExistingCustomer { get; }

    public CustomerFormViewModel(Customer? customer)
    {
        ExistingCustomer = customer;
        Title = customer == null ? "➕ Nuevo Cliente" : "✏️ Editar Cliente";

        if (customer != null)
        {
            DocumentType = customer.DocumentType;
            DocumentNumber = customer.DocumentNumber;
            Name = customer.Name;
            Phone = customer.Phone ?? "";
            Email = customer.Email ?? "";
            Address = customer.Address ?? "";
            Notes = customer.Notes ?? "";
        }
    }
}