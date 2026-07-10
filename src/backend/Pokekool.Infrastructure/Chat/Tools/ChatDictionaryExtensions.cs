namespace Pokekool.Infrastructure.Chat.Tools;

/// <summary>
/// GetValueOrDefault sur IDictionary (la version BCL n'existe que pour IReadOnlyDictionary,
/// que IDictionary n'implémente pas). Utilisé par les outils du chatbot.
/// </summary>
internal static class ChatDictionaryExtensions
{
    public static object? GetValueOrDefault(this IDictionary<string, object?> dict, string key)
        => dict.TryGetValue(key, out var value) ? value : null;
}
