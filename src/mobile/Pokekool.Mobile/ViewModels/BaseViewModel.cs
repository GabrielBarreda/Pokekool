using CommunityToolkit.Mvvm.ComponentModel;
using Pokekool.Mobile.Services;

namespace Pokekool.Mobile.ViewModels;

/// <summary>Socle commun : état de chargement, message d'erreur, helper d'exécution sûre.</summary>
public abstract partial class BaseViewModel : ObservableObject
{
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsNotBusy))]
    private bool _isBusy;

    [ObservableProperty]
    private string? _errorMessage;

    public bool IsNotBusy => !IsBusy;

    /// <summary>Exécute une action async avec gestion d'erreur + indicateur de chargement.</summary>
    protected async Task RunSafeAsync(Func<Task> action)
    {
        if (IsBusy) return;
        IsBusy = true;
        ErrorMessage = null;
        try
        {
            await action();
        }
        catch (ApiException ex)
        {
            ErrorMessage = ex.StatusCode switch
            {
                System.Net.HttpStatusCode.Unauthorized => "Session expirée — reconnectez-vous.",
                System.Net.HttpStatusCode.Forbidden => "Accès refusé.",
                System.Net.HttpStatusCode.NotFound => "Introuvable.",
                _ => $"Erreur serveur ({(int)ex.StatusCode}).",
            };
        }
        catch (HttpRequestException)
        {
            ErrorMessage = "Impossible de joindre le serveur.";
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }
        finally
        {
            IsBusy = false;
        }
    }
}
