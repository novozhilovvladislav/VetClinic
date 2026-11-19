namespace VetClinic.Domain.Enums;


public enum VisitStatus
{
    /// <summary>Зареєстрований </summary>
    Registered = 0,

    /// <summary>В процесі </summary>
    InProgress = 1,

    /// <summary>Завершено </summary>
    Completed = 2,

    /// <summary>Скасовано </summary>
    Cancelled = 3
}
