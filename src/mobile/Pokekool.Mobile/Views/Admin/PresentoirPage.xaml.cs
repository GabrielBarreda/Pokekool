using Pokekool.Mobile.ViewModels;

namespace Pokekool.Mobile.Views.Admin;

public partial class PresentoirPage : ContentPage
{
    private readonly PresentoirViewModel _vm;
    private bool _loaded;

    public PresentoirPage(PresentoirViewModel vm)
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
