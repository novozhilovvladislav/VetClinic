using VetClinic.Domain.Enums;

namespace VetClinic.Domain.Extensions;

/// <summary>Назви для відображення перелічень, що використовуються в паспорті тварини </summary>
public static class PassportEnumExtensions
{
    public static string ToDisplayName(this RestrictionType type) => type switch
    {
        RestrictionType.ChronicDisease => "Хронічна хвороба",
        RestrictionType.Allergy => "Алергія",
        RestrictionType.Surgery => "Операція",
        _ => type.ToString()
    };

    public static string ToDisplayName(this AntiparasiticKind kind) => kind switch
    {
        AntiparasiticKind.External => "Зовнішні паразити (блохи, кліщі)",
        AntiparasiticKind.Internal => "Внутрішні паразити (глисти)",
        AntiparasiticKind.Complex => "Комплексна обробка",
        _ => kind.ToString()
    };
}
