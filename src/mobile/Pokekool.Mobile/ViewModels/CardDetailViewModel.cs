using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Pokekool.Mobile.Services;
using Pokekool.Shared.Dtos.Pokemon;

namespace Pokekool.Mobile.ViewModels;

/// <summary>Fiche carte : détail + populations/prix gradés PSA/CGC (market/cards/{id}/grading).</summary>
[QueryProperty(nameof(CardId), "cardId")]
public partial class CardDetailViewModel : BaseViewModel
{
    private readonly PokemonApiService _pokemon;
    private readonly MarketApiService _market;

    [ObservableProperty] private Guid _cardId;
    [ObservableProperty] private PokemonCardDto? _card;

    public ObservableCollection<GradingRow> GradingRows { get; } = new();

    public CardDetailViewModel(PokemonApiService pokemon, MarketApiService market)
    {
        _pokemon = pokemon;
        _market = market;
    }

    partial void OnCardIdChanged(Guid value) => LoadCommand.Execute(null);

    [RelayCommand]
    private Task LoadAsync() => RunSafeAsync(async () =>
    {
        GradingRows.Clear();
        Card = await _pokemon.GetCardAsync(CardId);

        var gradings = await _market.GetCardGradingAsync(CardId);
        foreach (var company in gradings)
        foreach (var entry in company.Entries)
            GradingRows.Add(new GradingRow(
                company.Company,
                entry.Label,
                entry.MarketPrice is null ? "—" : $"{entry.MarketPrice:N2} {entry.Currency ?? "USD"}",
                entry.Population is null ? "—" : $"pop. {entry.Population}"));
    });
}

public sealed record GradingRow(string Company, string Grade, string Price, string Population);
