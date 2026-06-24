using HtmlAgilityPack;
using System.Text.RegularExpressions;

namespace Pokekool.Infrastructure.DataProviders.Scraping;

/// <summary>
/// Nettoie le HTML brut pour réduire la taille avant envoi au LLM.
/// Supprime scripts, styles, SVG, commentaires et attributs non sémantiques.
/// </summary>
public static class HtmlCleaner
{
    // Attributs conservés car sémantiquement utiles
    private static readonly HashSet<string> AllowedAttributes =
        new(StringComparer.OrdinalIgnoreCase) { "class", "id" };

    // Tags complètement supprimés (contenu inclus)
    private static readonly HashSet<string> BlockedTags =
        new(StringComparer.OrdinalIgnoreCase) { "script", "style", "svg", "noscript", "iframe", "img", "picture", "video", "audio" };

    private static readonly Regex MultipleWhitespace = new(@"\s{2,}", RegexOptions.Compiled);

    /// <summary>
    /// Nettoie le HTML brut et tronque à <paramref name="maxChars"/> caractères.
    /// </summary>
    public static string Clean(string rawHtml, int maxChars = 8000)
    {
        if (string.IsNullOrWhiteSpace(rawHtml))
            return string.Empty;

        var doc = new HtmlDocument();
        doc.LoadHtml(rawHtml);

        // 1. Supprimer les commentaires HTML
        var comments = doc.DocumentNode.SelectNodes("//comment()");
        if (comments is not null)
            foreach (var comment in comments.ToList())
                comment.Remove();

        // 2. Supprimer les tags bloqués (contenu inclus)
        foreach (var tag in BlockedTags)
        {
            var nodes = doc.DocumentNode.SelectNodes($"//{tag}");
            if (nodes is null) continue;
            foreach (var node in nodes.ToList())
                node.Remove();
        }

        // 3. Nettoyer les attributs : garder seulement class, id, data-*
        var allNodes = doc.DocumentNode.SelectNodes("//*");
        if (allNodes is not null)
        {
            foreach (var node in allNodes)
            {
                var toRemove = node.Attributes
                    .Where(a => !AllowedAttributes.Contains(a.Name) && !a.Name.StartsWith("data-", StringComparison.OrdinalIgnoreCase))
                    .ToList();
                foreach (var attr in toRemove)
                    node.Attributes.Remove(attr);
            }
        }

        // 4. Extraire le texte nettoyé
        var text = doc.DocumentNode.InnerText;

        // 5. Réduire les espaces blancs multiples
        text = MultipleWhitespace.Replace(text, " ").Trim();

        // 6. Tronquer si nécessaire
        if (text.Length > maxChars)
            text = text[..maxChars];

        return text;
    }
}
