using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Extensions.Logging;

namespace Pokekool.Infrastructure.DataProviders.CardLadder;

/// <summary>
/// Client HTTP pour l'API Firestore publique de CardLadder.
///
/// CardLadder utilise Firebase Firestore (projet : cardladder-71d53) avec des règles
/// en lecture publique — aucune authentification requise.
///
/// Stratégie en deux phases :
///   Phase 1 — Discovery : POST runQuery pour trouver les documents de la carte (un doc = un grade)
///   Phase 2 — OtherGrades : GET des documents liés via le champ "otherGrades" (tous les grades
///              de la même carte, découverts depuis le premier résultat)
///
/// Structure d'un document Firestore CardLadder :
///   currentValue  (double)   → prix "Last Sold" estimé pour ce grade
///   company       (string)   → société de gradation : "PSA", "BGS", "CGC", "SGC"
///   grade         (double)   → note : 10, 9.5, 9, etc.
///   pop           (integer)  → population (nb de cartes gradées à ce niveau)
///   otherGrades   (array)    → liste de doc IDs des autres grades de la même carte
///   name          (string)   → nom de la carte (ex: "Charizard")
///   number        (string)   → numéro dans le set (ex: "4")
///   year          (integer)  → année de sortie
///
/// Note : les noms de champs Firestore sont déduits de l'investigation initiale + URLs publiques.
/// Si les noms changent, ajuster les constantes de champs en bas de ce fichier.
/// </summary>
public sealed class CardLadderFirestoreClient
{
    private const string FirestoreBase =
        "https://firestore.googleapis.com/v1/projects/cardladder-71d53/databases/(default)/documents";

    private const string Collection = "cards";

    // ── Noms de champs Firestore (confirmés via LogSampleDocumentAsync) ──────────────────────
    // Champs confirmés : number, year, gradingCompany, grade, currentValue, pop, otherGrades
    // Pas de champ "name" dans Firestore — on filtre par number + year uniquement
    private const string FieldNumber       = "number";
    private const string FieldYear         = "year";
    private const string FieldCompany      = "gradingCompany";   // ← était "company"
    private const string FieldGrade        = "grade";
    private const string FieldCurrentValue = "currentValue";
    private const string FieldPop          = "pop";
    private const string FieldOtherGrades  = "otherGrades";

    private static readonly JsonSerializerOptions JsonOpts = new() { PropertyNameCaseInsensitive = true };

    private readonly HttpClient                          _http;
    private readonly ILogger<CardLadderFirestoreClient>  _logger;

    public CardLadderFirestoreClient(
        HttpClient                         http,
        ILogger<CardLadderFirestoreClient> logger)
    {
        _http   = http;
        _logger = logger;
    }

    // ── Public API ────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Retourne tous les grades disponibles pour une carte (PSA 10, BGS 9.5, CGC 10, etc.)
    /// avec leur prix "Last Sold".
    ///
    /// Phase 1 : runQuery par nom + numéro pour obtenir les premiers documents.
    /// Phase 2 : GET les "otherGrades" des documents trouvés pour découvrir tous les grades.
    /// </summary>
    public async Task<List<CardLadderGradeDoc>> FindGradesForCardAsync(
        string          cardName,
        string          cardNumber,
        int?            setYear,
        CancellationToken ct)
    {
        _logger.LogDebug(
            "Firestore — recherche '{Name} #{Number}' (année={Year})",
            cardName, cardNumber, setYear);

        // Phase 1 : runQuery
        var initialDocs = await RunQueryAsync(cardName, cardNumber, setYear, ct);

        if (initialDocs.Count == 0)
        {
            _logger.LogWarning(
                "Firestore — 0 résultat pour '{Name} #{Number}' (collection={Collection}).",
                cardName, cardNumber, Collection);
            return [];
        }

        _logger.LogDebug(
            "Firestore — {Count} doc(s) trouvé(s) en Phase 1 pour '{Name} #{Number}'",
            initialDocs.Count, cardName, cardNumber);

        // Phase 2 : suivre otherGrades pour obtenir TOUS les grades
        var allDocs      = new Dictionary<string, CardLadderGradeDoc>(StringComparer.Ordinal);
        var otherIdQueue = new Queue<string>();

        foreach (var doc in initialDocs)
        {
            if (allDocs.TryAdd(doc.DocId, doc))
                EnqueueOtherGrades(doc, allDocs, otherIdQueue);
        }

        while (otherIdQueue.Count > 0 && allDocs.Count < 60) // sécurité anti-boucle infinie
        {
            var docId = otherIdQueue.Dequeue();
            if (allDocs.ContainsKey(docId)) continue;

            await Task.Delay(150, ct); // rate-limit léger
            var doc = await GetDocumentAsync(docId, ct);
            if (doc is not null && allDocs.TryAdd(doc.DocId, doc))
                EnqueueOtherGrades(doc, allDocs, otherIdQueue);
        }

        var result = allDocs.Values
            .Where(d => d.CurrentValue > 0 && !string.IsNullOrEmpty(d.Company))
            .ToList();

        _logger.LogInformation(
            "Firestore — '{Name} #{Number}' : {Count} grade(s) avec prix",
            cardName, cardNumber, result.Count);

        return result;
    }

    // ── Requêtes Firestore ────────────────────────────────────────────────────────────────

    /// <summary>
    /// Exécute un StructuredQuery Firestore pour trouver des documents par numéro + année de carte.
    /// Pas de filtre sur le nom : le champ "name" n'existe pas dans Firestore CardLadder.
    /// Retourne jusqu'à 50 documents.
    /// </summary>
    private async Task<List<CardLadderGradeDoc>> RunQueryAsync(
        string cardName, string cardNumber, int? setYear, CancellationToken ct)
    {
        var url = $"{FirestoreBase}:runQuery";

        var filters = new List<object>
        {
            BuildFieldFilter(FieldNumber, "EQUAL", new { stringValue = cardNumber }),
        };

        if (setYear.HasValue)
            filters.Add(BuildFieldFilter(FieldYear, "EQUAL", new { stringValue = setYear.Value.ToString() }));

        // Firestore : compositeFilter AND nécessite ≥2 filtres.
        // Si on n'a qu'un filtre (number sans year), on passe le fieldFilter directement.
        // BuildFieldFilter retourne { fieldFilter = {...} } qui est valide comme clause where seule.
        object whereClause = filters.Count == 1
            ? filters[0]
            : new { compositeFilter = new { op = "AND", filters = filters.ToArray() } };

        var body = new
        {
            structuredQuery = new
            {
                from  = new[] { new { collectionId = Collection } },
                where = whereClause,
                limit = 50
            }
        };

        try
        {
            using var resp = await _http.PostAsJsonAsync(url, body, ct);
            if (!resp.IsSuccessStatusCode)
            {
                _logger.LogWarning(
                    "Firestore runQuery — HTTP {Status} pour '{Name} #{Number}'",
                    (int)resp.StatusCode, cardName, cardNumber);
                return [];
            }

            var json = await resp.Content.ReadAsStringAsync(ct);
            return ParseRunQueryResponse(json);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogWarning(ex, "Firestore runQuery — exception pour '{Name} #{Number}'",
                cardName, cardNumber);
            return [];
        }
    }

    /// <summary>Récupère un document Firestore par son ID.</summary>
    private async Task<CardLadderGradeDoc?> GetDocumentAsync(string docId, CancellationToken ct)
    {
        var url = $"{FirestoreBase}/{Collection}/{docId}";
        try
        {
            using var resp = await _http.GetAsync(url, ct);
            if (!resp.IsSuccessStatusCode)
            {
                _logger.LogDebug("Firestore GET — HTTP {Status} pour doc '{Id}'",
                    (int)resp.StatusCode, docId);
                return null;
            }

            var json = await resp.Content.ReadAsStringAsync(ct);
            var node = JsonNode.Parse(json)?["fields"];
            return node is null ? null : ParseGradeDoc(docId, node);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogDebug(ex, "Firestore GET — exception pour doc '{Id}'", docId);
            return null;
        }
    }

    // ── Parsing Firestore ──────────────────────────────────────────────────────────────────

    private List<CardLadderGradeDoc> ParseRunQueryResponse(string json)
    {
        var results = new List<CardLadderGradeDoc>();
        try
        {
            var array = JsonNode.Parse(json)?.AsArray();
            if (array is null) return results;

            foreach (var item in array)
            {
                var docNode = item?["document"];
                if (docNode is null) continue;

                var docName = docNode["name"]?.GetValue<string>();
                if (docName is null) continue;

                // Extraire l'ID depuis le nom complet : .../documents/cards/{docId}
                var docId = docName.Split('/').Last();
                var fields = docNode["fields"];
                if (fields is null) continue;

                var doc = ParseGradeDoc(docId, fields);
                if (doc is not null) results.Add(doc);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Firestore — erreur parsing runQuery response");
        }
        return results;
    }

    private CardLadderGradeDoc? ParseGradeDoc(string docId, JsonNode fields)
    {
        try
        {
            var company      = GetString(fields,  FieldCompany);   // gradingCompany
            var grade        = GetDouble(fields,  FieldGrade);
            var currentValue = GetDouble(fields,  FieldCurrentValue);
            var pop          = GetInt(fields,     FieldPop);
            var number       = GetString(fields,  FieldNumber);
            var otherGrades  = GetStringArray(fields, FieldOtherGrades);

            return new CardLadderGradeDoc(
                DocId:        docId,
                CardNumber:   number,
                Company:      company?.ToUpperInvariant() ?? string.Empty,
                GradeValue:   (decimal)(grade ?? 0),
                CurrentValue: (decimal)(currentValue ?? 0),
                Pop:          pop,
                OtherGradeIds: otherGrades);
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Firestore — erreur parsing doc '{Id}'", docId);
            return null;
        }
    }

    // ── Helpers extraction Firestore typed values ─────────────────────────────────────────

    /// <summary>Extrait une string depuis un champ Firestore {"stringValue": "..."}.</summary>
    private static string? GetString(JsonNode fields, string fieldName)
        => fields[fieldName]?["stringValue"]?.GetValue<string>();

    /// <summary>Extrait un double depuis un champ Firestore {"doubleValue": X}, {"integerValue": "X"} ou {"stringValue": "g10"}.</summary>
    private static double? GetDouble(JsonNode fields, string fieldName)
    {
        var f = fields[fieldName];
        if (f is null) return null;
        if (f["doubleValue"]  is { } dv) return dv.GetValue<double>();
        if (f["integerValue"] is { } iv) return double.TryParse(iv.GetValue<string>(), out var d) ? d : null;
        if (f["stringValue"]  is { } sv)
        {
            var s = sv.GetValue<string>();
            // Format CardLadder : "g10", "g9", "g8.5" → grade numérique
            if (s.Length > 1 && (s[0] == 'g' || s[0] == 'G') &&
                double.TryParse(s[1..], System.Globalization.NumberStyles.Any,
                    System.Globalization.CultureInfo.InvariantCulture, out var gd))
                return gd;
            return double.TryParse(s, System.Globalization.NumberStyles.Any,
                System.Globalization.CultureInfo.InvariantCulture, out var sd) ? sd : null;
        }
        return null;
    }

    /// <summary>Extrait un int depuis un champ Firestore {"integerValue": "X"}.</summary>
    private static int? GetInt(JsonNode fields, string fieldName)
    {
        var f = fields[fieldName]?["integerValue"]?.GetValue<string>();
        return f is not null && int.TryParse(f, out var i) ? i : null;
    }

    /// <summary>
    /// Extrait un tableau d'IDs depuis le champ otherGrades de Firestore.
    /// Structure réelle : arrayValue de mapValue avec champs {id, ratio, currentValue, condition, label}.
    /// Supporte aussi les anciens stringValue directs pour robustesse.
    /// </summary>
    private static List<string>? GetStringArray(JsonNode fields, string fieldName)
    {
        var arr = fields[fieldName]?["arrayValue"]?["values"]?.AsArray();
        if (arr is null) return null;

        var result = new List<string>();
        foreach (var v in arr)
        {
            // Cas 1 : stringValue direct (ancien format potentiel)
            var sv = v?["stringValue"]?.GetValue<string>();
            if (sv is not null) { result.Add(sv); continue; }

            // Cas 2 : mapValue avec champ "id" (format actuel CardLadder)
            var id = v?["mapValue"]?["fields"]?["id"]?["stringValue"]?.GetValue<string>();
            if (id is not null) result.Add(id);
        }
        return result.Count > 0 ? result : null;
    }

    // ── Utilitaires ───────────────────────────────────────────────────────────────────────

    private static object BuildFieldFilter(string fieldPath, string op, object value)
        => new
        {
            fieldFilter = new
            {
                field = new { fieldPath },
                op,
                value
            }
        };

    private static void EnqueueOtherGrades(
        CardLadderGradeDoc             doc,
        Dictionary<string, CardLadderGradeDoc> known,
        Queue<string>                  queue)
    {
        if (doc.OtherGradeIds is null) return;
        foreach (var id in doc.OtherGradeIds)
            if (!known.ContainsKey(id))
                queue.Enqueue(id);
    }

    /// <summary>
    /// Récupère un document quelconque de la collection pour diagnostiquer
    /// les vrais noms de champs si les queries ne retournent rien.
    /// </summary>
    private async Task LogSampleDocumentAsync(CancellationToken ct)
    {
        if (!_logger.IsEnabled(Microsoft.Extensions.Logging.LogLevel.Debug)) return;

        try
        {
            var url  = $"{FirestoreBase}:runQuery";
            var body = new
            {
                structuredQuery = new
                {
                    from  = new[] { new { collectionId = Collection } },
                    limit = 1
                }
            };
            using var resp = await _http.PostAsJsonAsync(url, body, ct);
            if (!resp.IsSuccessStatusCode) return;

            var json    = await resp.Content.ReadAsStringAsync(ct);
            var fields  = JsonNode.Parse(json)?[0]?["document"]?["fields"];
            if (fields is null) return;

            var fieldNames = string.Join(", ", fields.AsObject().Select(kv => kv.Key));
            _logger.LogDebug(
                "Firestore sample doc — champs disponibles : [{Fields}]", fieldNames);
        }
        catch { /* best-effort */ }
    }
}

/// <summary>Document Firestore CardLadder désérialisé — représente un grade d'une carte.</summary>
public sealed record CardLadderGradeDoc(
    string          DocId,
    string?         CardNumber,
    string          Company,       // "PSA", "BGS", "CGC", "SGC"
    decimal         GradeValue,    // 10, 9.5, 9, etc.
    decimal         CurrentValue,  // prix "Last Sold" estimé
    int?            Pop,
    List<string>?   OtherGradeIds);
