using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Extensions.Logging;
using Pokekool.Mobile.Services;
using Pokekool.Mobile.ViewModels;
using Pokekool.Mobile.Views;
using Pokekool.Mobile.Views.Admin;
using Pokekool.Mobile.Views.Client;
using Pokekool.Mobile.Views.Shared;

namespace Pokekool.Mobile;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder.UseMauiApp<App>();

#if DEBUG
        builder.Logging.AddDebug();
#endif

        RegisterServices(builder.Services);
        RegisterViewModels(builder.Services);
        RegisterViews(builder.Services);

        return builder.Build();
    }

    private static void RegisterServices(IServiceCollection services)
    {
        services.AddSingleton(WeakReferenceMessenger.Default);
        services.AddSingleton<SessionService>();
        services.AddSingleton<AuthService>();
        services.AddSingleton<CardDisplayLanguageService>();
        services.AddTransient<AuthHttpMessageHandler>();
        services.AddTransient<DisplayLanguageHttpMessageHandler>();

        services.AddHttpClient(ApiClient.HttpClientName, client =>
            {
                client.BaseAddress = new Uri(ApiConfig.BaseUrl);
                // 180s : couvre le 1er chargement du modèle vision en VRAM (cold start Ollama).
                client.Timeout = TimeSpan.FromSeconds(180);
            })
            .AddHttpMessageHandler<AuthHttpMessageHandler>()
            .AddHttpMessageHandler<DisplayLanguageHttpMessageHandler>();

        services.AddSingleton<ApiClient>();

        services.AddSingleton<PokemonApiService>();
        services.AddSingleton<MarketApiService>();
        services.AddSingleton<CollectionApiService>();
        services.AddSingleton<AdminShopApiService>();
        services.AddSingleton<StorageApiService>();
        services.AddSingleton<ShopClientApiService>();
        services.AddSingleton<UsersApiService>();
        services.AddSingleton<AccountApiService>();
        services.AddSingleton<ChatApiService>();
    }

    private static void RegisterViewModels(IServiceCollection services)
    {
        services.AddTransient<LoginViewModel>();

        // Partagé Admin / Client
        services.AddTransient<CollectionViewModel>();
        services.AddTransient<ScanViewModel>();
        services.AddTransient<CardDetailViewModel>();

        // Admin — Pokémon
        services.AddTransient<BourseViewModel>();
        services.AddTransient<SetCardsViewModel>();
        services.AddTransient<CardsForSaleViewModel>();

        // Admin — Boutique
        services.AddTransient<PresentoirViewModel>();
        services.AddTransient<StockViewModel>();
        services.AddTransient<PlanningViewModel>();
        services.AddTransient<StorageViewModel>();
        services.AddTransient<PurchaseRequestsViewModel>();
        services.AddTransient<ShopInfoViewModel>();
        services.AddTransient<AssistantViewModel>();

        // Admin — Paramètres
        services.AddTransient<ProfileViewModel>();
        services.AddTransient<UsersAdminViewModel>();
        services.AddTransient<RolesViewModel>();
        services.AddTransient<ProvidersViewModel>();

        // Client
        services.AddTransient<CatalogViewModel>();
        services.AddTransient<ProductDetailViewModel>();
        services.AddTransient<BookAppointmentViewModel>();
        services.AddTransient<MyAppointmentsViewModel>();
    }

    private static void RegisterViews(IServiceCollection services)
    {
        services.AddTransient<LoginPage>();

        services.AddTransient<CollectionPage>();
        services.AddTransient<ScanPage>();
        services.AddTransient<CardDetailPage>();

        services.AddTransient<BoursePage>();
        services.AddTransient<SetCardsPage>();
        services.AddTransient<CardsForSalePage>();

        services.AddTransient<PresentoirPage>();
        services.AddTransient<StockPage>();
        services.AddTransient<PlanningPage>();
        services.AddTransient<StoragePage>();
        services.AddTransient<PurchaseRequestsPage>();
        services.AddTransient<ShopInfoPage>();
        services.AddTransient<AssistantPage>();

        services.AddTransient<ProfilePage>();
        services.AddTransient<UsersAdminPage>();
        services.AddTransient<RolesPage>();
        services.AddTransient<ProvidersPage>();

        services.AddTransient<CatalogPage>();
        services.AddTransient<ProductDetailPage>();
        services.AddTransient<BookAppointmentPage>();
        services.AddTransient<MyAppointmentsPage>();
        services.AddTransient<ShopMessagesPage>();
        services.AddTransient<AlertsPage>();
    }
}
