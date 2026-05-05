namespace OftalmoLibre.Helpers;

public static class DocumentNumberHelper
{
    public static string? Normalize(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var compact = value.Trim()
            .Replace(" ", string.Empty)
            .Replace(".", string.Empty)
            .ToUpperInvariant();

        if (compact.Length <= 1)
        {
            return compact;
        }

        var verifier = compact[^1].ToString();
        var body = compact[..^1].Replace("-", string.Empty);
        return string.IsNullOrWhiteSpace(body) ? compact : $"{body}-{verifier}";
    }

    public static string NormalizeForComparison(string? value)
    {
        return (Normalize(value) ?? string.Empty).Replace("-", string.Empty, StringComparison.Ordinal);
    }
}
