using VetClinic.Domain.Exceptions;

namespace VetClinic.Domain.Validation;

/// <summary>
/// Теги зберігаються малими літерами, без повторів, не довші за 30 символів, не більше 10 штук
/// </summary>
public static class TagNormalizer
{
    public const int MaxTagLength = 30;
    public const int MaxTagsCount = 10;

    private static readonly char[] Separators = { ',', ';' };

    /// <summary>Розбирає рядок «тег1, тег2; тег3». Порожній рядок дає порожній список </summary>
    public static IReadOnlyList<string> Parse(string? text, string fieldName)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return new List<string>();
        }

        return Normalize(text.Split(Separators, StringSplitOptions.RemoveEmptyEntries), fieldName);
    }

    public static IReadOnlyList<string> Normalize(IEnumerable<string>? tags, string fieldName)
    {
        var result = new List<string>();
        if (tags is null)
        {
            return result;
        }

        foreach (var raw in tags)
        {
            if (string.IsNullOrWhiteSpace(raw))
            {
                continue;
            }

            var tag = Guard.RequiredText(raw, "Тег", MaxTagLength).ToLowerInvariant();
            if (!result.Contains(tag))
            {
                result.Add(tag);
            }
        }

        if (result.Count > MaxTagsCount)
        {
            throw new DomainException($"{fieldName}: не більше {MaxTagsCount} тегів.");
        }

        return result;
    }
}
