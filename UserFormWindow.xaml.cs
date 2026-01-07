using PosPokemon.App.Models;
using System.Windows;
using System.Windows.Controls;

namespace PosPokemon.App;

public partial class UserFormWindow : Window
{
    public string Username { get; private set; } = "";
    public string Password { get; private set; } = "";
    public string Role { get; private set; } = "SELLER";

    private readonly User? _existing;

    public UserFormWindow(User? user)
    {
        InitializeComponent();
        _existing = user;

        if (user != null)
        {
            Title = "Editar Usuario";
            TxtUsername.Text = user.Username;
            TxtUsername.IsEnabled = false; // no editamos username para evitar UNIQUE issues

            // Seleccionar rol
            Role = user.Role;
            CmbRole.SelectedIndex = user.Role == "ADMIN" ? 0 : 1;

            // Password no aplica en edición
            Pwd.IsEnabled = false;
            Pwd.Visibility = Visibility.Collapsed;
        }
        else
        {
            Title = "Nuevo Usuario";
            CmbRole.SelectedIndex = 1; // SELLER por defecto
        }
    }

    private void OnCancel(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }

    private void OnSave(object sender, RoutedEventArgs e)
    {
        Username = TxtUsername.Text.Trim();
        Password = Pwd.Password;
        Role = (CmbRole.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "SELLER";

        DialogResult = true;
        Close();
    }
}
