namespace VetClinic.Domain.Exceptions;

/// <summary>
/// Порушення правил предметної області.
/// Невалідні значення полів сутностей, недопустимі переходи між статусами візиту і т.д.
/// </summary>
public class DomainException : VetClinicException
{
    public DomainException(string message) : base(message)
    {
    }
}
