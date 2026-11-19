using System.Globalization;
using VetClinic.Business.Models;
using VetClinic.Domain.Entities;
using VetClinic.Domain.Extensions;

namespace VetClinic.ConsoleApp.Infrastructure;

/// <summary>Форматування значень для виводу в консоль </summary>
public static class Format
{
    private static readonly NumberFormatInfo MoneyFormat = new NumberFormatInfo
    {
        NumberGroupSeparator = " ",
        NumberDecimalSeparator = ","
    };

    public static string Money(decimal value) =>
        value.ToString("N2", MoneyFormat) + " грн";

    public static string Percent(double value) =>
        value.ToString("0.0", MoneyFormat) + "%";

    public static string DateTimeText(DateTime value) =>
        value.ToString("dd.MM.yyyy HH:mm", CultureInfo.InvariantCulture);

    public static string DateText(DateOnly value) =>
        value.ToString("dd.MM.yyyy", CultureInfo.InvariantCulture);

    public static string OptionalDateText(DateOnly? value) =>
        value.HasValue ? DateText(value.Value) : "—";

    /// <summary>Діапазон дат: «весь час», один день або «з – по».</summary>
    public static string RangeText(DateRange? range)
    {
        if (range is null)
        {
            return "весь час";
        }

        return range.From == range.To
            ? DateText(range.From)
            : $"{DateText(range.From)} – {DateText(range.To)}";
    }

    public static string PeriodKindText(PeriodKind kind) => kind switch
    {
        PeriodKind.Day => "День",
        PeriodKind.Week => "Тиждень (пн–нд)",
        PeriodKind.Month => "Календарний місяць",
        PeriodKind.Year => "Календарний рік",
        _ => kind.ToString()
    };

    public static string DurationText(TimeSpan value)
    {
        var totalMinutes = (int)Math.Round(value.TotalMinutes);
        var hours = totalMinutes / 60;
        var minutes = totalMinutes % 60;

        return hours > 0 ? $"{hours} год {minutes} хв" : $"{minutes} хв";
    }

    public static string Dash(string? value) =>
        string.IsNullOrWhiteSpace(value) ? "—" : value;

    /// <summary>Обрізає текст до заданої ширини (для вирівнювання таблиць).</summary>
    public static string Fit(string value, int width) =>
        value.Length <= width ? value : value.Substring(0, width - 1) + "…";

    public static string RoomText(int? room) =>
        room.HasValue ? room.Value.ToString(CultureInfo.InvariantCulture) : "—";

    public static string TagsText(IReadOnlyList<string> tags) =>
        tags.Count == 0 ? "—" : string.Join(", ", tags);

    /// <summary>Вік із правильним відмінюванням ¯\_(ツ)_/¯ </summary>
    public static string Years(int age)
    {
        var lastTwo = age % 100;
        var last = age % 10;

        string word;
        if (lastTwo >= 11 && lastTwo <= 14)
        {
            word = "років";
        }
        else if (last == 1)
        {
            word = "рік";
        }
        else if (last >= 2 && last <= 4)
        {
            word = "роки";
        }
        else
        {
            word = "років";
        }

        return $"{age} {word}";
    }

    public static string ShortId(Guid id) => id.ToString("D").Substring(0, 8);

    public static string OwnerLine(Owner owner) =>
        $"{owner.Name} | {owner.Phone} | {Dash(owner.Email)}";

    public static string PetLine(Pet pet) =>
        $"{pet.Name} | {pet.Species} | {Dash(pet.Breed)} | {Years(pet.Age)}";

    public static string VeterinarianLine(Veterinarian veterinarian) =>
        $"{veterinarian.Name} | {veterinarian.Specialization}";

    /// <summary>Короткий рядок процедури для вибору зі списку: назва, ціна, теги, позначка блокування.</summary>
    public static string ProcedureLine(Procedure procedure)
    {
        var text = $"{procedure.Name} — {Money(procedure.Price)}";

        if (procedure.Tags.Count > 0)
        {
            text += $" | теги: {TagsText(procedure.Tags)}";
        }

        if (procedure.IsBlocked)
        {
            text += " | [ЗАБЛОКОВАНО]";
        }

        return text;
    }

    /// <summary>Повний рядок процедури для довідника: додатково собівартість і причина блокування.</summary>
    public static string ProcedureFullLine(Procedure procedure)
    {
        var text = $"{procedure.Name} — {Money(procedure.Price)} | собівартість: {Money(procedure.CostPrice)}" +
                   $" | теги: {TagsText(procedure.Tags)}";

        if (procedure.IsBlocked)
        {
            text += string.IsNullOrWhiteSpace(procedure.BlockReason)
                ? " | [ЗАБЛОКОВАНО]"
                : $" | [ЗАБЛОКОВАНО: {procedure.BlockReason}]";
        }

        return text;
    }

    public static string VisitLine(VisitInfo info) =>
        $"#{ShortId(info.Visit.Id)} | {DateTimeText(info.Visit.Date)} | {info.PetName} ({info.OwnerName}) | " +
        $"лікар: {info.VeterinarianName} | каб. {RoomText(info.Visit.Room)} | " +
        $"{info.Visit.Status.ToDisplayName()} | процедур: {info.Visit.Procedures.Count} | {Money(info.Visit.TotalCost)}";

    public static string VaccinationLine(VaccinationRecord record)
    {
        var text = $"{DateText(record.Date)} — {record.Vaccine}";

        if (record.NextDueDate.HasValue)
        {
            text += $" (наступна: {DateText(record.NextDueDate.Value)})";
        }

        if (!string.IsNullOrWhiteSpace(record.Note))
        {
            text += $" | {record.Note}";
        }

        return text;
    }

    public static string AntiparasiticLine(AntiparasiticRecord record)
    {
        var text = $"{DateText(record.Date)} — {record.Kind.ToDisplayName()}: {record.Preparation}";

        if (record.NextDueDate.HasValue)
        {
            text += $" (наступна: {DateText(record.NextDueDate.Value)})";
        }

        if (!string.IsNullOrWhiteSpace(record.Note))
        {
            text += $" | {record.Note}";
        }

        return text;
    }

    public static string RestrictionLine(MedicalRestriction record)
    {
        var text = $"{record.Type.ToDisplayName()}: {record.Title}";

        if (record.Date.HasValue)
        {
            text += $" ({DateText(record.Date.Value)})";
        }

        if (record.RestrictedTags.Count > 0)
        {
            text += $" | обмежує процедури з тегами: {TagsText(record.RestrictedTags)}";
        }

        if (!string.IsNullOrWhiteSpace(record.Note))
        {
            text += $" | {record.Note}";
        }

        return text;
    }
}
