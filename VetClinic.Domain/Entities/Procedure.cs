using VetClinic.Domain.Exceptions;
using VetClinic.Domain.Validation;

namespace VetClinic.Domain.Entities;

/// <summary>Процедура </summary>
public class Procedure : IEntity
{
    public const int MaxNameLength = 100;
    public const decimal MaxPrice = 1_000_000m;

    private string _name = string.Empty;
    private decimal _price;

    public Procedure()
    {
    }

    public Procedure(string name, decimal price)
    {
        Name = name;
        Price = price;
    }

    public Guid Id { get; init; } = Guid.NewGuid();

    /// <summary>Назва процедури.</summary>
    public string Name
    {
        get => _name;
        set => _name = NormalizeName(value);
    }

    /// <summary>Вартість у гривнях, від 0, два знаки після коми)</summary>
    public decimal Price
    {
        get => _price;
        set => _price = NormalizePrice(value);
    }

    public static string NormalizeName(string? value) =>
        Guard.RequiredText(value, "Назва процедури", MaxNameLength);

    public static decimal NormalizePrice(decimal value) =>
        Guard.Money(value, "Вартість", MaxPrice);

    /// <summary>Повна перевірка цілісності, викликається перед збереженням </summary>
    public void Validate()
    {
        if (Id == Guid.Empty)
        {
            throw new DomainException("Процедура повинна мати ідентифікатор.");
        }

        NormalizeName(_name);
        NormalizePrice(_price);
    }

    public override string ToString() => $"{Name} — {Price:0.00}";
}
