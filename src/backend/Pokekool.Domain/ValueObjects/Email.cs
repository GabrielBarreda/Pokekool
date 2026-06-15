using System.Text.RegularExpressions;
using Pokekool.Domain.Common;

namespace Pokekool.Domain.ValueObjects;

/// <summary>
/// Email validé et normalisé (lowercase + trim).
/// </summary>
public sealed partial class Email : ValueObject
{
    public string Value { get; }

    private Email(string value) => Value = value;

    public static Email Create(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            throw new DomainException("email.empty", "Email is required");

        var normalized = raw.Trim().ToLowerInvariant();

        if (!EmailRegex().IsMatch(normalized))
            throw new DomainException("email.invalid", $"Invalid email format: {raw}");

        return new Email(normalized);
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }

    public override string ToString() => Value;

    [GeneratedRegex(@"^[^\s@]+@[^\s@]+\.[^\s@]+$")]
    private static partial Regex EmailRegex();
}
