using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Pokekool.Mobile.Services;
using Pokekool.Shared.Dtos.Collection;

namespace Pokekool.Mobile.ViewModels;

/// <summary>
/// Scan collection — cas d'usage qui motive l'app mobile :
/// photo via la caméra arrière (MediaPicker), envoi au backend (POST collection/scan),
/// puis ajout en masse des cartes reconnues (bulk-own).
/// </summary>
public partial class ScanViewModel : BaseViewModel
{
    private readonly CollectionApiService _collection;

    /// <summary>Dernière photo scannée, conservée pour permettre un re-roll sans reprendre la photo.</summary>
    private FileResult? _lastPhoto;

    /// <summary>Libellés rejetés par l'utilisateur, cumulés sur les re-rolls d'une même photo.</summary>
    private readonly List<string> _exclusions = new();

    public ObservableCollection<ScanMatchItem> Matches { get; } = new();

    [ObservableProperty] private string? _statusMessage;
    [ObservableProperty] private bool _hasResults;
    [ObservableProperty] private bool _canRetry;

    public ScanViewModel(CollectionApiService collection) => _collection = collection;

    [RelayCommand]
    private Task CaptureAsync() => RunSafeAsync(async () =>
    {
        if (!MediaPicker.Default.IsCaptureSupported)
        {
            ErrorMessage = "Capture photo non supportée sur cet appareil.";
            return;
        }

        var photo = await MediaPicker.Default.CapturePhotoAsync(
            new MediaPickerOptions { Title = "Scanner des cartes" });
        if (photo is null) return; // annulé

        await ScanPhotoAsync(photo, isNewPhoto: true);
    });

    [RelayCommand]
    private Task PickAsync() => RunSafeAsync(async () =>
    {
        var photo = await MediaPicker.Default.PickPhotoAsync(
            new MediaPickerOptions { Title = "Choisir une photo" });
        if (photo is null) return;

        await ScanPhotoAsync(photo, isNewPhoto: true);
    });

    /// <summary>« Ce n'est pas ça » : exclut la carte proposée et relance l'analyse de la même photo.</summary>
    [RelayCommand]
    private Task RejectAsync(ScanMatchItem? item) => RunSafeAsync(async () =>
    {
        if (item is null || _lastPhoto is null) return;

        var label = item.Match.Matched
            ? $"{item.Match.CardName} · {item.Match.SetName} #{item.Match.CardNumber}"
            : item.Match.CardName;
        if (!string.IsNullOrWhiteSpace(label) && !_exclusions.Contains(label))
            _exclusions.Add(label);

        await ScanPhotoAsync(_lastPhoto, isNewPhoto: false);
    });

    private async Task ScanPhotoAsync(FileResult photo, bool isNewPhoto)
    {
        if (isNewPhoto)
        {
            _lastPhoto = photo;
            _exclusions.Clear();
        }

        StatusMessage = _exclusions.Count > 0 ? "Nouvelle analyse (carte exclue)…" : "Analyse de la photo…";
        Matches.Clear();
        HasResults = false;

        await using var stream = await photo.OpenReadAsync();
        var result = await _collection.ScanAsync(
            stream, photo.FileName ?? "scan.jpg",
            _exclusions.Count > 0 ? _exclusions.ToList() : null);

        foreach (var match in result.Matches)
            Matches.Add(new ScanMatchItem(match));

        HasResults = Matches.Count > 0;

        // Le re-roll n'a de sens que si le backend a bien répondu (statut Ok) et qu'on a une photo.
        CanRetry = _lastPhoto is not null
                   && result.Status is ScanStatus.Ok or ScanStatus.LowConfidence or ScanStatus.NoCardDetected;

        // On affiche la cause précise remontée par le backend (service KO, modèle absent,
        // aucune carte, confiance trop basse…) au lieu d'un message générique.
        var excludedSuffix = _exclusions.Count > 0 ? $" ({_exclusions.Count} carte(s) exclue(s))" : "";
        StatusMessage = Matches.Count > 0
            ? $"{Matches.Count} carte(s) détectée(s) — sélectionnez celles à ajouter.{excludedSuffix}"
            : result.Message ?? result.Status switch
            {
                ScanStatus.ServiceUnavailable => "Service d'analyse indisponible. Vérifie qu'Ollama tourne.",
                ScanStatus.ModelNotFound => "Modèle d'analyse absent côté serveur (ollama pull).",
                ScanStatus.InvalidResponse => "Réponse d'analyse illisible. Réessaie.",
                ScanStatus.LowConfidence => $"Carte entrevue mais trop floue — rapproche-toi et améliore l'éclairage.{excludedSuffix}",
                _ => $"Aucune carte reconnue sur cette photo.{excludedSuffix}"
            };
    }

    [RelayCommand]
    private Task AddSelectedAsync() => RunSafeAsync(async () =>
    {
        var selected = Matches.Where(m => m.IsSelected && m.Match.CardId is not null).ToList();
        var ids = selected.Select(m => m.Match.CardId!.Value).Distinct().ToList();

        if (ids.Count == 0)
        {
            StatusMessage = "Aucune carte sélectionnée.";
            return;
        }

        var result = await _collection.BulkAddOwnedAsync(ids, selected.Select(m => m.Match).ToList());
        StatusMessage = $"{result.Added} ajoutée(s), {result.AlreadyOwned} déjà possédée(s).";
        foreach (var m in Matches.Where(m => m.IsSelected))
            m.IsSelected = false;
    });
}

public partial class ScanMatchItem : ObservableObject
{
    public CardScanMatchDto Match { get; }

    [ObservableProperty] private bool _isSelected;

    public string DisplayName => Match.Matched
        ? $"{(string.IsNullOrWhiteSpace(Match.DisplayName) ? Match.CardName : Match.DisplayName)} · {Match.SetName} #{Match.CardNumber}"
        : "Carte non identifiée";

    public string ConfidenceLabel => Match.Matched
        ? $"Confiance {Match.Confidence:P0}" + (Match.GradingCompany is null ? "" : $" · {Match.GradingCompany} {Match.GradeValue}")
        : "—";

    public string? Image => Match.ImageSmall;
    public bool CanSelect => Match.Matched && Match.CardId is not null;

    public ScanMatchItem(CardScanMatchDto match)
    {
        Match = match;
        _isSelected = CanSelect;
    }
}
