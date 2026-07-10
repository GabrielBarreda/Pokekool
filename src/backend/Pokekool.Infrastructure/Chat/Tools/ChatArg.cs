using System.Globalization;
using System.Text.Json;

namespace Pokekool.Infrastructure.Chat.Tools;

/// <summary>
/// Extraction robuste des arguments d'outils. Les valeurs peuvent être des primitifs
/// (appel direct du LLM) ou des <see cref="JsonElement"/> (rechargées depuis un PendingAction JSON).
/// </summary>
internal static class ChatArg
{
    public static string? String(IDictionary<string, object?> args, string key)
    {
        if (!args.TryGetValue(key, out var v) || v is null) return null;
        if (v is JsonElement el)
            return el.ValueKind switch
            {
                JsonValueKind.String => el.GetString(),
                JsonValueKind.Null => null,
                _ => el.GetRawText()
            };
        return v.ToString();
    }

    public static int Int(IDictionary<string, object?> args, string key, int fallback = 0)
    {
        var s = Raw(args, key);
        return s is not null && int.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out var v) ? v : fallback;
    }

    public static decimal? Decimal(IDictionary<string, object?> args, string key)
    {
        var s = Raw(args, key);
        return s is not null && decimal.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out var v) ? v : null;
    }

    public static bool Bool(IDictionary<string, object?> args, string key, bool fallback = false)
    {
        if (!args.TryGetValue(key, out var v) || v is null) return fallback;
        if (v is JsonElement el)
            return el.ValueKind switch
            {
                JsonValueKind.True => true,
                JsonValueKind.False => false,
                JsonValueKind.String => bool.TryParse(el.GetString(), out var b) && b,
                _ => fallback
            };
        return v is bool b2 ? b2 : bool.TryParse(v.ToString(), out var b3) ? b3 : fallback;
    }

    public static Guid? Guid(IDictionary<string, object?> args, string key)
    {
        var s = String(args, key);
        return System.Guid.TryParse(s, out var g) ? g : null;
    }

    /// <summary>Représentation string invariante d'une valeur numérique/texte, quel que soit le type source.</summary>
    private static string? Raw(IDictionary<string, object?> args, string key)
    {
        if (!args.TryGetValue(key, out var v) || v is null) return null;
        if (v is JsonElement el)
            return el.ValueKind switch
            {
                JsonValueKind.Number => el.GetRawText(),
                JsonValueKind.String => el.GetString(),
                JsonValueKind.Null => null,
                _ => el.GetRawText()
            };
        return v switch
        {
            IFormattable f => f.ToString(null, CultureInfo.InvariantCulture),
            _ => v.ToString()
        };
    }
}
