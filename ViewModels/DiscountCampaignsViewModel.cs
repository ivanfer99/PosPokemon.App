using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PosPokemon.App.Models;
using PosPokemon.App.Repositories;

namespace PosPokemon.App.ViewModels;

public partial class DiscountCampaignsViewModel : ObservableObject
{
    private readonly DiscountCampaignRepository _campaignRepo;
    private readonly ProductRepository _productRepo;
    private readonly User _currentUser;

    public event Action? BackToDashboardRequested;

    [ObservableProperty] private ObservableCollection<DiscountCampaign> _campaigns = new();
    [ObservableProperty] private DiscountCampaign? _selectedCampaign;

    public DiscountCampaignsViewModel(DiscountCampaignRepository campaignRepo, ProductRepository productRepo, User currentUser)
    {
        _campaignRepo = campaignRepo;
        _productRepo = productRepo;
        _currentUser = currentUser;
    }

    [RelayCommand]
    private void BackToDashboard()
    {
        BackToDashboardRequested?.Invoke();
    }

    [RelayCommand]
    public async Task LoadCampaignsAsync()
    {
        try
        {
            var campaigns = await _campaignRepo.GetAllCampaignsAsync();

            Campaigns.Clear();
            foreach (var campaign in campaigns)
            {
                Campaigns.Add(campaign);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"Error al cargar campañas:\n{ex.Message}",
                "Error",
                MessageBoxButton.OK,
                MessageBoxImage.Error
            );
        }
    }

    [RelayCommand]
    private void CreateCampaign()
    {
        var dialog = new Views.DiscountCampaignFormWindow(_productRepo, _currentUser, null);
        dialog.CampaignSaved += async (campaign, productIds) =>
        {
            try
            {
                await _campaignRepo.CreateCampaignAsync(campaign, productIds);
                await LoadCampaignsAsync();

                MessageBox.Show(
                    $"✅ Campaña '{campaign.Name}' creada exitosamente con {productIds.Count} productos.",
                    "Éxito",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information
                );
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Error al crear campaña:\n{ex.Message}",
                    "Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error
                );
            }
        };

        dialog.ShowDialog();
    }

    [RelayCommand]
    private async Task EditCampaign(DiscountCampaign campaign)
    {
        try
        {
            var (existingCampaign, products) = await _campaignRepo.GetCampaignWithProductsAsync(campaign.Id);

            if (existingCampaign == null)
            {
                MessageBox.Show("No se pudo cargar la campaña.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            var dialog = new Views.DiscountCampaignFormWindow(_productRepo, _currentUser, existingCampaign, products);
            dialog.CampaignSaved += async (updatedCampaign, productIds) =>
            {
                try
                {
                    await _campaignRepo.UpdateCampaignAsync(updatedCampaign, productIds);
                    await LoadCampaignsAsync();

                    MessageBox.Show(
                        $"✅ Campaña '{updatedCampaign.Name}' actualizada exitosamente.",
                        "Éxito",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information
                    );
                }
                catch (Exception ex)
                {
                    MessageBox.Show(
                        $"Error al actualizar campaña:\n{ex.Message}",
                        "Error",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error
                    );
                }
            };

            dialog.ShowDialog();
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"Error al cargar campaña:\n{ex.Message}",
                "Error",
                MessageBoxButton.OK,
                MessageBoxImage.Error
            );
        }
    }

    [RelayCommand]
    private async Task ToggleActive(DiscountCampaign campaign)
    {
        try
        {
            if (campaign.IsActive == 1)
            {
                await _campaignRepo.DeactivateCampaignAsync(campaign.Id);
            }
            else
            {
                await _campaignRepo.ActivateCampaignAsync(campaign.Id);
            }

            await LoadCampaignsAsync();
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"Error al cambiar estado:\n{ex.Message}",
                "Error",
                MessageBoxButton.OK,
                MessageBoxImage.Error
            );
        }
    }

    [RelayCommand]
    private async Task DeleteCampaign(DiscountCampaign campaign)
    {
        var result = MessageBox.Show(
            $"¿Estás seguro de eliminar la campaña '{campaign.Name}'?\n\n" +
            $"Esta acción no se puede deshacer.",
            "Confirmar Eliminación",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning
        );

        if (result != MessageBoxResult.Yes) return;

        try
        {
            await _campaignRepo.DeleteCampaignAsync(campaign.Id);
            await LoadCampaignsAsync();

            MessageBox.Show(
                "✅ Campaña eliminada exitosamente.",
                "Éxito",
                MessageBoxButton.OK,
                MessageBoxImage.Information
            );
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"Error al eliminar campaña:\n{ex.Message}",
                "Error",
                MessageBoxButton.OK,
                MessageBoxImage.Error
            );
        }
    }

    [RelayCommand]
    private async Task ViewDetails(DiscountCampaign campaign)
    {
        try
        {
            var (_, products) = await _campaignRepo.GetCampaignWithProductsAsync(campaign.Id);

            var startDate = DateTime.Parse(campaign.StartDate);
            var endDate = DateTime.Parse(campaign.EndDate);

            var details = $@"📊 DETALLES DE CAMPAÑA

Nombre: {campaign.Name}
Descuento: {campaign.DiscountPercentage}%
Estado: {(campaign.IsActive == 1 ? "✅ Activa" : "❌ Inactiva")}

📅 PERIODO
Inicio: {startDate:dd/MM/yyyy}
Fin: {endDate:dd/MM/yyyy}

📦 PRODUCTOS ({products.Count}):
";

            foreach (var product in products.Take(10))
            {
                var discountedPrice = product.Price * (1 - campaign.DiscountPercentage / 100);
                details += $"\n• {product.Name}";
                details += $"\n  Precio: S/ {product.Price:N2} → S/ {discountedPrice:N2}";
            }

            if (products.Count > 10)
            {
                details += $"\n\n... y {products.Count - 10} productos más";
            }

            details += $"\n\n👤 Creado por: {campaign.CreatorUsername}";
            details += $"\n📅 Creado: {DateTime.Parse(campaign.CreatedUtc):dd/MM/yyyy HH:mm}";

            MessageBox.Show(
                details,
                "Detalles de Campaña",
                MessageBoxButton.OK,
                MessageBoxImage.Information
            );
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"Error al cargar detalles:\n{ex.Message}",
                "Error",
                MessageBoxButton.OK,
                MessageBoxImage.Error
            );
        }
    }
}