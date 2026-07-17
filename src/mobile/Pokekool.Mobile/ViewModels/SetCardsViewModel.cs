using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Pokekool.Mobile.Services;
using Pokekool.Shared.Dtos.Pokemon;

namespace Pokekool.Mobile.ViewModels;

/// <summary>Cartes d'un set avec meilleur prix marché (GET market/sets/{id}/cards).</summary>
[QueryProperty(nameof(SetId), "setId")]
[QueryProperty(nameof(SetName), "setName")]
public partial class SetCardsViewModel : BaseViewModel
{
    private readonly MarketApiService _market;

    public ObservableCollection<MarketCardDto> Cards { get; } = new();

    [ObservableProperty] private Guid _setId;
    [ObservableProperty] private string _setName = "";
    [ObservableProperty] private string? _search;

    private int _page = 1;
    private bool _hasMore = true;

    public SetCardsViewModel(MarketApiService market) => _market = market;

    partial void OnSetIdChanged(Guid value) => RefreshCommand.Execute(null);

    [RelayCommand]
    public Task RefreshAsync() => RunSafeAsync(async () =>
    {
        _page = 1;
        _hasMore = true;
        Cards.Clear();
        await LoadPageAsync();
    });

    [RelayCommand]
    private async Task LoadMoreAsync()
    {
        if (IsBusy || !_hasMore) return;
        await RunSafeAsync(async () =>
        {
            _page++;
            await LoadPageAsync();
        });
    }

    private async Task LoadPageAsync()
    {
        var result = await _market.GetSetCardsAsync(SetId, Search, page: _page);
        foreach (var card in result.Items)
            Cards.Add(card);
        _hasMore = _page < result.TotalPages;
    }

    [RelayCommand]
    private async Task OpenCardAsync(MarketCardDto card)
        => await Shell.Current.GoToAsync("card-detail", new Dictionary<string, object>
        {
            ["cardId"] = card.Id,
        });
}
