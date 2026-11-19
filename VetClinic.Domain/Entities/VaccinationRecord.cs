using VetClinic.Domain.Exceptions;
using VetClinic.Domain.Validation;

namespace VetClinic.Domain.Entities;

/// <summary>Проведена вакцинація </summary>
public class VaccinationRecord : PassportRecord
{
    public const int MaxVaccineLength = 100;

    private string _vaccine = string.Empty;

    public VaccinationRecord()
    {
    }

    /// <param name="today">Поточна дата: вакцинація вже проведена, тож дата не може бути в майбутньому </param>
    public VaccinationRecord(string vaccine, DateOnly date, DateOnly? nextDueDate, string? note, DateOnly today)
    {
        Vaccine = vaccine;
        Date = Guard.PastOrToday(date, today, "Дата вакцинації");
        NextDueDate = Guard.NextDueDate(nextDueDate, Date, "Дата наступної вакцинації");
        Note = note;
    }

    /// <summary>Назва вакцини </summary>
    public string Vaccine
    {
        get => _vaccine;
        set => _vaccine = NormalizeVaccine(value);
    }

    /// <summary>Дата проведення </summary>
    public DateOnly Date { get; set; }

    /// <summary>Коли потрібно повторити вакцинацію (необов’язково) </summary>
    public DateOnly? NextDueDate { get; set; }

    public static string NormalizeVaccine(string? value) =>
        Guard.RequiredText(value, "Вакцина", MaxVaccineLength);

    public override void Validate()
    {
        ValidateId();
        NormalizeVaccine(_vaccine);

        if (Date == default)
        {
            throw new DomainException("Дата вакцинації: значення обов’язкове.");
        }
    }
}
