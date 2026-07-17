using Pokekool.Mobile.ViewModels;

namespace Pokekool.Mobile.Views.Client;

public partial class ProductDetailPage : ContentPage
{
    public ProductDetailPage(ProductDetailViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }
}
