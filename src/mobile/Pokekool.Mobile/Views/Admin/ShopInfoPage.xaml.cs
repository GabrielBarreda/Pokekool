using Pokekool.Mobile.ViewModels;

namespace Pokekool.Mobile.Views.Admin;

public partial class ShopInfoPage : ContentPage
{
    private readonly ShopInfoViewModel _vm;
    private bool _loaded;

    public ShopInfoPage(ShopInfoViewModel vm)
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
