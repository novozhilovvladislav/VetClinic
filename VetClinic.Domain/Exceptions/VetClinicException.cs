namespace VetClinic.Domain.Exceptions;

/// <summary>
/// Базовий тип для всіх «очікуваних» помилок системи
/// </summary>
public abstract class VetClinicException : Exception
{
    protected VetClinicException(string message) : base(message)
    {
    }

    protected VetClinicException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
