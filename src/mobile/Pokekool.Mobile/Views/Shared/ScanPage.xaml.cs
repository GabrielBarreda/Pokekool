using Pokekool.Mobile.ViewModels;

namespace Pokekool.Mobile.Views.Shared;

public partial class ScanPage : ContentPage
{
    public ScanPage(ScanViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }
}
