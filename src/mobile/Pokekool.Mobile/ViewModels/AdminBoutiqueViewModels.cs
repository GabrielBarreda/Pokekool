using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Pokekool.Mobile.Services;
using Pokekool.Shared.Dtos.Chat;
using Pokekool.Shared.Dtos.Shop;
using Pokekool.Shared.Dtos.Storage;

namespace Pokekool.Mobile.ViewModels;

// ============================================================
// Onglet Boutique (Admin)
// ============================================================

/// <summary>Présentoir — produits en vente, toggle disponibilité.</summary>
public partial class PresentoirViewModel : BaseViewModel
{
    private readonly AdminShopApiService _shop;

    public ObservableCollection<ShopProductDto> Products { get; } = new();

    [ObservableProperty] private string? _search;

    public PresentoirViewModel(AdminShopApiService shop) => _shop = shop;

    [RelayCommand]
    public Task RefreshAsync() => RunSafeAsync(async () =>
    {
        Products.Clear();
        var page = await _shop.GetProductsAsync(search: Search, pageSize: 100);
        foreach (var p in page.Items) Products.Add(p);
    });

    [RelayCommand]
    private Task ToggleAvailabilityAsync(ShopProductDto product) => RunSafeAsync(async () =>
    {
        var updated = await _shop.ToggleAvailabilityAsync(product.Id, !product.IsAvailable);
        var index = Products.IndexOf(product);
        if (index >= 0) Products[index] = updated;
    });
}

/// <summary>Stock boutique (lecture).</summary>
public partial class StockViewModel : BaseViewModel
{
    private readonly AdminShopApiService _shop;

    public ObservableCollection<ShopStockDto> Items { get; } = new();

    [ObservableProperty] private string? _search;

    public StockViewModel(AdminShopApiService shop) => _shop = shop;

    [RelayCommand]
    public Task RefreshAsync() => RunSafeAsync(async () =>
    {
        Items.Clear();
        var page = await _shop.GetStockAsync(search: Search, pageSize: 100);
        foreach (var s in page.Items) Items.Add(s);
    });
}

/// <summary>Planning RDV — liste + changement de statut.</summary>
public partial class PlanningViewModel : BaseViewModel
{
    private readonly AdminShopApiService _shop;

    public ObservableCollection<ShopAppointmentDto> Appointments { get; } = new();

    [ObservableProperty] private string? _statusFilter;

    public PlanningViewModel(AdminShopApiService shop) => _shop = shop;

    [RelayCommand]
    public Task RefreshAsync() => RunSafeAsync(async () =>
    {
        Appointments.Clear();
        var page = await _shop.GetAppointmentsAsync(status: StatusFilter);
        foreach (var a in page.Items.OrderBy(a => a.ScheduledAt)) Appointments.Add(a);
    });

    [RelayCommand]
    private async Task ChangeStatusAsync(ShopAppointmentDto appointment)
    {
        var status = await Shell.Current.DisplayActionSheetAsync(
            "Statut du RDV", "Annuler", null, "Confirmed", "InProgress", "Completed", "Cancelled");
        if (status is null or "Annuler") return;

        await RunSafeAsync(async () =>
        {
            var updated = await _shop.UpdateAppointmentStatusAsync(appointment.Id, status);
            var index = Appointments.IndexOf(appointment);
            if (index >= 0) Appointments[index] = updated;
        });
    }
}

/// <summary>
/// Stockage 3D (mode boutique) : salles/cartons en liste, lookup par code,
/// confirmation des déplacements détectés. Le viewer 3D reste sur le web.
/// </summary>
public partial class StorageViewModel : BaseViewModel
{
    private readonly StorageApiService _storage;

    public ObservableCollection<StorageRoomDto> Rooms { get; } = new();
    public ObservableCollection<StorageBoxDto> Boxes { get; } = new();
    public ObservableCollection<StorageMovementEventDto> PendingMovements { get; } = new();

    [ObservableProperty] private StorageRoomDto? _selectedRoom;
    [ObservableProperty] private string? _lookupCode;
    [ObservableProperty] private string? _lookupResult;

    public StorageViewModel(StorageApiService storage) => _storage = storage;

    [RelayCommand]
    public Task RefreshAsync() => RunSafeAsync(async () =>
    {
        Rooms.Clear();
        Boxes.Clear();
        PendingMovements.Clear();
        foreach (var r in await _storage.GetRoomsAsync()) Rooms.Add(r);
        foreach (var m in await _storage.GetPendingMovementsAsync()) PendingMovements.Add(m);
    });

    [RelayCommand]
    private Task SelectRoomAsync(StorageRoomDto room) => RunSafeAsync(async () =>
    {
        SelectedRoom = room;
        Boxes.Clear();
        foreach (var b in await _storage.GetBoxesAsync(room.Id)) Boxes.Add(b);
    });

    [RelayCommand]
    private Task LookupAsync() => RunSafeAsync(async () =>
    {
        LookupResult = null;
        if (string.IsNullOrWhiteSpace(LookupCode)) return;

        var result = await _storage.LookupAsync(LookupCode.Trim());
        var binders = result.Binders.Count;
        var cards = result.Binders.Sum(b => b.Locations.Count);
        LookupResult = $"Carton « {result.Box.LabelOrigin} » — {binders} classeur(s), {cards} emplacement(s).";
    });

    [RelayCommand]
    private Task ConfirmMovementAsync(StorageMovementEventDto movement) => RunSafeAsync(async () =>
    {
        await _storage.ConfirmMovementAsync(movement.Id, confirm: true);
        PendingMovements.Remove(movement);
    });

    [RelayCommand]
    private Task RejectMovementAsync(StorageMovementEventDto movement) => RunSafeAsync(async () =>
    {
        await _storage.ConfirmMovementAsync(movement.Id, confirm: false);
        PendingMovements.Remove(movement);
    });
}

/// <summary>Demandes d'achat clients — liste + traitement.</summary>
public partial class PurchaseRequestsViewModel : BaseViewModel
{
    private readonly AdminShopApiService _shop;

    public ObservableCollection<PurchaseRequestDto> Requests { get; } = new();

    public PurchaseRequestsViewModel(AdminShopApiService shop) => _shop = shop;

    [RelayCommand]
    public Task RefreshAsync() => RunSafeAsync(LoadAsync);

    private async Task LoadAsync()
    {
        Requests.Clear();
        var page = await _shop.GetPurchaseRequestsAsync(pageSize: 100);
        foreach (var r in page.Items) Requests.Add(r);
    }

    [RelayCommand]
    private async Task ChangeStatusAsync(PurchaseRequestDto request)
    {
        var status = await Shell.Current.DisplayActionSheetAsync(
            "Statut de la demande", "Annuler", null, "Contacted", "Completed", "Cancelled");
        if (status is null or "Annuler") return;

        await RunSafeAsync(async () =>
        {
            await _shop.UpdatePurchaseRequestStatusAsync(request.Id, status);
            await LoadAsync();
        });
    }
}

/// <summary>Infos boutique (lecture — l'édition complète reste sur le web).</summary>
public partial class ShopInfoViewModel : BaseViewModel
{
    private readonly AdminShopApiService _shop;

    [ObservableProperty] private ShopDto? _shopInfo;

    public ShopInfoViewModel(AdminShopApiService shop) => _shop = shop;

    [RelayCommand]
    public Task RefreshAsync() => RunSafeAsync(async () => ShopInfo = await _shop.GetInfoAsync());
}

/// <summary>Assistant IA — réutilise POST chat (mêmes outils que le web).</summary>
public partial class AssistantViewModel : BaseViewModel
{
    private readonly ChatApiService _chat;

    public ObservableCollection<ChatMessageItem> Messages { get; } = new();

    [ObservableProperty] private string _input = string.Empty;
    [ObservableProperty] private ChatPendingAction? _pendingAction;

    private string? _sessionId;

    public AssistantViewModel(ChatApiService chat) => _chat = chat;

    [RelayCommand]
    private Task SendAsync() => RunSafeAsync(async () =>
    {
        var text = Input?.Trim();
        if (string.IsNullOrEmpty(text)) return;

        Messages.Add(new ChatMessageItem(text, IsUser: true));
        Input = string.Empty;

        var response = await _chat.SendAsync(new ChatRequestDto(_sessionId, text, null));
        _sessionId = response.SessionId;
        Messages.Add(new ChatMessageItem(response.Message, IsUser: false));
        PendingAction = response.PendingAction;
    });

    [RelayCommand]
    private Task ConfirmActionAsync() => RunSafeAsync(async () =>
    {
        if (PendingAction is null) return;

        var response = await _chat.SendAsync(new ChatRequestDto(_sessionId, "oui, confirme", null));
        _sessionId = response.SessionId;
        Messages.Add(new ChatMessageItem(response.Message, IsUser: false));
        PendingAction = response.PendingAction;
    });

    [RelayCommand]
    private void DismissAction() => PendingAction = null;
}

public sealed record ChatMessageItem(string Text, bool IsUser);
