using VetClinic.Domain.Exceptions;
using VetClinic.Domain.Validation;

namespace VetClinic.Domain.Entities;

/// <summary>Домашня тварина, завжди належить певному власнику </summary>
public class Pet : IEntity
{
    public const int MaxNameLength = 50;
    public const int MaxSpeciesLength = 50;
    public const int MaxBreedLength = 80;
    public const int MinAge = 0;
    public const int MaxAge = 60;

    private Guid _ownerId;
    private string _name = string.Empty;
    private string _species = string.Empty;
    private string? _breed;
    private int _age;

    public Pet()
    {
    }

    public Pet(Guid ownerId, string name, string species, string? breed, int age)
    {
        OwnerId = ownerId;
        Name = name;
        Species = species;
        Breed = breed;
        Age = age;
    }

    public Guid Id { get; init; } = Guid.NewGuid();

    public Guid OwnerId
    {
        get => _ownerId;
        set => _ownerId = Guard.NotEmpty(value, "Власник");
    }

    /// <summary>Кличка </summary>
    public string Name
    {
        get => _name;
        set => _name = NormalizeName(value);
    }

    /// <summary>Вид </summary>
    public string Species
    {
        get => _species;
        set => _species = NormalizeSpecies(value);
    }

    /// <summary>Порода </summary>
    public string? Breed
    {
        get => _breed;
        set => _breed = NormalizeBreed(value);
    }

    /// <summary>Вік у повних роках </summary>
    public int Age
    {
        get => _age;
        set => _age = NormalizeAge(value);
    }

    public static string NormalizeName(string? value) =>
        Guard.RequiredText(value, "Кличка", MaxNameLength);

    public static string NormalizeSpecies(string? value) =>
        Guard.RequiredText(value, "Вид", MaxSpeciesLength);

    public static string? NormalizeBreed(string? value) =>
        Guard.OptionalText(value, "Порода", MaxBreedLength);

    public static int NormalizeAge(int value) =>
        Guard.InRange(value, MinAge, MaxAge, "Вік");

    public void Validate()
    {
        if (Id == Guid.Empty)
        {
            throw new DomainException("Тварина повинна мати ідентифікатор.");
        }

        Guard.NotEmpty(_ownerId, "Власник");
        NormalizeName(_name);
        NormalizeSpecies(_species);
    }

    public override string ToString() => $"{Name} ({Species})";
}
