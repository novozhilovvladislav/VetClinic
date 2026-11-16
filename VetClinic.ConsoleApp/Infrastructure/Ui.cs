using System.Globalization;
using VetClinic.Domain.Exceptions;

namespace VetClinic.ConsoleApp.Infrastructure;

/// <summary>Допоміжні методи консольного введення/виведення, спільні для всіх екранів </summary>
public static class Ui
{
    private static readonly string[] DateTimeFormats = { "dd.MM.yyyy HH:mm", "d.M.yyyy H:mm" };
    private static readonly string[] DateFormats = { "dd.MM.yyyy", "d.M.yyyy" };

    // ---------- Вивід ----------

    public static void Header(string title)
    {
        TryClear();
        var line = new string('=', Math.Max(title.Length + 4, 44));
        Console.WriteLine(line);
        Console.WriteLine($"  {title}");
        Console.WriteLine(line);
    }

    /// <summary>Заголовок форми введення з підказкою про скасування </summary>
    public static void FormHeader(string title)
    {
        Header(title);
        Info("Щоб скасувати введення, натисніть «-» і Enter.");
        Console.WriteLine();
    }

    public static void Info(string message) => WriteColored(message, ConsoleColor.Gray);

    public static void Success(string message) => WriteColored(message, ConsoleColor.Green);

    public static void Warn(string message) => WriteColored(message, ConsoleColor.Yellow);

    public static void Error(string message) => WriteColored(message, ConsoleColor.Red);

    public static void Pause()
    {
        Console.WriteLine();
        Console.Write("Натисніть Enter, щоб продовжити...");
        ConsoleInput.ReadLine();
    }

    /// <summary>Виводить нумерований список </summary>
    public static void PrintList<T>(IReadOnlyList<T> items, Func<T, string> format, string emptyMessage)
    {
        if (items.Count == 0)
        {
            Warn(emptyMessage);
            return;
        }

        for (var i = 0; i < items.Count; i++)
        {
            Console.WriteLine($"  {i + 1}. {format(items[i])}");
        }
    }

    // ---------- Виконання операцій ----------

    /// <summary>
    /// По-факту меню: показує повідомлення про очікувані помилки (валідація, бізнес-правила,
    /// скасування користувачем, а після завершення чекає натискання Enter
    /// </summary>
    public static void Run(Action action)
    {
        try
        {
            action();
        }
        catch (UserCancelledException)
        {
            Warn("Операцію скасовано.");
        }
        catch (VetClinicException ex)
        {
            Error(ex.Message);
        }
        catch (Exception ex)
        {
            Error($"Неочікувана помилка: {ex.Message}");
        }

        Pause();
    }

    // ---------- Меню ----------

    public static void Menu(string title, IReadOnlyList<MenuItem> items, string exitTitle = "Назад")
    {
        while (true)
        {
            Header(title);
            for (var i = 0; i < items.Count; i++)
            {
                Console.WriteLine($"  {i + 1}. {items[i].Title}");
            }

            Console.WriteLine($"  0. {exitTitle}");
            Console.WriteLine();

            var choice = ReadMenuChoice(items.Count);
            if (choice == 0)
            {
                return;
            }

            items[choice - 1].Execute();
        }
    }

    private static int ReadMenuChoice(int max)
    {
        while (true)
        {
            Console.Write("Ваш вибір: ");
            var input = ConsoleInput.ReadLine();
            if (input is null)
            {
                return 0; // вхідний потік закрито — виходимо з меню
            }

            if (int.TryParse(input.Trim(), NumberStyles.None, CultureInfo.InvariantCulture, out var number)
                && number >= 0 && number <= max)
            {
                return number;
            }

            Warn("Невірний пункт меню. Спробуйте ще раз.");
        }
    }

    // ---------- Введення ----------

    /// <summary>Зчитує рядок. Порожній рядок повертається як є; «-» скасовує операцію </summary>
    public static string ReadLine(string prompt)
    {
        Console.Write($"{prompt}: ");
        var input = ConsoleInput.ReadLine();

        if (input is null || input.Trim() == "-")
        {
            throw new UserCancelledException();
        }

        return input.Trim();
    }


    public static T Read<T>(string prompt, Func<string, T> convert)
    {
        while (true)
        {
            var input = ReadLine(prompt);
            if (TryConvert(input, convert, out var value))
            {
                return value;
            }
        }
    }


    public static T ReadKeep<T>(string prompt, string currentDisplay, T currentValue, Func<string, T> convert)
    {
        while (true)
        {
            var input = ReadLine($"{prompt} [{currentDisplay}] (Enter — залишити, «.» — очистити)");
            if (input.Length == 0)
            {
                return currentValue;
            }

            if (TryConvert(input == "." ? string.Empty : input, convert, out var value))
            {
                return value;
            }
        }
    }

    public static int ParseInt(string input)
    {
        if (!int.TryParse(input.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var value))
        {
            throw new FormatException("Введіть ціле число.");
        }

        return value;
    }

    /// <summary>Приймає і крапку, і кому як десятковий роздільник </summary>
    public static decimal ParseDecimal(string input)
    {
        var normalized = input.Replace(" ", string.Empty).Replace(',', '.');
        if (!decimal.TryParse(normalized, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var value))
        {
            throw new FormatException("Введіть число, наприклад 350 або 350.50.");
        }

        return value;
    }

    /// <summary>Дата й час у форматі «дд.мм.рррр гг:хх»; Enter — поточний час </summary>
    public static DateTime ReadDateTime(string prompt)
    {
        while (true)
        {
            var input = ReadLine(prompt);
            if (input.Length == 0)
            {
                var now = DateTime.Now;
                return new DateTime(now.Year, now.Month, now.Day, now.Hour, now.Minute, 0);
            }

            if (DateTime.TryParseExact(input, DateTimeFormats, CultureInfo.InvariantCulture, DateTimeStyles.None, out var value))
            {
                return value;
            }

            Error("Невірний формат. Приклад: 25.11.2026 14:30");
        }
    }

    /// <summary>Дата у форматі «дд.мм.рррр»; Enter — значення за замовчуванням </summary>
    public static DateOnly ReadDate(string prompt, DateOnly defaultValue)
    {
        while (true)
        {
            var input = ReadLine(prompt);
            if (input.Length == 0)
            {
                return defaultValue;
            }

            if (DateOnly.TryParseExact(input, DateFormats, CultureInfo.InvariantCulture, DateTimeStyles.None, out var value))
            {
                return value;
            }

            Error("Невірний формат. Приклад: 18.11.2026");
        }
    }

    public static bool Confirm(string question)
    {
        Console.Write($"{question} (т/н): ");
        var answer = ConsoleInput.ReadLine()?.Trim().ToLowerInvariant();
        return answer == "т" || answer == "так" || answer == "y" || answer == "yes";
    }

    // ---------- Вибір зі списку ----------

    /// <summary>Показує нумерований список. Повертає індекс (з нуля) або null, якщо обрано «0 — Скасувати» </summary>
    public static int? ChooseIndex(string title, IReadOnlyList<string> options)
    {
        Console.WriteLine(title);
        for (var i = 0; i < options.Count; i++)
        {
            Console.WriteLine($"  {i + 1}. {options[i]}");
        }

        Console.WriteLine("  0. Скасувати");

        while (true)
        {
            var input = ReadLine("Номер");
            if (int.TryParse(input, NumberStyles.None, CultureInfo.InvariantCulture, out var number)
                && number >= 0 && number <= options.Count)
            {
                return number == 0 ? null : number - 1;
            }

            Error("Невірний номер. Спробуйте ще раз.");
        }
    }

    public static T? Choose<T>(string title, IReadOnlyList<T> items, Func<T, string> format, string emptyMessage)
        where T : class
    {
        if (items.Count == 0)
        {
            Warn(emptyMessage);
            return null;
        }

        var index = ChooseIndex(title, items.Select(format).ToList());
        return index is null ? null : items[index.Value];
    }

    /// <summary>Вибір кількох елементів: номери через кому; Enter — нічого не обирати.</summary>
    public static IReadOnlyList<T> ChooseMany<T>(
        string title, IReadOnlyList<T> items, Func<T, string> format, string emptyMessage)
    {
        if (items.Count == 0)
        {
            Warn(emptyMessage);
            return Array.Empty<T>();
        }

        Console.WriteLine(title);
        for (var i = 0; i < items.Count; i++)
        {
            Console.WriteLine($"  {i + 1}. {format(items[i])}");
        }

        while (true)
        {
            var input = ReadLine("Номери через кому (Enter — без вибору)");
            if (input.Length == 0)
            {
                return Array.Empty<T>();
            }

            var parts = input.Split(new[] { ',', ';', ' ' }, StringSplitOptions.RemoveEmptyEntries);
            var selected = new List<T>();
            var valid = true;

            foreach (var part in parts)
            {
                if (!int.TryParse(part, NumberStyles.None, CultureInfo.InvariantCulture, out var number)
                    || number < 1 || number > items.Count)
                {
                    Error($"«{part}» — невірний номер. Допустимі значення: від 1 до {items.Count}.");
                    valid = false;
                    break;
                }

                var item = items[number - 1];
                if (!selected.Contains(item))
                {
                    selected.Add(item);
                }
            }

            if (valid)
            {
                return selected;
            }
        }
    }

    // ---------- Внутрішні методи ----------

    private static bool TryConvert<T>(string input, Func<string, T> convert, out T value)
    {
        try
        {
            value = convert(input);
            return true;
        }
        catch (Exception ex) when (ex is VetClinicException || ex is FormatException)
        {
            Error(ex.Message);
            value = default!;
            return false;
        }
    }

    private static void WriteColored(string message, ConsoleColor color)
    {
        var previous = Console.ForegroundColor;
        Console.ForegroundColor = color;
        try
        {
            Console.WriteLine(message);
        }
        finally
        {
            Console.ForegroundColor = previous;
        }
    }

    private static void TryClear()
    {
        try
        {
            Console.Clear();
        }
        catch (IOException)
        {
            // Вивід перенаправлено у файл/канал — очищати нічого.
        }
    }
}
