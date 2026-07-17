using Pokekool.Mobile.ViewModels;

namespace Pokekool.Mobile.Views.Shared;

public partial class CardDetailPage : ContentPage
{
    public CardDetailPage(CardDetailViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }
}
