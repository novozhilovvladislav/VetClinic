namespace VetClinic.Business.Models;

/// <summary>
/// Статистика процедури за період: скільки разів виконано, виручка, собівартість і прибуток.
/// </summary>
public sealed record ProcedureStat(
    Guid ProcedureId,
    string Name,
    int TimesPerformed,
    decimal Revenue,
    decimal Cost)
{
    /// <summary>Прибуток = виручка мінус собівартість.</summary>
    public decimal Profit => Revenue - Cost;
}
