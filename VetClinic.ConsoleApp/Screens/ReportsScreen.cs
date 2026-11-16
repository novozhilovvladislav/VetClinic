using VetClinic.Business.Services;
using VetClinic.ConsoleApp.Infrastructure;

namespace VetClinic.ConsoleApp.Screens;

/// <summary>Статистика </summary>
public sealed class ReportsScreen
{
    private readonly StatisticsService _statistics;

    public ReportsScreen(StatisticsService statistics)
    {
        _statistics = statistics;
    }

    public void Show() => Ui.Menu("Статистика", new[]
    {
        new MenuItem("Виручка за день", () => Ui.Run(DailyRevenue))
    });

    private void DailyRevenue()
    {
        Ui.FormHeader("Виручка за день");

        var today = DateOnly.FromDateTime(DateTime.Today);
        var day = Ui.ReadDate("Дата (дд.мм.рррр, Enter — сьогодні)", today);

        var report = _statistics.GetDailyReport(day);

        Console.WriteLine();
        Console.WriteLine($"Дата:                  {Format.DateText(report.Date)}");
        Console.WriteLine($"Завершених візитів:    {report.CompletedVisitsCount}");
        Console.WriteLine($"Виручка за день:       {Format.Money(report.Revenue)}");

        if (report.CompletedVisitsCount == 0)
        {
            Ui.Info("У цей день не було завершених візитів.");
        }
    }
}
