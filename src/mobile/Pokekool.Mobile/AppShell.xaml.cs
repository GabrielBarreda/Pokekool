using Pokekool.Mobile.Services;
using Pokekool.Mobile.Views.Admin;
using Pokekool.Mobile.Views.Client;
using Pokekool.Mobile.Views.Shared;

namespace Pokekool.Mobile;

public partial class AppShell : Shell
{
    private readonly SessionService _session;

    public AppShell(SessionService session)
    {
        InitializeComponent();
        _session = session;

        // Pages de détail (navigation push, hors TabBar)
        Routing.RegisterRoute("set-cards", typeof(SetCardsPage));
        Routing.RegisterRoute("card-detail", typeof(CardDetailPage));
        Routing.RegisterRoute("product-detail", typeof(ProductDetailPage));
    }

    /// <summary>Navigue vers le bon TabBar selon le rôle, ou vers le login.</summary>
    public async Task NavigateAfterAuthAsync(bool authenticated)
    {
        if (!authenticated || !_session.IsAuthenticated)
        {
            await GoToAsync("//login");
            return;
        }

        await GoToAsync(_session.IsAdmin ? "//admin" : "//client");
    }
}
