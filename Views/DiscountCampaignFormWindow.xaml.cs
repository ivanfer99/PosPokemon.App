using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using PosPokemon.App.Models;
using PosPokemon.App.Repositories;

namespace PosPokemon.App.Views;

public partial class DiscountCampaignFormWindow : Window
{
    private readonly ProductRepository _productRepo;
    private readonly User _currentUser;
    private readonly DiscountCampaign? _existingCampaign;

    private readonly ObservableCollection<Product> _availableProducts = new();
    private readonly ObservableCollection<Product> _selectedProducts = new();

    public event Action<DiscountCampaign, List<long>>? CampaignSaved;

    public DiscountCampaignFormWindow(
        ProductRepository productRepo,
        User currentUser,
        DiscountCampaign? existingCampaign = null,
        List<Product>? preselectedProducts = null)
    {
        InitializeComponent();

        _productRepo = productRepo;
        _currentUser = currentUser;
        _existingCampaign = existingCampaign;

        ListAvailableProducts.ItemsSource = _availableProducts;
        ListSelectedProducts.ItemsSource = _selectedProducts;

        if (existingCampaign != null)
        {
            TxtTitle.Text = "✏️ Editar Campaña de Descuento";
            LoadExistingCampaign(existingCampaign, preselectedProducts);
        }
        else
        {
            DateStart.SelectedDate = DateTime.Today;
            DateEnd.SelectedDate = DateTime.Today.AddDays(7);
        }

        LoadAllProducts();
    }

    private void LoadExistingCampaign(DiscountCampaign campaign, List<Product>? products)
    {
        TxtName.Text = campaign.Name;
        TxtDiscountPercentage.Text = campaign.DiscountPercentage.ToString();
        DateStart.SelectedDate = DateTime.Parse(campaign.StartDate);
        DateEnd.SelectedDate = DateTime.Parse(campaign.EndDate);

        if (products != null)
        {
            foreach (var product in products)
            {
                _selectedProducts.Add(product);
            }
        }
    }

    private async void LoadAllProducts()
    {
        try
        {
            var allProducts = await _productRepo.SearchAsync("");

            foreach (var product in allProducts)
            {
                if (!_selectedProducts.Any(p => p.Id == product.Id))
                {
                    _availableProducts.Add(product);
                }
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error al cargar productos:\n{ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async void OnSearchProducts(object sender, RoutedEventArgs e)
    {
        try
        {
            var query = TxtSearchProduct.Text.Trim();
            var results = await _productRepo.SearchAsync(query);

            _availableProducts.Clear();
            foreach (var product in results)
            {
                if (!_selectedProducts.Any(p => p.Id == product.Id))
                {
                    _availableProducts.Add(product);
                }
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error al buscar productos:\n{ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void OnAddProduct(object sender, MouseButtonEventArgs e)
    {
        var selected = ListAvailableProducts.SelectedItems.Cast<Product>().ToList();

        foreach (var product in selected)
        {
            _selectedProducts.Add(product);
            _availableProducts.Remove(product);
        }
    }

    private void OnRemoveProduct(object sender, MouseButtonEventArgs e)
    {
        var selected = ListSelectedProducts.SelectedItems.Cast<Product>().ToList();

        foreach (var product in selected)
        {
            _availableProducts.Add(product);
            _selectedProducts.Remove(product);
        }
    }

    private void OnSave(object sender, RoutedEventArgs e)
    {
        // Validaciones
        if (string.IsNullOrWhiteSpace(TxtName.Text))
        {
            MessageBox.Show("El nombre de la campaña es obligatorio.", "Validación", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (!decimal.TryParse(TxtDiscountPercentage.Text, out var discountPercentage) || discountPercentage <= 0 || discountPercentage > 100)
        {
            MessageBox.Show("Ingresa un porcentaje de descuento válido (1-100).", "Validación", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (DateStart.SelectedDate == null || DateEnd.SelectedDate == null)
        {
            MessageBox.Show("Selecciona las fechas de inicio y fin.", "Validación", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (DateEnd.SelectedDate < DateStart.SelectedDate)
        {
            MessageBox.Show("La fecha de fin no puede ser anterior a la fecha de inicio.", "Validación", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (_selectedProducts.Count == 0)
        {
            MessageBox.Show("Selecciona al menos un producto.", "Validación", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        // Crear o actualizar campaña
        var campaign = _existingCampaign ?? new DiscountCampaign();

        campaign.Name = TxtName.Text.Trim();
        campaign.DiscountPercentage = discountPercentage;
        campaign.StartDate = DateStart.SelectedDate.Value.ToString("yyyy-MM-dd");
        campaign.EndDate = DateEnd.SelectedDate.Value.ToString("yyyy-MM-dd");
        campaign.IsActive = 1;
        campaign.CreatedBy = _currentUser.Id;

        var productIds = _selectedProducts.Select(p => p.Id).ToList();

        CampaignSaved?.Invoke(campaign, productIds);
        Close();
    }

    private void OnCancel(object sender, RoutedEventArgs e)
    {
        Close();
    }
}