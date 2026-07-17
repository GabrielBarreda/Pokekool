using Pokekool.Mobile.ViewModels;

namespace Pokekool.Mobile.Views.Admin;

public partial class BoursePage : ContentPage
{
    private readonly BourseViewModel _vm;
    private bool _loaded;

    public BoursePage(BourseViewModel vm)
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
