using VetClinic.Business.Models;
using VetClinic.Business.Services;
using VetClinic.ConsoleApp.Infrastructure;
using VetClinic.Domain.Entities;
using VetClinic.Domain.Enums;
using VetClinic.Domain.Extensions;

namespace VetClinic.ConsoleApp.Screens;

/// <summary>
/// Паспорти тварин: перегляд і ведення записів (вакцинації, протипаразитні процедури,
/// історія хвороб, що створюють обмеження для майбутніх процедур).
/// </summary>
public sealed class PassportsScreen
{
    private static readonly AntiparasiticKind[] AntiparasiticKinds =
    {
        AntiparasiticKind.External,
        AntiparasiticKind.Internal,
        AntiparasiticKind.Complex
    };

    private static readonly RestrictionType[] RestrictionTypes =
    {
        RestrictionType.ChronicDisease,
        RestrictionType.Allergy,
        RestrictionType.Surgery
    };

    private readonly PetPassportService _passports;
    private readonly ProcedureService _procedures;
    private readonly Pickers _pickers;

    public PassportsScreen(PetPassportService passports, ProcedureService procedures, Pickers pickers)
    {
        _passports = passports;
        _procedures = procedures;
        _pickers = pickers;
    }

    /// <summary>Обирає тварину і відкриває меню її паспорта.</summary>
    public void Show()
    {
        Pet? selected = null;

        try
        {
            Ui.FormHeader("Паспорт тварини");
            selected = _pickers.ChooseAnyPet();
        }
        catch (UserCancelledException)
        {
            return;
        }

        if (selected is null)
        {
            Ui.Pause();
            return;
        }

        Pet pet = selected;

        Ui.Menu($"Паспорт тварини: {pet.Name}", new[]
        {
            new MenuItem("Переглянути паспорт", () => Ui.Run(() => ShowPassport(pet))),
            new MenuItem("Додати вакцинацію", () => Ui.Run(() => AddVaccination(pet))),
            new MenuItem("Додати протипаразитну процедуру", () => Ui.Run(() => AddAntiparasitic(pet))),
            new MenuItem("Додати запис в історію хвороб (хронічна хвороба / алергія / операція)",
                () => Ui.Run(() => AddRestriction(pet))),
            new MenuItem("Видалити запис", () => Ui.Run(() => RemoveRecord(pet)))
        });
    }

    private void ShowPassport(Pet pet)
    {
        Ui.Header($"Паспорт тварини: {pet.Name}");
        PrintPassport(_passports.GetInfo(pet.Id));
    }

    private void AddVaccination(Pet pet)
    {
        Ui.FormHeader($"Нова вакцинація — {pet.Name}");

        var vaccine = Ui.Read("Вакцина (назва)", VaccinationRecord.NormalizeVaccine);
        var date = Ui.ReadDate("Дата вакцинації (дд.мм.рррр, Enter — сьогодні)", Today());
        var nextDue = Ui.Read("Дата наступної вакцинації (дд.мм.рррр; необов’язково)", Ui.ParseOptionalDate);
        var note = Ui.Read("Примітка (необов’язково)", PassportRecord.NormalizeNote);

        _passports.AddVaccination(pet.Id, vaccine, date, nextDue, note);
        Ui.Success("Вакцинацію додано до паспорта.");
    }

    private void AddAntiparasitic(Pet pet)
    {
        Ui.FormHeader($"Нова протипаразитна процедура — {pet.Name}");

        var kindIndex = Ui.ChooseIndex(
            "Вид обробки:", AntiparasiticKinds.Select(k => k.ToDisplayName()).ToList());
        if (kindIndex is null)
        {
            return;
        }

        Console.WriteLine();
        var preparation = Ui.Read("Препарат (назва)", AntiparasiticRecord.NormalizePreparation);
        var date = Ui.ReadDate("Дата обробки (дд.мм.рррр, Enter — сьогодні)", Today());
        var nextDue = Ui.Read("Дата наступної обробки (дд.мм.рррр; необов’язково)", Ui.ParseOptionalDate);
        var note = Ui.Read("Примітка (необов’язково)", PassportRecord.NormalizeNote);

        _passports.AddAntiparasitic(pet.Id, AntiparasiticKinds[kindIndex.Value], preparation, date, nextDue, note);
        Ui.Success("Протипаразитну процедуру додано до паспорта.");
    }

    private void AddRestriction(Pet pet)
    {
        Ui.FormHeader($"Новий запис в історії хвороб — {pet.Name}");
        Ui.Info("Вносьте лише те, що може обмежувати майбутні процедури.");
        Console.WriteLine();

        var typeIndex = Ui.ChooseIndex(
            "Тип запису:", RestrictionTypes.Select(t => t.ToDisplayName()).ToList());
        if (typeIndex is null)
        {
            return;
        }

        Console.WriteLine();
        var title = Ui.Read("Назва (хвороба / алерген / операція)", MedicalRestriction.NormalizeTitle);
        var date = Ui.Read("Дата (дд.мм.рррр; необов’язково)", Ui.ParseOptionalDate);

        var knownTags = _procedures.GetAllTags();
        if (knownTags.Count > 0)
        {
            Ui.Info($"Наявні теги процедур: {string.Join(", ", knownTags)}");
        }

        var tags = Ui.Read(
            "Теги процедур, яких стосується обмеження (через кому; необов’язково)", MedicalRestriction.ParseTags);
        var note = Ui.Read("Примітка (необов’язково)", PassportRecord.NormalizeNote);

        _passports.AddRestriction(pet.Id, RestrictionTypes[typeIndex.Value], title, date, note, tags);
        Ui.Success("Запис додано до історії хвороб.");
    }

    private void RemoveRecord(Pet pet)
    {
        Ui.FormHeader($"Видалення запису з паспорта — {pet.Name}");

        var info = _passports.GetInfo(pet.Id);

        var category = Ui.ChooseIndex(
            "Який запис видалити?",
            new List<string> { "Вакцинацію", "Протипаразитну процедуру", "Запис історії хвороб" });
        if (category is null)
        {
            return;
        }

        Console.WriteLine();

        switch (category.Value)
        {
            case 0:
            {
                var record = Ui.Choose("Оберіть вакцинацію:", info.Passport.Vaccinations,
                    Format.VaccinationLine, "Записів про вакцинації ще немає.");
                if (record is null || !ConfirmRemoval())
                {
                    return;
                }

                _passports.RemoveVaccination(pet.Id, record.Id);
                break;
            }

            case 1:
            {
                var record = Ui.Choose("Оберіть процедуру:", info.Passport.AntiparasiticTreatments,
                    Format.AntiparasiticLine, "Записів про протипаразитні процедури ще немає.");
                if (record is null || !ConfirmRemoval())
                {
                    return;
                }

                _passports.RemoveAntiparasitic(pet.Id, record.Id);
                break;
            }

            default:
            {
                var record = Ui.Choose("Оберіть запис:", info.Passport.Restrictions,
                    Format.RestrictionLine, "Записів в історії хвороб ще немає.");
                if (record is null || !ConfirmRemoval())
                {
                    return;
                }

                _passports.RemoveRestriction(pet.Id, record.Id);
                break;
            }
        }

        Ui.Success("Запис видалено.");
    }

    private static bool ConfirmRemoval()
    {
        if (Ui.Confirm("Видалити запис?"))
        {
            return true;
        }

        Ui.Warn("Видалення скасовано.");
        return false;
    }

    private static DateOnly Today() => DateOnly.FromDateTime(DateTime.Today);

    private static void PrintPassport(PetPassportInfo info)
    {
        var pet = info.Pet;
        var owner = info.Owner;
        var passport = info.Passport;

        Console.WriteLine("Тварина");
        Console.WriteLine($"  Кличка:   {pet.Name}");
        Console.WriteLine($"  Вид:      {pet.Species}");
        Console.WriteLine($"  Порода:   {Format.Dash(pet.Breed)}");
        Console.WriteLine($"  Вік:      {Format.Years(pet.Age)}");
        Console.WriteLine();

        Console.WriteLine("Власник");
        Console.WriteLine($"  Ім’я:     {owner.Name}");
        Console.WriteLine($"  Телефон:  {owner.Phone}");
        Console.WriteLine($"  Email:    {Format.Dash(owner.Email)}");
        Console.WriteLine();

        PrintSection("Вакцинації", passport.Vaccinations, Format.VaccinationLine,
            "Записів про вакцинації ще немає.");
        PrintSection("Протипаразитні процедури", passport.AntiparasiticTreatments, Format.AntiparasiticLine,
            "Записів про протипаразитні процедури ще немає.");
        PrintSection("Історія хвороб (що може обмежувати процедури)", passport.Restrictions, Format.RestrictionLine,
            "Записів немає: хронічних хвороб, алергій та операцій не зафіксовано.");
    }

    private static void PrintSection<T>(
        string title, IReadOnlyList<T> items, Func<T, string> format, string emptyMessage)
    {
        Console.WriteLine($"{title}:");
        Ui.PrintList(items, format, emptyMessage);
        Console.WriteLine();
    }
}
