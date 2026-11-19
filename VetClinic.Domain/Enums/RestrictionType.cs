namespace VetClinic.Domain.Enums;

/// <summary>
/// Тип запису в історії хвороб паспорта
/// лише те, що може обмежувати майбутні процедури
/// </summary>
public enum RestrictionType
{
    /// <summary>Хронічна хвороба </summary>
    ChronicDisease = 0,

    /// <summary>Алергія </summary>
    Allergy = 1,

    /// <summary>Перенесена операція </summary>
    Surgery = 2
}
