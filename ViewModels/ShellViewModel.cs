using CommunityToolkit.Mvvm.ComponentModel;
using PosPokemon.App.Models;

namespace PosPokemon.App.ViewModels;

public partial class ShellViewModel : ObservableObject
{
    public User CurrentUser { get; }

    public bool IsAdmin => CurrentUser.Role == "ADMIN";
    public bool IsSeller => CurrentUser.Role == "SELLER";

    public ShellViewModel(User user)
    {
        CurrentUser = user;
    }
}
