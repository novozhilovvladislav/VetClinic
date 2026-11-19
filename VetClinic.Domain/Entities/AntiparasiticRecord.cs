using VetClinic.Domain.Enums;
using VetClinic.Domain.Exceptions;
using VetClinic.Domain.Validation;

namespace VetClinic.Domain.Entities;

/// <summary>Проведена протипаразитна обробка </summary>
public class AntiparasiticRecord : PassportRecord
{
    public const int MaxPreparationLength = 100;

    private string _preparation = string.Empty;

    public AntiparasiticRecord()
    {
    }

    /// <summary>Обробка проведена, дата може бути в майбутньому </summary>
    public AntiparasiticRecord(
        AntiparasiticKind kind,
        string preparation,
        DateOnly date,
        DateOnly? nextDueDate,
        string? note,
        DateOnly today)
    {
        Kind = kind;
        Preparation = preparation;
        Date = Guard.PastOrToday(date, today, "Дата обробки");
        NextDueDate = Guard.NextDueDate(nextDueDate, Date, "Дата наступної обробки");
        Note = note;
    }

    public AntiparasiticKind Kind { get; set; }

    /// <summary>Назва препарату </summary>
    public string Preparation
    {
        get => _preparation;
        set => _preparation = NormalizePreparation(value);
    }

    /// <summary>Дата проведення </summary>
    public DateOnly Date { get; set; }

    public DateOnly? NextDueDate { get; set; }

    public static string NormalizePreparation(string? value) =>
        Guard.RequiredText(value, "Препарат", MaxPreparationLength);

    public override void Validate()
    {
        ValidateId();
        NormalizePreparation(_preparation);

        if (Date == default)
        {
            throw new DomainException("Дата обробки: значення обов’язкове.");
        }
    }
}
