namespace VetClinic.Business.Models;

/// <summary> Статистика кабінету за період </summary>
public sealed record CabinetStat(
    int Room,
    int VisitsCount,
    TimeSpan AverageDuration,
    TimeSpan BusyTime,
    double LoadPercent);
