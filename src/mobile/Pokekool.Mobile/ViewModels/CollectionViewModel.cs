using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Pokekool.Mobile.Services;
using Pokekool.Shared.Dtos.Collection;

namespace Pokekool.Mobile.ViewModels;

/// <summary>Ma collection — partagé Admin/Client : liste filtrable, toggle possédée, favoris, priorité, valeur.</summary>
public partial class CollectionViewModel : BaseViewModel
{
    private readonly CollectionApiService _collection;

    public ObservableCollection<CollectionCardItem> Cards { get; } = new();

    [ObservableProperty] private string? _search;
    [ObservableProperty] private bool _ownedOnly;
    [ObservableProperty] private bool _favoritesOnly;
    [ObservableProperty] private string _valueSummary = "";
    [ObservableProperty] private string _statsSummary = "";

    private int _page = 1;
    private bool _hasMore = true;

    public CollectionViewModel(CollectionApiService collection) => _collection = collection;

    [RelayCommand]
    public Task RefreshAsync() => RunSafeAsync(async () =>
    {
        _page = 1;
        _hasMore = true;
        Cards.Clear();
        await LoadPageAsync();
        await LoadHeaderAsync();
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
        var result = await _collection.GetCardsAsync(Search, OwnedOnly, FavoritesOnly, _page);
        foreach (var card in result.Items)
            Cards.Add(new CollectionCardItem(card));
        _hasMore = _page < result.TotalPages;
    }

    private async Task LoadHeaderAsync()
    {
        var value = await _collection.GetValueAsync();
        var stats = await _collection.GetStatsAsync();
        ValueSummary = $"{value.TotalValue:N2} {value.Currency} · {value.CardCount} cartes";
        StatsSummary = $"{stats.OwnedCards}/{stats.TotalCards} possédées · {stats.CompletionPercent:N1}% · {stats.FavoriteCards} favorites";
    }

    [RelayCommand]
    private Task ToggleOwnedAsync(CollectionCardItem item) => RunSafeAsync(async () =>
    {
        var status = await _collection.ToggleOwnedAsync(item.Card.Id);
        item.Apply(status);
        await LoadHeaderAsync();
    });

    [RelayCommand]
    private Task ToggleFavoriteAsync(CollectionCardItem item) => RunSafeAsync(async () =>
    {
        var status = await _collection.SetFavoriteAsync(item.Card.Id, !item.IsFavorite);
        item.Apply(status);
    });

    [RelayCommand]
    private Task TogglePriorityAsync(CollectionCardItem item) => RunSafeAsync(async () =>
    {
        var status = await _collection.SetPriorityAsync(item.Card.Id, !item.IsPriority);
        item.Apply(status);
    });
}

/// <summary>Wrapper observable d'une carte de collection (état togglable).</summary>
public partial class CollectionCardItem : ObservableObject
{
    public CollectionCardDto Card { get; }

    [ObservableProperty] private bool _isOwned;
    [ObservableProperty] private bool _isFavorite;
    [ObservableProperty] private bool _isPriority;

    public string DisplayName => $"{(string.IsNullOrWhiteSpace(Card.DisplayName) ? Card.Name : Card.DisplayName)} · {Card.Number}";
    public string SetLabel => Card.SetName;
    public string? Image => Card.ImageSmall;

    public CollectionCardItem(CollectionCardDto card)
    {
        Card = card;
        _isOwned = card.IsOwned;
        _isFavorite = card.IsFavorite;
        _isPriority = card.IsPriority;
    }

    public void Apply(CollectionCardStatusDto status)
    {
        IsOwned = status.IsOwned;
        IsFavorite = status.IsFavorite;
        IsPriority = status.IsPriority;
    }
}
