using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.Input;
using Pokekool.Mobile.Services;
using Pokekool.Shared.Dtos.Shop;

namespace Pokekool.Mobile.ViewModels;

/// <summary>Cartes à vendre — vue des produits boutique de type carte (admin/shop/products).</summary>
public partial class CardsForSaleViewModel : BaseViewModel
{
    private readonly AdminShopApiService _shop;

    public ObservableCollection<ShopProductDto> Products { get; } = new();

    public CardsForSaleViewModel(AdminShopApiService shop) => _shop = shop;

    [RelayCommand]
    public Task RefreshAsync() => RunSafeAsync(async () =>
    {
        Products.Clear();
        // Cartes uniquement (brutes + gradées), disponibles à la vente.
        foreach (var type in new[] { "CardRaw", "CardGraded" })
        {
            var page = await _shop.GetProductsAsync(type: type, isAvailable: true, pageSize: 100);
            foreach (var p in page.Items)
                Products.Add(p);
        }
    });
}
