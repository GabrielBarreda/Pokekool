using Pokekool.Mobile.ViewModels;

namespace Pokekool.Mobile.Views.Shared;

public partial class CollectionPage : ContentPage
{
    private readonly CollectionViewModel _vm;
    private bool _loaded;

    public CollectionPage(CollectionViewModel vm)
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
