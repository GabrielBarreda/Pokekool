using Pokekool.Mobile.ViewModels;

namespace Pokekool.Mobile.Views.Client;

public partial class CatalogPage : ContentPage
{
    private readonly CatalogViewModel _vm;
    private bool _loaded;

    public CatalogPage(CatalogViewModel vm)
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
