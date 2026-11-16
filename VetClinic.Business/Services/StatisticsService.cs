using VetClinic.Business.Models;
using VetClinic.Domain.Entities;
using VetClinic.Domain.Enums;
using VetClinic.Persistence;

namespace VetClinic.Business.Services;

/// <summary>Статистика клініки.</summary>
public class StatisticsService
{
    private readonly IRepository<Visit> _visits;

    public StatisticsService(IRepository<Visit> visits)
    {
        _visits = visits;
    }

    /// <summary>
    /// Виручка за день — сумарна вартість візитів, завершених у цей день
    /// (день визначається за часом завершення візиту, а не за плановою датою).
    /// </summary>
    public DailyReport GetDailyReport(DateOnly day)
    {
        var completed = _visits.GetAll()
            .Where(v => v.Status == VisitStatus.Completed
                        && v.CompletedAt.HasValue
                        && DateOnly.FromDateTime(v.CompletedAt.Value) == day)
            .ToList();

        return new DailyReport(day, completed.Count, completed.Sum(v => v.TotalCost));
    }
}
