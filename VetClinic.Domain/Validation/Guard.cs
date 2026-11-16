using System.Net.Mail;
using VetClinic.Domain.Exceptions;

namespace VetClinic.Domain.Validation;

internal static class Guard
{
    public static string RequiredText(string? value, string fieldName, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new DomainException($"{fieldName}: значення обов’язкове.");
        }

        var trimmed = value.Trim();
        if (trimmed.Any(char.IsControl))
        {
            throw new DomainException($"{fieldName}: містить недопустимі керуючі символи.");
        }

        if (trimmed.Length > maxLength)
        {
            throw new DomainException($"{fieldName}: максимальна довжина — {maxLength} символів.");
        }

        return trimmed;
    }

    public static string? OptionalText(string? value, string fieldName, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        return RequiredText(value, fieldName, maxLength);
    }

    public static Guid NotEmpty(Guid value, string fieldName)
    {
        if (value == Guid.Empty)
        {
            throw new DomainException($"{fieldName}: не вказано.");
        }

        return value;
    }

    public static int InRange(int value, int min, int max, string fieldName)
    {
        if (value < min || value > max)
        {
            throw new DomainException($"{fieldName}: допустимі значення від {min} до {max}.");
        }

        return value;
    }

    public static decimal Money(decimal value, string fieldName, decimal max)
    {
        if (value < 0)
        {
            throw new DomainException($"{fieldName}: значення не може бути від’ємним.");
        }

        if (value > max)
        {
            throw new DomainException($"{fieldName}: максимально допустиме значення — {max:0.##}.");
        }

        return Math.Round(value, 2, MidpointRounding.AwayFromZero);
    }

    public static string Phone(string? value, string fieldName)
    {
        var phone = RequiredText(value, fieldName, 25);

        var digits = 0;
        for (var i = 0; i < phone.Length; i++)
        {
            var ch = phone[i];
            var isDigit = char.IsAsciiDigit(ch);
            var isAllowedSymbol = ch == ' ' || ch == '-' || ch == '(' || ch == ')' || (ch == '+' && i == 0);

            if (!isDigit && !isAllowedSymbol)
            {
                throw new DomainException(
                    $"{fieldName}: допустимі лише цифри, пробіли, дефіси, дужки та «+» на початку.");
            }

            if (isDigit)
            {
                digits++;
            }
        }

        if (digits < 10 || digits > 15)
        {
            throw new DomainException($"{fieldName}: номер має містити від 10 до 15 цифр.");
        }

        return phone;
    }

    public static string? OptionalEmail(string? value, string fieldName)
    {
        var email = OptionalText(value, fieldName, 100);
        if (email is null)
        {
            return null;
        }

        var isValid = MailAddress.TryCreate(email, out var parsed)
                      && string.Equals(parsed.Address, email, StringComparison.OrdinalIgnoreCase)
                      && parsed.Host.Contains('.');

        if (!isValid)
        {
            throw new DomainException($"{fieldName}: некоректна адреса електронної пошти.");
        }

        return email;
    }
}
