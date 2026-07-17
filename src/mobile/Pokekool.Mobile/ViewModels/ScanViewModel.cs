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

    public ObservableCollection<ScanMatchItem> Matches { get; } = new();

    [ObservableProperty] private string? _statusMessage;
    [ObservableProperty] private bool _hasResults;

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

        await ScanPhotoAsync(photo);
    });

    [RelayCommand]
    private Task PickAsync() => RunSafeAsync(async () =>
    {
        var photo = await MediaPicker.Default.PickPhotoAsync(
            new MediaPickerOptions { Title = "Choisir une photo" });
        if (photo is null) return;

        await ScanPhotoAsync(photo);
    });

    private async Task ScanPhotoAsync(FileResult photo)
    {
        StatusMessage = "Analyse de la photo…";
        Matches.Clear();
        HasResults = false;

        await using var stream = await photo.OpenReadAsync();
        var result = await _collection.ScanAsync(stream, photo.FileName ?? "scan.jpg");

        foreach (var match in result.Matches)
            Matches.Add(new ScanMatchItem(match));

        HasResults = Matches.Count > 0;
        StatusMessage = Matches.Count == 0
            ? "Aucune carte reconnue sur cette photo."
            : $"{Matches.Count} carte(s) détectée(s) — sélectionnez celles à ajouter.";
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
        ? $"{Match.CardName} · {Match.SetName} #{Match.CardNumber}"
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
