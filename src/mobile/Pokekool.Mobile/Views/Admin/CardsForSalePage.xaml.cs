using Pokekool.Mobile.ViewModels;

namespace Pokekool.Mobile.Views.Admin;

public partial class CardsForSalePage : ContentPage
{
    private readonly CardsForSaleViewModel _vm;
    private bool _loaded;

    public CardsForSalePage(CardsForSaleViewModel vm)
    {
        InitializeComponent();
        BindingContext = _vm = vm;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (_loaded) return;
        _loaded = true;
        await _vm.RefreshAsync();
    }
}
