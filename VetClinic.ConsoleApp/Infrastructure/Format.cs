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

    public static string DateTimeText(DateTime value) =>
        value.ToString("dd.MM.yyyy HH:mm", CultureInfo.InvariantCulture);

    public static string DateText(DateOnly value) =>
        value.ToString("dd.MM.yyyy", CultureInfo.InvariantCulture);

    public static string DurationText(TimeSpan value)
    {
        var totalMinutes = (int)Math.Round(value.TotalMinutes);
        var hours = totalMinutes / 60;
        var minutes = totalMinutes % 60;

        return hours > 0 ? $"{hours} год {minutes} хв" : $"{minutes} хв";
    }

    public static string Dash(string? value) =>
        string.IsNullOrWhiteSpace(value) ? "—" : value;

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

    public static string ProcedureLine(Procedure procedure) =>
        $"{procedure.Name} — {Money(procedure.Price)}";

    public static string VisitLine(VisitInfo info) =>
        $"#{ShortId(info.Visit.Id)} | {DateTimeText(info.Visit.Date)} | {info.PetName} ({info.OwnerName}) | " +
        $"{info.Visit.Status.ToDisplayName()} | процедур: {info.Visit.Procedures.Count} | {Money(info.Visit.TotalCost)}";
}
