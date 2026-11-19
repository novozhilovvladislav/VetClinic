using VetClinic.Business.Models;
using VetClinic.Business.Services;
using VetClinic.ConsoleApp.Infrastructure;

namespace VetClinic.ConsoleApp.Screens;

/// <summary>Статистика: виручка, періоди, кабінети, лікарі, процедури </summary>
public sealed class ReportsScreen
{
    private const int TopCount = 10;

    private static readonly PeriodKind[] PeriodKinds =
    {
        PeriodKind.Day,
        PeriodKind.Week,
        PeriodKind.Month,
        PeriodKind.Year
    };

    private readonly StatisticsService _statistics;

    public ReportsScreen(StatisticsService statistics)
    {
        _statistics = statistics;
    }

    public void Show() => Ui.Menu("Статистика", new[]
    {
        new MenuItem("Виручка за день", () => Ui.Run(DailyRevenue)),
        new MenuItem("Виручка та кількість прийомів за період (день / тиждень / місяць / рік)",
            () => Ui.Run(PeriodRevenue)),
        new MenuItem("Статистика по кабінетах", () => Ui.Run(Cabinets)),
        new MenuItem("Статистика по лікарях", () => Ui.Run(Veterinarians)),
        new MenuItem("Найчастіше виконувані процедури", () => Ui.Run(MostFrequentProcedures)),
        new MenuItem("Найбільш прибуткові процедури", () => Ui.Run(MostProfitableProcedures))
    });

    private void DailyRevenue()
    {
        Ui.FormHeader("Виручка за день");

        var day = Ui.ReadDate("Дата (дд.мм.рррр, Enter — сьогодні)", Today());

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

    private void PeriodRevenue()
    {
        Ui.FormHeader("Виручка та прийоми за період");

        var kind = ReadPeriodKind();
        var anchor = Ui.ReadDate("Будь-яка дата з потрібного періоду (дд.мм.рррр, Enter — сьогодні)", Today());

        var report = _statistics.GetPeriodReport(kind, anchor);

        Console.WriteLine();
        Console.WriteLine($"Період:                {Format.PeriodKindText(report.Kind)}");
        Console.WriteLine($"Дати:                  {Format.RangeText(report.Range)}");
        Console.WriteLine($"Завершених прийомів:   {report.CompletedVisitsCount}");
        Console.WriteLine($"Виручка:               {Format.Money(report.Revenue)}");

        if (report.CompletedVisitsCount == 0)
        {
            Ui.Info("За цей період не було завершених візитів.");
        }
    }

    private void Cabinets()
    {
        Ui.FormHeader("Статистика по кабінетах");

        var range = ReadRange(allowAllTime: true);
        var stats = _statistics.GetCabinetStatistics(range);

        Console.WriteLine();
        Console.WriteLine($"Період: {Format.RangeText(range)}");

        if (stats.Count == 0)
        {
            Ui.Info("За цей період немає завершених візитів із вказаним кабінетом.");
            return;
        }

        Ui.Info($"Завантаженість = час прийомів / (дні періоду × {StatisticsService.WorkDayHours:0} год робочого дня, 09:00–18:00).");
        Console.WriteLine();
        Console.WriteLine($"{"Кабінет",-9}{"Візитів",9}{"Середній час",16}{"Час прийомів",16}{"Завантаженість",17}");

        foreach (var stat in stats)
        {
            Console.WriteLine(
                $"{stat.Room,-9}{stat.VisitsCount,9}{Format.DurationText(stat.AverageDuration),16}" +
                $"{Format.DurationText(stat.BusyTime),16}{Format.Percent(stat.LoadPercent),17}");
        }
    }

    private void Veterinarians()
    {
        Ui.FormHeader("Статистика по лікарях");

        var range = ReadRange(allowAllTime: true);
        var stats = _statistics.GetVeterinarianStatistics(range);

        Console.WriteLine();
        Console.WriteLine($"Період: {Format.RangeText(range)}");

        if (stats.Count == 0)
        {
            Ui.Info("Лікарів ще немає.");
            return;
        }

        Console.WriteLine();
        Console.WriteLine($"{"Лікар",-28}{"Спеціалізація",-20}{"Візитів",9}{"Виручка",18}{"Середній час",16}");

        foreach (var stat in stats)
        {
            Console.WriteLine(
                $"{Format.Fit(stat.Name, 27),-28}{Format.Fit(stat.Specialization, 19),-20}{stat.VisitsCount,9}" +
                $"{Format.Money(stat.Revenue),18}{Format.DurationText(stat.AverageDuration),16}");
        }
    }

    private void MostFrequentProcedures()
    {
        Ui.FormHeader("Найчастіше виконувані процедури");

        var range = ReadRange(allowAllTime: true);
        PrintProcedures(range, _statistics.GetMostFrequentProcedures(range, TopCount));
    }

    private void MostProfitableProcedures()
    {
        Ui.FormHeader("Найбільш прибуткові процедури");

        var range = ReadRange(allowAllTime: true);
        PrintProcedures(range, _statistics.GetMostProfitableProcedures(range, TopCount));
    }

    private static void PrintProcedures(DateRange? range, IReadOnlyList<ProcedureStat> stats)
    {
        Console.WriteLine();
        Console.WriteLine($"Період: {Format.RangeText(range)}");

        if (stats.Count == 0)
        {
            Ui.Info("За цей період немає завершених візитів.");
            return;
        }

        Ui.Info($"Прибуток = виручка мінус собівартість (топ-{TopCount}).");
        Console.WriteLine();
        Console.WriteLine($"{"№",-4}{"Процедура",-32}{"Разів",7}{"Виручка",18}{"Собівартість",18}{"Прибуток",18}");

        for (var i = 0; i < stats.Count; i++)
        {
            var stat = stats[i];
            Console.WriteLine(
                $"{i + 1,-4}{Format.Fit(stat.Name, 31),-32}{stat.TimesPerformed,7}{Format.Money(stat.Revenue),18}" +
                $"{Format.Money(stat.Cost),18}{Format.Money(stat.Profit),18}");
        }
    }

    // ---------- Вибір періоду ----------

    private static PeriodKind ReadPeriodKind()
    {
        var index = Ui.ChooseIndex("Оберіть період:", PeriodKinds.Select(kind => Format.PeriodKindText(kind)).ToList());
        if (index is null)
        {
            throw new UserCancelledException();
        }

        return PeriodKinds[index.Value];
    }

    /// <summary>Запитує період; повертає null, якщо обрано «За весь час».</summary>
    private static DateRange? ReadRange(bool allowAllTime)
    {
        var options = PeriodKinds.Select(kind => Format.PeriodKindText(kind)).ToList();
        if (allowAllTime)
        {
            options.Add("За весь час");
        }

        var index = Ui.ChooseIndex("Оберіть період:", options);
        if (index is null)
        {
            throw new UserCancelledException();
        }

        if (index.Value >= PeriodKinds.Length)
        {
            return null;
        }

        var anchor = Ui.ReadDate("Будь-яка дата з потрібного періоду (дд.мм.рррр, Enter — сьогодні)", Today());
        return DateRange.For(PeriodKinds[index.Value], anchor);
    }

    private static DateOnly Today() => DateOnly.FromDateTime(DateTime.Today);
}
