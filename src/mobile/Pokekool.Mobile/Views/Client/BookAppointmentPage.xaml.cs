using Pokekool.Mobile.ViewModels;

namespace Pokekool.Mobile.Views.Client;

public partial class BookAppointmentPage : ContentPage
{
    public BookAppointmentPage(BookAppointmentViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }
}
