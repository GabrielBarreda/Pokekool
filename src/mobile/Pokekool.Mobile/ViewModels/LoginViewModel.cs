using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Pokekool.Mobile.Services;
using Pokekool.Shared.Dtos.Auth;

namespace Pokekool.Mobile.ViewModels;

public partial class LoginViewModel : BaseViewModel
{
    private readonly AuthService _auth;

    [ObservableProperty] private string _email = string.Empty;
    [ObservableProperty] private string _password = string.Empty;

    // Inscription (client)
    [ObservableProperty] private bool _isRegisterMode;
    [ObservableProperty] private string? _firstName;
    [ObservableProperty] private string? _lastName;

    public LoginViewModel(AuthService auth) => _auth = auth;

    [RelayCommand]
    private Task LoginAsync() => RunSafeAsync(async () =>
    {
        if (string.IsNullOrWhiteSpace(Email) || string.IsNullOrWhiteSpace(Password))
        {
            ErrorMessage = "Email et mot de passe requis.";
            return;
        }

        if (IsRegisterMode)
            await _auth.RegisterAsync(new RegisterRequest(Email.Trim(), Password, FirstName, LastName));
        else
            await _auth.LoginAsync(Email.Trim(), Password);

        Password = string.Empty;
        if (Shell.Current is AppShell shell)
            await shell.NavigateAfterAuthAsync(true);
    });

    [RelayCommand]
    private void ToggleMode() => IsRegisterMode = !IsRegisterMode;
}
