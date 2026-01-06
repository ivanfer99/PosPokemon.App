using System.Windows;
using PosPokemon.App.ViewModels;

namespace PosPokemon.App;

public partial class LoginWindow : Window
{
    public LoginWindow()
    {
        InitializeComponent();

        // Enfocar el campo de usuario al abrir
        Loaded += (s, e) => TxtUsername.Focus();

        // Permitir presionar Enter para login
        TxtUsername.KeyDown += (s, e) => { if (e.Key == System.Windows.Input.Key.Enter) Pwd.Focus(); };
        Pwd.KeyDown += (s, e) => { if (e.Key == System.Windows.Input.Key.Enter) OnLoginClick(s, e); };
    }

    private void OnLoginClick(object sender, RoutedEventArgs e)
    {
        if (DataContext is not LoginViewModel vm)
            return;

        // Pasar password real desde PasswordBox al VM
        vm.Password = Pwd.Password;

        // Ejecutar comando de login
        vm.LoginCommand.Execute(null);
    }

    private void OnCloseClick(object sender, RoutedEventArgs e)
    {
        Application.Current.Shutdown();
    }
}
