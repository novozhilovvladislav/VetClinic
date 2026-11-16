namespace VetClinic.Business.Models;

/// <summary>Підсумок за день: кількість завершених візитів і виручка.</summary>
public sealed record DailyReport(DateOnly Date, int CompletedVisitsCount, decimal Revenue);
