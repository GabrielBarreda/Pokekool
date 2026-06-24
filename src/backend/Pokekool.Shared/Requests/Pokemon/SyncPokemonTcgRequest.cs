namespace Pokekool.Shared.Requests.Pokemon;

public sealed record SyncPokemonTcgRequest(bool ForceRefresh = false);
