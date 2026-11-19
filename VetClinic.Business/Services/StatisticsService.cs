using VetClinic.Business.Models;
using VetClinic.Domain.Entities;
using VetClinic.Domain.Enums;
using VetClinic.Persistence;

namespace VetClinic.Business.Services;

/// <summary>
/// Статистика клініки. Усі показники рахуються за <b>завершеними</b> візитами, а день, до якого
/// належить візит, визначається за часом його завершення (<see cref="Visit.CompletedAt"/>),
/// а не за плановою датою — так само, як і виручка за день.
/// </summary>
public class StatisticsService
{
    /// <summary>
    /// Робочий день клініки — 09:00–18:00, тобто 9 годин. Використовується як «доступний час»
    /// кабінету під час розрахунку його завантаженості.
    /// </summary>
    public const double WorkDayHours = 9;

    private readonly IRepository<Visit> _visits;
    private readonly IRepository<Veterinarian> _veterinarians;
    private readonly TimeProvider _timeProvider;

    public StatisticsService(
        IRepository<Visit> visits,
        IRepository<Veterinarian> veterinarians,
        TimeProvider? timeProvider = null)
    {
        _visits = visits;
        _veterinarians = veterinarians;
        _timeProvider = timeProvider ?? TimeProvider.System;
    }

    // ---------- Виручка ----------

    /// <summary>
    /// Виручка за день — сумарна вартість візитів, завершених у цей день
    /// (день визначається за часом завершення візиту, а не за плановою датою).
    /// </summary>
    public DailyReport GetDailyReport(DateOnly day)
    {
        var completed = GetCompleted(new DateRange(day, day));

        return new DailyReport(day, completed.Count, completed.Sum(v => v.TotalCost));
    }

    /// <summary>
    /// Виручка та кількість прийомів за день, тиждень (пн–нд), календарний місяць або рік,
    /// у які потрапляє дата <paramref name="anchor"/>.
    /// </summary>
    public PeriodReport GetPeriodReport(PeriodKind kind, DateOnly anchor)
    {
        var range = DateRange.For(kind, anchor);
        var completed = GetCompleted(range);

        return new PeriodReport(kind, range, completed.Count, completed.Sum(v => v.TotalCost));
    }

    // ---------- Кабінети ----------

    /// <summary>
    /// Статистика по кабінетах за період (<c>null</c> — за весь час): кількість завершених візитів,
    /// середній час одного прийому та завантаженість.
    /// Завантаженість = сумарний час прийомів кабінету / (кількість днів періоду × 9 год робочого дня).
    /// Дні, що ще не настали, у розрахунок не беруться. Візити без кабінету (зі старих версій) пропускаються.
    /// </summary>
    public IReadOnlyList<CabinetStat> GetCabinetStatistics(DateRange? range)
    {
        var completed = GetCompleted(range).Where(v => v.Room.HasValue).ToList();
        if (completed.Count == 0)
        {
            return new List<CabinetStat>();
        }

        var availableHours = GetAvailableHours(range, completed);
        var result = new List<CabinetStat>();

        foreach (var roomVisits in completed.GroupBy(v => v.Room.GetValueOrDefault()))
        {
            var durations = GetDurations(roomVisits);
            var busy = TotalTime(durations);
            var load = availableHours > 0 ? busy.TotalHours / availableHours * 100.0 : 0.0;

            result.Add(new CabinetStat(roomVisits.Key, roomVisits.Count(), AverageTime(durations), busy, load));
        }

        return result.OrderBy(s => s.Room).ToList();
    }

    // ---------- Лікарі ----------

    /// <summary>
    /// Статистика по лікарях за період (<c>null</c> — за весь час): кількість завершених візитів,
    /// виручка та середній час прийому. Лікарі без візитів теж включаються (з нулями).
    /// Візити без лікаря (зі старих версій) пропускаються.
    /// </summary>
    public IReadOnlyList<VeterinarianStat> GetVeterinarianStatistics(DateRange? range)
    {
        var completed = GetCompleted(range).Where(v => v.VeterinarianId.HasValue).ToList();
        var result = new List<VeterinarianStat>();

        foreach (var veterinarian in _veterinarians.GetAll())
        {
            var visits = completed.Where(v => v.VeterinarianId == veterinarian.Id).ToList();
            var durations = GetDurations(visits);

            result.Add(new VeterinarianStat(
                veterinarian.Id,
                veterinarian.Name,
                veterinarian.Specialization,
                visits.Count,
                visits.Sum(v => v.TotalCost),
                AverageTime(durations)));
        }

        return result
            .OrderByDescending(s => s.Revenue)
            .ThenByDescending(s => s.VisitsCount)
            .ThenBy(s => s.Name, StringComparer.CurrentCultureIgnoreCase)
            .ToList();
    }

    // ---------- Процедури ----------

    /// <summary>Найчастіше виконувані процедури (за кількістю виконань), перші <paramref name="top"/>.</summary>
    public IReadOnlyList<ProcedureStat> GetMostFrequentProcedures(DateRange? range, int top)
    {
        return AggregateProcedures(range)
            .OrderByDescending(s => s.TimesPerformed)
            .ThenByDescending(s => s.Revenue)
            .ThenBy(s => s.Name, StringComparer.CurrentCultureIgnoreCase)
            .Take(Math.Max(top, 1))
            .ToList();
    }

    /// <summary>
    /// Найбільш прибуткові процедури (виручка мінус собівартість), перші <paramref name="top"/>.
    /// Собівартість береться з копії у візиті, тож пізніша зміна собівартості не спотворює минулі періоди;
    /// для візитів зі старих версій системи (без собівартості) вона вважається нульовою.
    /// </summary>
    public IReadOnlyList<ProcedureStat> GetMostProfitableProcedures(DateRange? range, int top)
    {
        return AggregateProcedures(range)
            .OrderByDescending(s => s.Profit)
            .ThenByDescending(s => s.TimesPerformed)
            .ThenBy(s => s.Name, StringComparer.CurrentCultureIgnoreCase)
            .Take(Math.Max(top, 1))
            .ToList();
    }

    // ---------- Внутрішні методи ----------

    /// <summary>Завершені візити, час завершення яких потрапляє в діапазон (null — без обмежень).</summary>
    private List<Visit> GetCompleted(DateRange? range)
    {
        return _visits.GetAll()
            .Where(v => v.Status == VisitStatus.Completed
                        && v.CompletedAt.HasValue
                        && (range is null || range.Contains(DateOnly.FromDateTime(v.CompletedAt.Value))))
            .ToList();
    }

    private List<ProcedureStat> AggregateProcedures(DateRange? range)
    {
        // Візити впорядковано від найновіших, тому g.First() — назва з найсвіжішого візиту
        // (процедуру могли перейменувати).
        return GetCompleted(range)
            .OrderByDescending(v => v.CompletedAt)
            .SelectMany(v => v.Procedures.Where(p => p.IsPerformed))
            .GroupBy(p => p.ProcedureId)
            .Select(g => new ProcedureStat(
                g.Key,
                g.First().Name,
                g.Count(),
                g.Sum(p => p.Price),
                g.Sum(p => p.CostPrice)))
            .ToList();
    }

    /// <summary>Скільки годин кабінети були доступні за період (див. <see cref="GetCabinetStatistics"/>).</summary>
    private double GetAvailableHours(DateRange? range, List<Visit> completed)
    {
        var today = DateOnly.FromDateTime(_timeProvider.GetLocalNow().DateTime);

        DateOnly from;
        DateOnly to;

        if (range is null)
        {
            from = completed.Min(v => DateOnly.FromDateTime(v.CompletedAt.GetValueOrDefault()));
            to = today;
        }
        else
        {
            from = range.From;
            to = range.To < today ? range.To : today;
        }

        var days = to.DayNumber - from.DayNumber + 1;
        return days > 0 ? days * WorkDayHours : 0;
    }

    private static List<TimeSpan> GetDurations(IEnumerable<Visit> visits) =>
        visits
            .Select(v => v.ServiceDuration)
            .Where(d => d.HasValue)
            .Select(d => d.GetValueOrDefault())
            .ToList();

    private static TimeSpan TotalTime(List<TimeSpan> durations) =>
        TimeSpan.FromTicks(durations.Sum(d => d.Ticks));

    private static TimeSpan AverageTime(List<TimeSpan> durations) =>
        durations.Count == 0 ? TimeSpan.Zero : TimeSpan.FromTicks(durations.Sum(d => d.Ticks) / durations.Count);
}
