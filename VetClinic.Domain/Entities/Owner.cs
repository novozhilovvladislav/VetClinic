using System.Text.Json.Serialization;
using VetClinic.Domain.Exceptions;
using VetClinic.Domain.Validation;

namespace VetClinic.Domain.Entities;

/// <summary>Власник тварини </summary>
public class Owner : IEntity
{
    public const int MaxNameLength = 100;

    private string _name = string.Empty;
    private string _phone = string.Empty;
    private string? _email;

    public Owner()
    {
    }

    public Owner(string name, string phone, string? email = null)
    {
        Name = name;
        Phone = phone;
        Email = email;
    }

    public Guid Id { get; init; } = Guid.NewGuid();

    /// <summary>Прізвище та ім'я власника.</summary>
    public string Name
    {
        get => _name;
        set => _name = NormalizeName(value);
    }

    /// <summary>Номер телефону власника </summary>
    public string Phone
    {
        get => _phone;
        set => _phone = NormalizePhone(value);
    }

    /// <summary>Електронна пошта (опціонально) </summary>
    public string? Email
    {
        get => _email;
        set => _email = NormalizeEmail(value);
    }

    /// <summary>
    /// Ключ для порівняння телефонів - останні 10 цифр,
    /// щоб при записі 0981234567 і (+380) 98 123 45 67 сприймались однаково
    /// </summary>
    [JsonIgnore]
    public string PhoneKey
    {
        get
        {
            var digits = new string(_phone.Where(char.IsAsciiDigit).ToArray());
            return digits.Length > 10 ? digits.Substring(digits.Length - 10) : digits;
        }
    }

    public static string NormalizeName(string? value) =>
        Guard.RequiredText(value, "Ім’я власника", MaxNameLength);

    public static string NormalizePhone(string? value) =>
        Guard.Phone(value, "Телефон");

    public static string? NormalizeEmail(string? value) =>
        Guard.OptionalEmail(value, "Email");

    /// <summary>Повна перевірка цілісності </summary>
    public void Validate()
    {
        if (Id == Guid.Empty)
        {
            throw new DomainException("Власник повинен мати ідентифікатор.");
        }

        NormalizeName(_name);
        NormalizePhone(_phone);
    }

    public override string ToString() => $"{Name} ({Phone})";
}
