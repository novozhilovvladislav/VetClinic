using VetClinic.Domain.Exceptions;

namespace VetClinic.Persistence;

/// <summary>Помилка читання або запису файлів даних.</summary>
public class DataStorageException : VetClinicException
{
    public DataStorageException(string message) : base(message)
    {
    }

    public DataStorageException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
