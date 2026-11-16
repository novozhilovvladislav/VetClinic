using VetClinic.Domain.Exceptions;

namespace VetClinic.Business.Exceptions;

/// <summary>
/// Порушення бізнес-правил на рівні системи: дублікати, видалення пов’язаних даних,
/// звернення до неіснуючих записів тощо.
/// </summary>
public class BusinessRuleException : VetClinicException
{
    public BusinessRuleException(string message) : base(message)
    {
    }
}
