using Pokekool.Mobile.ViewModels;

namespace Pokekool.Mobile.Views.Admin;

public partial class SetCardsPage : ContentPage
{
    public SetCardsPage(SetCardsViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }
}
