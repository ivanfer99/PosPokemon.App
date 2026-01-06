using System.Windows;
using PosPokemon.App.ViewModels;

namespace PosPokemon.App;

public partial class LoginWindow : Window
{
    public LoginWindow()
    {
        InitializeComponent();
    }

    private void OnLoginClick(object sender, RoutedEventArgs e)
    {
        if (DataContext is not LoginViewModel vm)
            return;

        // Pasar password real desde PasswordBox al VM
        vm.Password = Pwd.Password;

        // Ejecutar comando de login (sin ExecuteAsync para máxima compatibilidad)
        vm.LoginCommand.Execute(null);
    }
}
