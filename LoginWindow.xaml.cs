using System.Windows;
using System.Windows.Input;
using PosPokemon.App.ViewModels;

namespace PosPokemon.App
{
    public partial class LoginWindow : Window
    {
        public LoginWindow()
        {
            InitializeComponent();

            // Enfocar el campo de usuario al abrir
            Loaded += (s, e) => TxtUsername.Focus();

            // Permitir presionar Enter para login
            TxtUsername.KeyDown += (s, e) =>
            {
                if (e.Key == Key.Enter)
                    Pwd.Focus();
            };

            Pwd.KeyDown += async (s, e) =>
            {
                if (e.Key == Key.Enter)
                    await DoLoginAsync();
            };
        }

        private async void OnLoginClick(object sender, RoutedEventArgs e)
        {
            await DoLoginAsync();
        }

        private async System.Threading.Tasks.Task DoLoginAsync()
        {
            if (DataContext is not LoginViewModel vm)
                return;

            // Pasar password real desde PasswordBox al VM
            vm.Password = Pwd.Password;

            // ✅ EJECUTAR COMANDO ASYNC CORRECTAMENTE
            await vm.LoginCommand.ExecuteAsync(null);
        }

        private void OnCloseClick(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }
    }
}
