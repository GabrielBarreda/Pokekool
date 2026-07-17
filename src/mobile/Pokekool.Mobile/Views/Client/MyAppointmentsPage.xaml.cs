using Pokekool.Mobile.ViewModels;

namespace Pokekool.Mobile.Views.Client;

public partial class MyAppointmentsPage : ContentPage
{
    private readonly MyAppointmentsViewModel _vm;
    private bool _loaded;

    public MyAppointmentsPage(MyAppointmentsViewModel vm)
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
