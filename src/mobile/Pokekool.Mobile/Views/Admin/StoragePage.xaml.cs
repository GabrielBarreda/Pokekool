using Pokekool.Mobile.ViewModels;

namespace Pokekool.Mobile.Views.Admin;

public partial class StoragePage : ContentPage
{
    private readonly StorageViewModel _vm;
    private bool _loaded;

    public StoragePage(StorageViewModel vm)
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
