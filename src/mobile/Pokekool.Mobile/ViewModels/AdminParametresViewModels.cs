using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Pokekool.Mobile.Services;
using Pokekool.Shared.Dtos.Auth;
using Pokekool.Shared.Dtos.Authorization;

namespace Pokekool.Mobile.ViewModels;

// ============================================================
// Onglet Paramètres (Admin) + Profil (partagé)
// ============================================================

/// <summary>Mon profil : infos du compte, changement de mot de passe, déconnexion.</summary>
public partial class ProfileViewModel : BaseViewModel
{
    private readonly AccountApiService _account;
    private readonly AuthService _auth;
    private readonly CardDisplayLanguageService _cardDisplayLanguage;

    [ObservableProperty] private CurrentUserDto? _me;
    [ObservableProperty] private string _currentPassword = string.Empty;
    [ObservableProperty] private string _newPassword = string.Empty;
    [ObservableProperty] private string _newPasswordConfirm = string.Empty;
    [ObservableProperty] private string? _passwordMessage;

    /// <summary>Langue d'affichage des noms de cartes — persistée localement, envoyée au
    /// backend via un header sur chaque requête (voir ADR-004, mémoire Obsidian du projet).
    /// Résolue par CardDisplayLanguageService, jamais côté client.</summary>
    public bool CardLanguageIsFrench
    {
        get => _cardDisplayLanguage.Language == "fr";
        set
        {
            _cardDisplayLanguage.Language = value ? "fr" : "en";
            OnPropertyChanged();
        }
    }

    public ProfileViewModel(AccountApiService account, AuthService auth, CardDisplayLanguageService cardDisplayLanguage)
    {
        _account = account;
        _auth = auth;
        _cardDisplayLanguage = cardDisplayLanguage;
    }

    [RelayCommand]
    public Task RefreshAsync() => RunSafeAsync(async () => Me = await _account.GetMeAsync());

    [RelayCommand]
    private Task ChangePasswordAsync() => RunSafeAsync(async () =>
    {
        PasswordMessage = null;
        if (string.IsNullOrWhiteSpace(CurrentPassword) || string.IsNullOrWhiteSpace(NewPassword))
        {
            PasswordMessage = "Tous les champs sont requis.";
            return;
        }
        if (NewPassword != NewPasswordConfirm)
        {
            PasswordMessage = "La confirmation ne correspond pas.";
            return;
        }

        await _account.ChangePasswordAsync(CurrentPassword, NewPassword);
        CurrentPassword = NewPassword = NewPasswordConfirm = string.Empty;
        PasswordMessage = "Mot de passe modifié ✔";
    });

    [RelayCommand]
    private async Task LogoutAsync()
    {
        await _auth.LogoutAsync();
        await Shell.Current.GoToAsync("//login");
    }
}

/// <summary>Utilisateurs (admin) : liste, activation/désactivation.</summary>
public partial class UsersAdminViewModel : BaseViewModel
{
    private readonly UsersApiService _users;

    public ObservableCollection<Pokekool.Shared.Dtos.Users.UserSummaryDto> Users { get; } = new();

    [ObservableProperty] private string? _search;

    public UsersAdminViewModel(UsersApiService users) => _users = users;

    [RelayCommand]
    public Task RefreshAsync() => RunSafeAsync(LoadAsync);

    private async Task LoadAsync()
    {
        Users.Clear();
        var page = await _users.GetUsersAsync(Search, pageSize: 100);
        foreach (var u in page.Items) Users.Add(u);
    }

    [RelayCommand]
    private Task ToggleActiveAsync(Pokekool.Shared.Dtos.Users.UserSummaryDto user) => RunSafeAsync(async () =>
    {
        if (user.IsActive) await _users.DeactivateAsync(user.Id);
        else await _users.ActivateAsync(user.Id);
        await LoadAsync();
    });
}

/// <summary>Rôles &amp; permissions (lecture — l'édition reste sur le web).</summary>
public partial class RolesViewModel : BaseViewModel
{
    private readonly UsersApiService _users;

    public ObservableCollection<RoleItem> Roles { get; } = new();

    public RolesViewModel(UsersApiService users) => _users = users;

    [RelayCommand]
    public Task RefreshAsync() => RunSafeAsync(async () =>
    {
        Roles.Clear();
        foreach (var r in await _users.GetRolesAsync())
            Roles.Add(new RoleItem(r.Name, r.Code, r.IsSystem,
                $"{r.Permissions.Count} permission(s)" + (r.Description is null ? "" : $" · {r.Description}")));
    });
}

public sealed record RoleItem(string Name, string Code, bool IsSystem, string Details);

/// <summary>Paramètres providers (CardMarket, etc.) : clés masquées + upsert d'une valeur.</summary>
public partial class ProvidersViewModel : BaseViewModel
{
    private static readonly string[] KnownProviders = { "cardmarket", "pricecharting", "pokemontcg" };

    private readonly AccountApiService _account;

    public ObservableCollection<ProviderSettingRow> Settings { get; } = new();
    public IReadOnlyList<string> Providers => KnownProviders;

    [ObservableProperty] private string _selectedProvider = KnownProviders[0];

    public ProvidersViewModel(AccountApiService account) => _account = account;

    partial void OnSelectedProviderChanged(string value) => RefreshCommand.Execute(null);

    [RelayCommand]
    public Task RefreshAsync() => RunSafeAsync(LoadAsync);

    private async Task LoadAsync()
    {
        Settings.Clear();
        var result = await _account.GetProviderSettingsAsync(SelectedProvider);
        foreach (var s in result.Keys)
            Settings.Add(new ProviderSettingRow(
                s.Key,
                s.Value ?? (s.IsConfigured ? "•••••• (configurée)" : "non configurée")));
    }

    [RelayCommand]
    private async Task EditSettingAsync(ProviderSettingRow row)
    {
        var value = await Shell.Current.DisplayPromptAsync(
            row.Key, "Nouvelle valeur (sera chiffrée côté serveur) :", "Enregistrer", "Annuler");
        if (string.IsNullOrWhiteSpace(value)) return;

        await RunSafeAsync(async () =>
        {
            await _account.UpsertProviderSettingAsync(SelectedProvider, row.Key, value);
            await LoadAsync();
        });
    }
}

public sealed record ProviderSettingRow(string Key, string MaskedValue);
