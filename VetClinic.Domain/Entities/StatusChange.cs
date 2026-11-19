using VetClinic.Domain.Enums;

namespace VetClinic.Domain.Entities;

/// <summary>Запис в історії змін статусу візиту </summary>
public class StatusChange
{
    public StatusChange()
    {
    }

    public StatusChange(VisitStatus? from, VisitStatus to, DateTime changedAt, string? comment = null)
    {
        From = from;
        To = to;
        ChangedAt = changedAt;
        Comment = comment;
    }

    /// <summary>Попередній статус, null для першого запису, коли візит щойно створено </summary>
    public VisitStatus? From { get; set; }

    public VisitStatus To { get; set; }

    public DateTime ChangedAt { get; set; }

    public string? Comment { get; set; }
}
