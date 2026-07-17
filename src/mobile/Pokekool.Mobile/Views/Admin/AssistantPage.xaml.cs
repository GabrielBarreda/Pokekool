using Pokekool.Mobile.ViewModels;

namespace Pokekool.Mobile.Views.Admin;

public partial class AssistantPage : ContentPage
{
    public AssistantPage(AssistantViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }
}
