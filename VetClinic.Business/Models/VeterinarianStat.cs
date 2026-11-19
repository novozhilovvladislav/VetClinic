namespace VetClinic.Business.Models;

/// <summary>Статистика лікаря за період: кількість завершених візитів, виручка, середній час прийому.</summary>
public sealed record VeterinarianStat(
    Guid VeterinarianId,
    string Name,
    string Specialization,
    int VisitsCount,
    decimal Revenue,
    TimeSpan AverageDuration);
