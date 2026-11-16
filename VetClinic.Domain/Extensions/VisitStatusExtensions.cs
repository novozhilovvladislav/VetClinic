using VetClinic.Domain.Enums;

namespace VetClinic.Domain.Extensions;

public static class VisitStatusExtensions
{
    public static string ToDisplayName(this VisitStatus status) => status switch
    {
        VisitStatus.Registered => "Зареєстрований",
        VisitStatus.InProgress => "В процесі",
        VisitStatus.Completed => "Завершено",
        VisitStatus.Cancelled => "Скасовано",
        _ => status.ToString()
    };

    /// <summary>Фінальні статуси: після них візит змінювати не можна.</summary>
    public static bool IsFinal(this VisitStatus status) =>
        status == VisitStatus.Completed || status == VisitStatus.Cancelled;

    /// <summary>
    /// Дозволені статуси:
    /// Зареєстрований → В процесі / Скасовано; В процесі → Завершено / Скасовано.
    /// </summary>
    public static IReadOnlyList<VisitStatus> AllowedNext(this VisitStatus status)
    {
        switch (status)
        {
            case VisitStatus.Registered:
                return new[] { VisitStatus.InProgress, VisitStatus.Cancelled };
            case VisitStatus.InProgress:
                return new[] { VisitStatus.Completed, VisitStatus.Cancelled };
            default:
                return Array.Empty<VisitStatus>();
        }
    }

    public static bool CanTransitionTo(this VisitStatus from, VisitStatus to) =>
        from.AllowedNext().Contains(to);
}
