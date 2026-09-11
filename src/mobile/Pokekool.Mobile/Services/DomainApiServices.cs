using Pokekool.Shared.Dtos.Auth;
using Pokekool.Shared.Dtos.Authorization;
using Pokekool.Shared.Dtos.Chat;
using Pokekool.Shared.Dtos.Collection;
using Pokekool.Shared.Dtos.Common;
using Pokekool.Shared.Dtos.Pokemon;
using Pokekool.Shared.Dtos.Shop;
using Pokekool.Shared.Dtos.Storage;
using Pokekool.Shared.Dtos.Users;

namespace Pokekool.Mobile.Services;

// ============================================================
// Services API métier — un service par domaine, routes api/v1 relatives.
// Les DTOs viennent exclusivement de Pokekool.Shared (zéro duplication).
// ============================================================

public sealed class PokemonApiService
{
    private readonly ApiClient _api;
    public PokemonApiService(ApiClient api) => _api = api;

    public Task<IReadOnlyList<PokemonSeriesDto>> GetSeriesAsync(CancellationToken ct = default)
        => _api.GetAsync<IReadOnlyList<PokemonSeriesDto>>("pokemon/series", ct);

    public Task<IReadOnlyList<PokemonSetDto>> GetSetsAsync(Guid? seriesId = null, CancellationToken ct = default)
        => _api.GetAsync<IReadOnlyList<PokemonSetDto>>(
            "pokemon/sets" + (seriesId is null ? "" : $"?seriesId={seriesId}"), ct);

    public Task<PokemonCardDto> GetCardAsync(Guid id, CancellationToken ct = default)
        => _api.GetAsync<PokemonCardDto>($"pokemon/cards/{id}", ct);
}

public sealed class MarketApiService
{
    private readonly ApiClient _api;
    public MarketApiService(ApiClient api) => _api = api;

    public Task<PagedResult<MarketCardDto>> GetSetCardsAsync(
        Guid setId, string? search = null, string? rarity = null,
        int page = 1, int pageSize = 60, CancellationToken ct = default)
    {
        var route = $"market/sets/{setId}/cards?page={page}&pageSize={pageSize}";
        if (!string.IsNullOrWhiteSpace(search)) route += $"&search={Uri.EscapeDataString(search)}";
        if (!string.IsNullOrWhiteSpace(rarity)) route += $"&rarity={Uri.EscapeDataString(rarity)}";
        return _api.GetAsync<PagedResult<MarketCardDto>>(route, ct);
    }

    public Task<IReadOnlyList<CardGradingDto>> GetCardGradingAsync(Guid cardId, CancellationToken ct = default)
        => _api.GetAsync<IReadOnlyList<CardGradingDto>>($"market/cards/{cardId}/grading", ct);
}

public sealed class CollectionApiService
{
    private readonly ApiClient _api;
    public CollectionApiService(ApiClient api) => _api = api;

    public Task<PagedResult<CollectionCardDto>> GetCardsAsync(
        string? search = null, bool? ownedOnly = null, bool? favoritesOnly = null,
        int page = 1, int pageSize = 60, CancellationToken ct = default)
    {
        var route = $"collection/cards?page={page}&pageSize={pageSize}";
        if (!string.IsNullOrWhiteSpace(search)) route += $"&search={Uri.EscapeDataString(search)}";
        if (ownedOnly is true) route += "&ownedOnly=true";
        if (favoritesOnly is true) route += "&favoritesOnly=true";
        return _api.GetAsync<PagedResult<CollectionCardDto>>(route, ct);
    }

    public Task<CollectionStatsDto> GetStatsAsync(CancellationToken ct = default)
        => _api.GetAsync<CollectionStatsDto>("collection/stats", ct);

    public Task<CollectionValueDto> GetValueAsync(CancellationToken ct = default)
        => _api.GetAsync<CollectionValueDto>("collection/value", ct);

    public Task<CollectionCardStatusDto> ToggleOwnedAsync(string cardId, CancellationToken ct = default)
        => _api.PostAsync<CollectionCardStatusDto>($"collection/cards/{cardId}/toggle-owned", null, ct);

    public Task<CollectionCardStatusDto> SetFavoriteAsync(string cardId, bool isFavorite, CancellationToken ct = default)
        => _api.PostAsync<CollectionCardStatusDto>($"collection/cards/{cardId}/favorite", new { isFavorite }, ct);

    public Task<CollectionCardStatusDto> SetPriorityAsync(string cardId, bool isPriority, CancellationToken ct = default)
        => _api.PostAsync<CollectionCardStatusDto>($"collection/cards/{cardId}/priority", new { isPriority }, ct);

    /// <summary>POST collection/scan — multipart/form-data, champ "image".</summary>
    public async Task<ScanCollectionResult> ScanAsync(
        Stream imageStream, string fileName,
        IReadOnlyList<string>? excluded = null, CancellationToken ct = default)
    {
        using var content = new MultipartFormDataContent();
        var streamContent = new StreamContent(imageStream);
        streamContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("image/jpeg");
        content.Add(streamContent, "image", fileName);

        // Re-roll : libellés déjà rejetés, envoyés en champs répétés "excluded".
        if (excluded is not null)
            foreach (var label in excluded.Where(l => !string.IsNullOrWhiteSpace(l)))
                content.Add(new StringContent(label), "excluded");

        return await _api.PostMultipartAsync<ScanCollectionResult>("collection/scan", content, ct);
    }

    public Task<BulkAddResult> BulkAddOwnedAsync(
        IReadOnlyList<Guid> cardIds, IReadOnlyList<CardScanMatchDto> matches, CancellationToken ct = default)
        => _api.PostAsync<BulkAddResult>("collection/bulk-own", new { cardIds, matches }, ct);
}

public sealed class AdminShopApiService
{
    private readonly ApiClient _api;
    public AdminShopApiService(ApiClient api) => _api = api;

    // --- Produits (présentoir) ---
    public Task<PagedResult<ShopProductDto>> GetProductsAsync(
        string? type = null, bool? isAvailable = null, string? search = null,
        int page = 1, int pageSize = 50, CancellationToken ct = default)
    {
        var route = $"admin/shop/products?page={page}&pageSize={pageSize}";
        if (!string.IsNullOrWhiteSpace(type)) route += $"&type={Uri.EscapeDataString(type)}";
        if (isAvailable is not null) route += $"&isAvailable={isAvailable.Value.ToString().ToLowerInvariant()}";
        if (!string.IsNullOrWhiteSpace(search)) route += $"&search={Uri.EscapeDataString(search)}";
        return _api.GetAsync<PagedResult<ShopProductDto>>(route, ct);
    }

    public Task<ProductInterestDto> GetProductInterestAsync(Guid id, CancellationToken ct = default)
        => _api.GetAsync<ProductInterestDto>($"admin/shop/products/{id}/interest", ct);

    public Task<ShopProductDto> ToggleAvailabilityAsync(Guid id, bool isAvailable, CancellationToken ct = default)
        => _api.PatchAsync<ShopProductDto>($"admin/shop/products/{id}/availability", new { isAvailable }, ct);

    // --- Stock ---
    public Task<PagedResult<ShopStockDto>> GetStockAsync(
        string? type = null, string? search = null, int page = 1, int pageSize = 50, CancellationToken ct = default)
    {
        var route = $"admin/shop/stock?page={page}&pageSize={pageSize}";
        if (!string.IsNullOrWhiteSpace(type)) route += $"&type={Uri.EscapeDataString(type)}";
        if (!string.IsNullOrWhiteSpace(search)) route += $"&search={Uri.EscapeDataString(search)}";
        return _api.GetAsync<PagedResult<ShopStockDto>>(route, ct);
    }

    // --- RDV ---
    public Task<PagedResult<ShopAppointmentDto>> GetAppointmentsAsync(
        string? status = null, int page = 1, int pageSize = 100, CancellationToken ct = default)
    {
        var route = $"admin/shop/appointments?page={page}&pageSize={pageSize}";
        if (!string.IsNullOrWhiteSpace(status)) route += $"&status={Uri.EscapeDataString(status)}";
        return _api.GetAsync<PagedResult<ShopAppointmentDto>>(route, ct);
    }

    public Task<ShopAppointmentDto> UpdateAppointmentStatusAsync(Guid id, string status, CancellationToken ct = default)
        => _api.PutAsync<ShopAppointmentDto>($"admin/shop/appointments/{id}/status", new { status }, ct);

    // --- Demandes d'achat ---
    public Task<PagedResult<PurchaseRequestDto>> GetPurchaseRequestsAsync(
        string? status = null, int page = 1, int pageSize = 20, CancellationToken ct = default)
    {
        var route = $"admin/shop/purchase-requests?page={page}&pageSize={pageSize}";
        if (!string.IsNullOrWhiteSpace(status)) route += $"&status={Uri.EscapeDataString(status)}";
        return _api.GetAsync<PagedResult<PurchaseRequestDto>>(route, ct);
    }

    public Task UpdatePurchaseRequestStatusAsync(Guid id, string status, CancellationToken ct = default)
        => _api.PutAsync($"admin/shop/purchase-requests/{id}/status", new { status }, ct);

    // --- Infos boutique ---
    public Task<ShopDto> GetInfoAsync(CancellationToken ct = default)
        => _api.GetAsync<ShopDto>("admin/shop/info", ct);
}

public sealed class StorageApiService
{
    private readonly ApiClient _api;
    public StorageApiService(ApiClient api) => _api = api;

    public Task<IReadOnlyList<StorageRoomDto>> GetRoomsAsync(CancellationToken ct = default)
        => _api.GetAsync<IReadOnlyList<StorageRoomDto>>("admin/storage/rooms", ct);

    public Task<IReadOnlyList<StorageBoxDto>> GetBoxesAsync(Guid roomId, CancellationToken ct = default)
        => _api.GetAsync<IReadOnlyList<StorageBoxDto>>($"admin/storage/boxes?roomId={roomId}", ct);

    public Task<StorageBoxLookupDto> LookupAsync(string code, CancellationToken ct = default)
        => _api.GetAsync<StorageBoxLookupDto>($"admin/storage/lookup?code={Uri.EscapeDataString(code)}", ct);

    public Task<IReadOnlyList<StorageMovementEventDto>> GetPendingMovementsAsync(CancellationToken ct = default)
        => _api.GetAsync<IReadOnlyList<StorageMovementEventDto>>("admin/storage/movements/pending", ct);

    public Task<StorageMovementEventDto> ConfirmMovementAsync(Guid id, bool confirm, CancellationToken ct = default)
        => _api.PostAsync<StorageMovementEventDto>($"admin/storage/movements/{id}/confirm", new { confirm }, ct);
}

public sealed class ShopClientApiService
{
    private readonly ApiClient _api;
    public ShopClientApiService(ApiClient api) => _api = api;

    public Task<PagedResult<ShopProductDto>> GetProductsAsync(
        string? type = null, decimal? priceMax = null, string? search = null,
        int page = 1, int pageSize = 24, CancellationToken ct = default)
    {
        var route = $"shop/products?page={page}&pageSize={pageSize}";
        if (!string.IsNullOrWhiteSpace(type)) route += $"&type={Uri.EscapeDataString(type)}";
        if (priceMax is not null) route += $"&price_max={priceMax.Value}";
        if (!string.IsNullOrWhiteSpace(search)) route += $"&search={Uri.EscapeDataString(search)}";
        return _api.GetAsync<PagedResult<ShopProductDto>>(route, ct);
    }

    public Task<Guid> RequestPurchaseAsync(Guid productId, string? message, CancellationToken ct = default)
        => _api.PostAsync<Guid>($"shop/products/{productId}/request-purchase", new { message }, ct);

    public Task<IReadOnlyList<AvailableSlotDto>> GetAvailableSlotsAsync(
        string serviceType, string date, CancellationToken ct = default)
        => _api.GetAsync<IReadOnlyList<AvailableSlotDto>>(
            $"shop/appointments/available-slots?service_type={Uri.EscapeDataString(serviceType)}&date={Uri.EscapeDataString(date)}", ct);

    public Task<ShopAppointmentDto> CreateAppointmentAsync(
        string serviceType, DateTimeOffset scheduledAt, string? notesClient, CancellationToken ct = default)
        => _api.PostAsync<ShopAppointmentDto>("shop/appointments", new { serviceType, scheduledAt, notesClient }, ct);

    public Task<PagedResult<ShopAppointmentDto>> GetMyAppointmentsAsync(
        int page = 1, int pageSize = 20, CancellationToken ct = default)
        => _api.GetAsync<PagedResult<ShopAppointmentDto>>($"shop/appointments/mine?page={page}&pageSize={pageSize}", ct);

    public Task CancelAppointmentAsync(Guid id, CancellationToken ct = default)
        => _api.DeleteAsync($"shop/appointments/{id}/cancel", ct);
}

public sealed class UsersApiService
{
    private readonly ApiClient _api;
    public UsersApiService(ApiClient api) => _api = api;

    public Task<PagedResult<Pokekool.Shared.Dtos.Users.UserSummaryDto>> GetUsersAsync(
        string? search = null, int page = 1, int pageSize = 30, CancellationToken ct = default)
    {
        var route = $"users?page={page}&pageSize={pageSize}";
        if (!string.IsNullOrWhiteSpace(search)) route += $"&search={Uri.EscapeDataString(search)}";
        return _api.GetAsync<PagedResult<Pokekool.Shared.Dtos.Users.UserSummaryDto>>(route, ct);
    }

    public Task ActivateAsync(Guid id, CancellationToken ct = default)
        => _api.PatchAsync<object>($"users/{id}/activate", null, ct);

    public Task DeactivateAsync(Guid id, CancellationToken ct = default)
        => _api.PatchAsync<object>($"users/{id}/deactivate", null, ct);

    public Task<IReadOnlyList<RoleDto>> GetRolesAsync(CancellationToken ct = default)
        => _api.GetAsync<IReadOnlyList<RoleDto>>("roles", ct);
}

public sealed class AccountApiService
{
    private readonly ApiClient _api;
    public AccountApiService(ApiClient api) => _api = api;

    public Task<CurrentUserDto> GetMeAsync(CancellationToken ct = default)
        => _api.GetAsync<CurrentUserDto>("me", ct);

    public Task ChangePasswordAsync(string currentPassword, string newPassword, CancellationToken ct = default)
        => _api.PostAsync("me/change-password", new ChangePasswordRequest(currentPassword, newPassword), ct);

    public Task<ProviderSettingsInfo> GetProviderSettingsAsync(string provider, CancellationToken ct = default)
        => _api.GetAsync<ProviderSettingsInfo>($"admin/providers/{provider}/settings", ct);

    public Task UpsertProviderSettingAsync(string provider, string key, string value, CancellationToken ct = default)
        => _api.PutAsync($"admin/providers/{provider}/settings/{Uri.EscapeDataString(key)}", new { value }, ct);
}

/// <summary>Miroir de ProviderSettingsDto (Application) — pas exposé dans Pokekool.Shared.</summary>
public sealed record ProviderSettingsInfo(
    string Provider,
    IReadOnlyList<ProviderSettingKeyInfo> Keys);

/// <summary>IsConfigured = clé présente en base ; Value fourni uniquement pour les clés non sensibles.</summary>
public sealed record ProviderSettingKeyInfo(string Key, bool IsConfigured, string? Value = null);

public sealed class ChatApiService
{
    private readonly ApiClient _api;
    public ChatApiService(ApiClient api) => _api = api;

    public Task<ChatResponseDto> SendAsync(ChatRequestDto request, CancellationToken ct = default)
        => _api.PostAsync<ChatResponseDto>("chat", request, ct);
}
