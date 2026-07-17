using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Pokekool.Mobile.Services;
using Pokekool.Shared.Dtos.Pokemon;

namespace Pokekool.Mobile.ViewModels;

/// <summary>Bourse — séries → sets. Tap sur un set → SetCardsPage (cartes + cotes).</summary>
public partial class BourseViewModel : BaseViewModel
{
    private readonly PokemonApiService _pokemon;

    public ObservableCollection<PokemonSeriesDto> Series { get; } = new();
    public ObservableCollection<PokemonSetDto> Sets { get; } = new();

    [ObservableProperty] private PokemonSeriesDto? _selectedSeries;

    public BourseViewModel(PokemonApiService pokemon) => _pokemon = pokemon;

    [RelayCommand]
    public Task RefreshAsync() => RunSafeAsync(async () =>
    {
        Series.Clear();
        Sets.Clear();
        SelectedSeries = null;
        foreach (var s in await _pokemon.GetSeriesAsync())
            Series.Add(s);
    });

    [RelayCommand]
    private Task SelectSeriesAsync(PokemonSeriesDto series) => RunSafeAsync(async () =>
    {
        SelectedSeries = series;
        Sets.Clear();
        foreach (var set in await _pokemon.GetSetsAsync(series.Id))
            Sets.Add(set);
    });

    [RelayCommand]
    private async Task OpenSetAsync(PokemonSetDto set)
        => await Shell.Current.GoToAsync("set-cards", new Dictionary<string, object>
        {
            ["setId"] = set.Id,
            ["setName"] = set.Name,
        });
}
