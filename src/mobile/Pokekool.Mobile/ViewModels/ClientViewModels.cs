using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Pokekool.Mobile.Services;
using Pokekool.Shared.Dtos.Shop;

namespace Pokekool.Mobile.ViewModels;

// ============================================================
// Interface CLIENT — onglet Boutique
// ============================================================

/// <summary>Catalogue boutique (présentoir disponible côté client).</summary>
public partial class CatalogViewModel : BaseViewModel
{
    private readonly ShopClientApiService _shop;

    public ObservableCollection<ShopProductDto> Products { get; } = new();

    [ObservableProperty] private string? _search;

    public CatalogViewModel(ShopClientApiService shop) => _shop = shop;

    [RelayCommand]
    public Task RefreshAsync() => RunSafeAsync(async () =>
    {
        Products.Clear();
        var page = await _shop.GetProductsAsync(search: Search, pageSize: 60);
        foreach (var p in page.Items) Products.Add(p);
    });

    [RelayCommand]
    private async Task OpenProductAsync(ShopProductDto product)
        => await Shell.Current.GoToAsync("product-detail", new Dictionary<string, object>
        {
            ["product"] = product,
        });
}

/// <summary>Détail produit + demande d'achat.</summary>
[QueryProperty(nameof(Product), "product")]
public partial class ProductDetailViewModel : BaseViewModel
{
    private readonly ShopClientApiService _shop;

    [ObservableProperty] private ShopProductDto? _product;
    [ObservableProperty] private string? _message;
    [ObservableProperty] private string? _confirmation;

    public ProductDetailViewModel(ShopClientApiService shop) => _shop = shop;

    [RelayCommand]
    private Task RequestPurchaseAsync() => RunSafeAsync(async () =>
    {
        if (Product is null) return;
        await _shop.RequestPurchaseAsync(Product.Id, Message);
        Confirmation = "Demande envoyée ✔ — la boutique vous recontactera.";
        Message = null;
    });
}

/// <summary>Prise de RDV : service + date → créneaux → réservation.</summary>
public partial class BookAppointmentViewModel : BaseViewModel
{
    // Codes acceptés par l'API (MapServiceType) — mêmes valeurs que le front web client.
    private static readonly string[] Services = { "pre_grading", "restoration" };

    private readonly ShopClientApiService _shop;

    public ObservableCollection<AvailableSlotDto> Slots { get; } = new();
    public IReadOnlyList<string> ServiceTypes => Services;

    [ObservableProperty] private string _serviceType = Services[0];
    [ObservableProperty] private DateTime _date = DateTime.Today.AddDays(1);
    [ObservableProperty] private AvailableSlotDto? _selectedSlot;
    [ObservableProperty] private string? _notes;
    [ObservableProperty] private string? _confirmation;

    public BookAppointmentViewModel(ShopClientApiService shop) => _shop = shop;

    [RelayCommand]
    public Task LoadSlotsAsync() => RunSafeAsync(async () =>
    {
        Confirmation = null;
        SelectedSlot = null;
        Slots.Clear();
        var slots = await _shop.GetAvailableSlotsAsync(ServiceType, Date.ToString("yyyy-MM-dd"));
        foreach (var s in slots.Where(s => s.Available)) Slots.Add(s);
    });

    [RelayCommand]
    private void SelectSlot(AvailableSlotDto slot) => SelectedSlot = slot;

    [RelayCommand]
    private Task BookAsync() => RunSafeAsync(async () =>
    {
        if (SelectedSlot is null)
        {
            ErrorMessage = "Choisissez un créneau.";
            return;
        }

        var scheduledAt = DateTimeOffset.Parse($"{SelectedSlot.Date}T{SelectedSlot.Time}:00");
        var appointment = await _shop.CreateAppointmentAsync(ServiceType, scheduledAt, Notes);
        Confirmation = $"RDV confirmé le {appointment.ScheduledAt:dddd d MMMM à HH:mm} ✔";
        Slots.Clear();
        SelectedSlot = null;
        Notes = null;
    });
}

/// <summary>Mes RDV + annulation (pending, >24h).</summary>
public partial class MyAppointmentsViewModel : BaseViewModel
{
    private readonly ShopClientApiService _shop;

    public ObservableCollection<ShopAppointmentDto> Appointments { get; } = new();

    public MyAppointmentsViewModel(ShopClientApiService shop) => _shop = shop;

    [RelayCommand]
    public Task RefreshAsync() => RunSafeAsync(async () =>
    {
        Appointments.Clear();
        var page = await _shop.GetMyAppointmentsAsync(pageSize: 50);
        foreach (var a in page.Items.OrderBy(a => a.ScheduledAt)) Appointments.Add(a);
    });

    [RelayCommand]
    private async Task CancelAsync(ShopAppointmentDto appointment)
    {
        var ok = await Shell.Current.DisplayAlertAsync(
            "Annuler le RDV",
            $"Annuler le rendez-vous du {appointment.ScheduledAt:d MMMM à HH:mm} ?",
            "Oui, annuler", "Non");
        if (!ok) return;

        await RunSafeAsync(async () =>
        {
            await _shop.CancelAppointmentAsync(appointment.Id);
            Appointments.Remove(appointment);
        });
    }
}
