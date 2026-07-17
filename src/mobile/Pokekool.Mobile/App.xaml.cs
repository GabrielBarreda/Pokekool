using Pokekool.Mobile.Services;

namespace Pokekool.Mobile;

public partial class App : Application
{
    private readonly AuthService _auth;
    private readonly SessionService _session;

    public App(AuthService auth, SessionService session)
    {
        InitializeComponent();
        _auth = auth;
        _session = session;
    }

    protected override Window CreateWindow(IActivationState? activationState)
        => new(new AppShell(_session));

    protected override async void OnStart()
    {
        base.OnStart();

        // Tentative de restauration de session via le refresh token (SecureStorage).
        var restored = await _auth.TryRestoreSessionAsync();
        if (Shell.Current is AppShell shell)
            await shell.NavigateAfterAuthAsync(restored);
    }
}
