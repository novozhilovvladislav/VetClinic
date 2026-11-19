namespace VetClinic.Business.Models;

/// <summary>Підсумок за період: кількість завершених прийомів і виручка.</summary>
public sealed record PeriodReport(PeriodKind Kind, DateRange Range, int CompletedVisitsCount, decimal Revenue);
