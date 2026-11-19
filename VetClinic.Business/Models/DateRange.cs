namespace VetClinic.Business.Models;

/// <summary>Діапазон дат</summary>
public sealed record DateRange(DateOnly From, DateOnly To)
{
    /// <summary>Кількість календарних днів у діапазоні.</summary>
    public int Days => To.DayNumber - From.DayNumber + 1;

    public bool Contains(DateOnly date) => date >= From && date <= To;


    public static DateRange For(PeriodKind kind, DateOnly anchor)
    {
        switch (kind)
        {
            case PeriodKind.Day:
                return new DateRange(anchor, anchor);

            case PeriodKind.Week:
                var offsetFromMonday = ((int)anchor.DayOfWeek + 6) % 7;
                var monday = anchor.AddDays(-offsetFromMonday);
                return new DateRange(monday, monday.AddDays(6));

            case PeriodKind.Month:
                var firstDay = new DateOnly(anchor.Year, anchor.Month, 1);
                return new DateRange(firstDay, firstDay.AddMonths(1).AddDays(-1));

            case PeriodKind.Year:
                return new DateRange(new DateOnly(anchor.Year, 1, 1), new DateOnly(anchor.Year, 12, 31));

            default:
                throw new ArgumentOutOfRangeException(nameof(kind), kind, "Невідомий тип періоду.");
        }
    }
}
