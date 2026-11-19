using VetClinic.Domain.Exceptions;
using VetClinic.Domain.Validation;

namespace VetClinic.Domain.Entities;

/// <summary>Лікар клініки: ім’я та спеціалізація </summary>
public class Veterinarian : IEntity
{
    public const int MaxNameLength = 100;
    public const int MaxSpecializationLength = 100;

    private string _name = string.Empty;
    private string _specialization = string.Empty;

    public Veterinarian()
    {
    }

    public Veterinarian(string name, string specialization)
    {
        Name = name;
        Specialization = specialization;
    }

    public Guid Id { get; init; } = Guid.NewGuid();

    /// <summary>Прізвище та ім’я лікаря </summary>
    public string Name
    {
        get => _name;
        set => _name = NormalizeName(value);
    }

    /// <summary>Спеціалізація (терапевт, хірург, офтальмолог тощо) </summary>
    public string Specialization
    {
        get => _specialization;
        set => _specialization = NormalizeSpecialization(value);
    }

    public static string NormalizeName(string? value) =>
        Guard.RequiredText(value, "Ім’я лікаря", MaxNameLength);

    public static string NormalizeSpecialization(string? value) =>
        Guard.RequiredText(value, "Спеціалізація", MaxSpecializationLength);

    public void Validate()
    {
        if (Id == Guid.Empty)
        {
            throw new DomainException("Лікар повинен мати ідентифікатор.");
        }

        NormalizeName(_name);
        NormalizeSpecialization(_specialization);
    }

    public override string ToString() => $"{Name} ({Specialization})";
}
