using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PosPokemon.App.Models;
using PosPokemon.App.Repositories;

namespace PosPokemon.App.ViewModels;

public sealed partial class UsersViewModel : ObservableObject
{
    private readonly UserRepository _repo;

    public event Action? BackToDashboardRequested;

    public ObservableCollection<User> Users { get; } = new();

    [ObservableProperty] private User? _selectedUser;

    public UsersViewModel(UserRepository repo)
    {
        _repo = repo;
    }

    [RelayCommand]
    private void BackToDashboard()
    {
        BackToDashboardRequested?.Invoke();
    }

    [RelayCommand]
    public async Task LoadAsync()
    {
        Users.Clear();
        var all = await _repo.GetAllAsync();
        foreach (var u in all)
            Users.Add(u);
    }

    [RelayCommand]
    private async Task CreateUserAsync()
    {
        var win = new UserFormWindow(null);
        if (win.ShowDialog() == true)
        {
            // Validación simple
            if (string.IsNullOrWhiteSpace(win.Username) || string.IsNullOrWhiteSpace(win.Password))
            {
                MessageBox.Show("Usuario y contraseña son obligatorios.", "Error");
                return;
            }

            try
            {
                await _repo.CreateAsync(win.Username, win.Password, win.Role);
                await LoadAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show("No se pudo crear usuario:\n" + ex.Message, "Error");
            }
        }
    }

    [RelayCommand]
    private async Task EditUserAsync(User user)
    {
        var win = new UserFormWindow(user);
        if (win.ShowDialog() == true)
        {
            try
            {
                await _repo.SetRoleAsync(user.Id, win.Role);

                // Nota: username no lo cambiamos (evita conflictos UNIQUE)
                // Si quisieras cambiar username, habría que agregar método UpdateUsername.

                await LoadAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show("No se pudo editar usuario:\n" + ex.Message, "Error");
            }
        }
    }

    [RelayCommand]
    private async Task ToggleActiveAsync(User user)
    {
        try
        {
            var newValue = user.IsActive == 1 ? 0 : 1;
            await _repo.SetActiveAsync(user.Id, newValue);
            await LoadAsync();
        }
        catch (Exception ex)
        {
            MessageBox.Show("No se pudo cambiar estado:\n" + ex.Message, "Error");
        }
    }

    [RelayCommand]
    private async Task ResetPasswordAsync(User user)
    {
        var result = MessageBox.Show(
            $"¿Resetear contraseña de '{user.Username}' a '1234'?",
            "Reset Password",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);

        if (result != MessageBoxResult.Yes) return;

        try
        {
            await _repo.ResetPasswordAsync(user.Id, "1234");
            MessageBox.Show("Contraseña reseteada a: 1234", "OK");
        }
        catch (Exception ex)
        {
            MessageBox.Show("No se pudo resetear contraseña:\n" + ex.Message, "Error");
        }
    }
}
