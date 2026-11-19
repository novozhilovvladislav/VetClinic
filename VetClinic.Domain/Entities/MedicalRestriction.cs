using VetClinic.Domain.Enums;
using VetClinic.Domain.Validation;

namespace VetClinic.Domain.Entities;

/// <summary>
/// Те, що може обмежувати майбутні процедури (хронічна хвороба, алергія, операція). Теги вказують, на які процедури
/// поширюється обмеження.
/// </summary>
public class MedicalRestriction : PassportRecord
{
    public const int MaxTitleLength = 150;

    private const string TagsField = "Теги обмеження";

    private string _title = string.Empty;
    private List<string> _restrictedTags = new List<string>();

    public MedicalRestriction()
    {
    }

    /// <summary>Обмеження встановлено, дата може бути в майбутньому </summary>
    public MedicalRestriction(
        RestrictionType type,
        string title,
        DateOnly? date,
        string? note,
        IEnumerable<string>? restrictedTags,
        DateOnly today)
    {
        Type = type;
        Title = title;
        Date = Guard.OptionalPastOrToday(date, today, "Дата");
        Note = note;
        _restrictedTags = new List<string>(NormalizeTags(restrictedTags));
    }

    public RestrictionType Type { get; set; }

    /// <summary>Назва хвороби, алергену чи операції </summary>
    public string Title
    {
        get => _title;
        set => _title = NormalizeTitle(value);
    }

    public DateOnly? Date { get; set; }

    public IReadOnlyList<string> RestrictedTags
    {
        get => _restrictedTags;
        set => _restrictedTags = new List<string>(NormalizeTags(value));
    }

    public static string NormalizeTitle(string? value) =>
        Guard.RequiredText(value, "Назва", MaxTitleLength);

    public static IReadOnlyList<string> NormalizeTags(IEnumerable<string>? tags) =>
        TagNormalizer.Normalize(tags, TagsField);

    /// <summary>Розбирає рядок із тегами через кому або крапку з комою </summary>
    public static IReadOnlyList<string> ParseTags(string? text) =>
        TagNormalizer.Parse(text, TagsField);

    public override void Validate()
    {
        ValidateId();
        NormalizeTitle(_title);
    }
}
