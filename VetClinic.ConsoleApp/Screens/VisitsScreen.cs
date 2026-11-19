using VetClinic.Business.Models;
using VetClinic.Business.Services;
using VetClinic.ConsoleApp.Infrastructure;
using VetClinic.Domain.Entities;
using VetClinic.Domain.Enums;
using VetClinic.Domain.Extensions;

namespace VetClinic.ConsoleApp.Screens;

/// <summary>Візити: реєстрація, перегляд, зміна статусів, лікар і кабінет, процедури візиту, закриття прийому.</summary>
public sealed class VisitsScreen
{
    private readonly VisitService _visits;
    private readonly ProcedureService _procedures;
    private readonly PetPassportService _passports;
    private readonly Pickers _pickers;

    public VisitsScreen(
        VisitService visits,
        ProcedureService procedures,
        PetPassportService passports,
        Pickers pickers)
    {
        _visits = visits;
        _procedures = procedures;
        _passports = passports;
        _pickers = pickers;
    }

    public void Show() => Ui.Menu("Візити", new[]
    {
        new MenuItem("Зареєструвати візит", () => Ui.Run(Register)),
        new MenuItem("Усі візити", () => Ui.Run(ListAll)),
        new MenuItem("Активні візити (зареєстровані та в процесі)", () => Ui.Run(ListActive)),
        new MenuItem("Деталі візиту та історія статусів", () => Ui.Run(ShowDetails)),
        new MenuItem("Змінити статус (розпочати / завершити / скасувати)", () => Ui.Run(ChangeStatus)),
        new MenuItem("Змінити лікаря або кабінет (для зареєстрованих візитів)", () => Ui.Run(Reassign)),
        new MenuItem("Додати процедуру до візиту", () => Ui.Run(AddProcedure)),
        new MenuItem("Позначити процедуру виконаною", () => Ui.Run(MarkProcedurePerformed))
    });

    private void Register()
    {
        Ui.FormHeader("Реєстрація візиту");

        var owner = _pickers.ChooseOwner();
        if (owner is null)
        {
            return;
        }

        Console.WriteLine();
        var pet = _pickers.ChoosePetOfOwner(owner);
        if (pet is null)
        {
            return;
        }

        Console.WriteLine();
        var veterinarian = _pickers.ChooseVeterinarian();
        if (veterinarian is null)
        {
            return;
        }

        Console.WriteLine();
        var room = Ui.Read("Кабінет (номер від 1 до 999)", ParseRoom);

        Console.WriteLine();
        var date = Ui.ReadDateTime("Дата й час візиту (дд.мм.рррр гг:хх, Enter — зараз)");

        Console.WriteLine();
        var procedures = _pickers.ChooseProcedures();
        if (procedures.Count == 0)
        {
            Ui.Warn("Не обрано жодної процедури — візит не зареєстровано.");
            return;
        }

        if (!ConfirmRestrictions(pet.Id, procedures))
        {
            Ui.Warn("Візит не зареєстровано.");
            return;
        }

        var info = _visits.Register(owner.Id, pet.Id, veterinarian.Id, room, date, procedures.Select(p => p.Id));

        Console.WriteLine();
        Ui.Success("Візит зареєстровано.");
        PrintDetails(info);
    }

    private void ListAll()
    {
        Ui.Header("Усі візити");
        Ui.PrintList(_visits.GetAll(), Format.VisitLine, "Візитів ще немає.");
    }

    private void ListActive()
    {
        Ui.Header("Активні візити");
        Ui.PrintList(_visits.GetActive(), Format.VisitLine, "Активних візитів немає.");
    }

    private void ShowDetails()
    {
        Ui.FormHeader("Деталі візиту");

        var info = ChooseVisit(_visits.GetAll(), "Візитів ще немає.");
        if (info is null)
        {
            return;
        }

        Console.WriteLine();
        PrintDetails(info);
    }

    private void ChangeStatus()
    {
        Ui.FormHeader("Зміна статусу візиту");

        var info = ChooseVisit(_visits.GetActive(), "Немає візитів, статус яких можна змінити.");
        if (info is null)
        {
            return;
        }

        var visit = info.Visit;
        var options = visit.Status.AllowedNext();

        Console.WriteLine();
        var index = Ui.ChooseIndex(
            $"Поточний статус: «{visit.Status.ToDisplayName()}». Оберіть новий статус:",
            options.Select(s => s.ToDisplayName()).ToList());
        if (index is null)
        {
            return;
        }

        var target = options[index.Value];
        var comment = Ui.Read("Коментар (необов’язково)", Visit.NormalizeComment);

        var updated = _visits.ChangeStatus(visit.Id, target, comment);

        Console.WriteLine();
        Ui.Success($"Статус змінено на «{updated.Visit.Status.ToDisplayName()}».");

        if (updated.Visit.Status == VisitStatus.Completed)
        {
            Console.WriteLine($"Загальна вартість: {Format.Money(updated.Visit.TotalCost)}");
            Console.WriteLine($"Час завершення:    {Format.DateTimeText(updated.Visit.CompletedAt!.Value)}");

            if (updated.Visit.ServiceDuration is { } duration)
            {
                Console.WriteLine($"Тривалість прийому: {Format.DurationText(duration)}");
            }
        }
    }

    private void Reassign()
    {
        Ui.FormHeader("Зміна лікаря або кабінету");

        var info = ChooseVisit(
            _visits.GetByStatus(VisitStatus.Registered),
            "Немає візитів у статусі «Зареєстрований».");
        if (info is null)
        {
            return;
        }

        Console.WriteLine($"Зараз: лікар — {info.VeterinarianName}, кабінет — {Format.RoomText(info.Visit.Room)}.");

        Console.WriteLine();
        var veterinarian = _pickers.ChooseVeterinarian();
        if (veterinarian is null)
        {
            return;
        }

        Console.WriteLine();
        var currentRoom = info.Visit.Room;
        var room = currentRoom.HasValue
            ? Ui.ReadKeep("Кабінет", Format.RoomText(currentRoom), currentRoom.Value, ParseRoom)
            : Ui.Read("Кабінет (номер від 1 до 999)", ParseRoom);

        var updated = _visits.Reassign(info.Visit.Id, veterinarian.Id, room);
        Ui.Success($"Призначено: лікар — {updated.VeterinarianName}, кабінет — {Format.RoomText(updated.Visit.Room)}.");
    }

    private void AddProcedure()
    {
        Ui.FormHeader("Додавання процедури до візиту");

        var info = ChooseVisit(_visits.GetActive(), "Немає візитів, до яких можна додати процедуру.");
        if (info is null)
        {
            return;
        }

        var alreadyInVisit = info.Visit.Procedures.Select(p => p.ProcedureId).ToHashSet();
        var available = _procedures.GetAvailable().Where(p => !alreadyInVisit.Contains(p.Id)).ToList();

        Console.WriteLine();
        var procedure = _pickers.ChooseProcedure(
            available, "Немає доступних процедур, які ще не додано до цього візиту.");
        if (procedure is null)
        {
            return;
        }

        if (!ConfirmRestrictions(info.Visit.PetId, new[] { procedure }))
        {
            Ui.Warn("Процедуру не додано.");
            return;
        }

        var updated = _visits.AddProcedure(info.Visit.Id, procedure.Id);
        Ui.Success($"Процедуру «{procedure.Name}» додано. Загальна вартість візиту: {Format.Money(updated.Visit.TotalCost)}.");
    }

    private void MarkProcedurePerformed()
    {
        Ui.FormHeader("Позначити процедуру виконаною");

        var inProgress = _visits.GetByStatus(VisitStatus.InProgress);
        var info = ChooseVisit(inProgress, "Немає візитів у статусі «В процесі». Спочатку розпочніть візит.");
        if (info is null)
        {
            return;
        }

        var pending = info.Visit.Procedures.Where(p => !p.IsPerformed).ToList();

        Console.WriteLine();
        var item = Ui.Choose("Оберіть виконану процедуру:", pending,
            p => $"{p.Name} — {Format.Money(p.Price)}", "Усі процедури цього візиту вже виконано.");
        if (item is null)
        {
            return;
        }

        var updated = _visits.MarkProcedurePerformed(info.Visit.Id, item.ProcedureId);
        Ui.Success($"Процедуру «{item.Name}» позначено виконаною.");

        if (updated.Visit.AllProceduresPerformed)
        {
            Ui.Info("Усі процедури виконано — візит можна завершити (пункт «Змінити статус»).");
        }
    }

    private static VisitInfo? ChooseVisit(IReadOnlyList<VisitInfo> visits, string emptyMessage) =>
        Ui.Choose("Оберіть візит:", visits, Format.VisitLine, emptyMessage);

    private static int ParseRoom(string input) =>
        Visit.NormalizeRoom(Ui.ParseInt(input));

    /// <summary>
    /// Перевіряє обрані процедури на збіг з обмеженнями з паспорта тварини (хронічні хвороби,
    /// алергії, операції). Якщо збіги є, показує їх і просить підтвердження: рішення за лікарем.
    /// </summary>
    private bool ConfirmRestrictions(Guid petId, IEnumerable<Procedure> procedures)
    {
        var conflicts = _passports.FindConflicts(petId, procedures);
        if (conflicts.Count == 0)
        {
            return true;
        }

        Console.WriteLine();
        Ui.Warn("Увага! У паспорті тварини є обмеження, що стосуються обраних процедур:");
        foreach (var conflict in conflicts)
        {
            Ui.Warn($"  • «{conflict.ProcedureName}» — {conflict.Type.ToDisplayName()}: {conflict.Title} (тег «{conflict.Tag}»)");
        }

        Console.WriteLine();
        return Ui.Confirm("Усе одно продовжити?");
    }

    private static void PrintDetails(VisitInfo info)
    {
        var visit = info.Visit;

        Console.WriteLine($"ID візиту:     {visit.Id}");
        Console.WriteLine($"Дата й час:    {Format.DateTimeText(visit.Date)}");
        Console.WriteLine($"Власник:       {info.OwnerName}");
        Console.WriteLine($"Тварина:       {info.PetName}");
        Console.WriteLine($"Лікар:         {info.VeterinarianName}");
        Console.WriteLine($"Кабінет:       {Format.RoomText(visit.Room)}");
        Console.WriteLine($"Статус:        {visit.Status.ToDisplayName()}");

        Console.WriteLine("Процедури:");
        foreach (var item in visit.Procedures)
        {
            var mark = item.IsPerformed ? "[x]" : "[ ]";
            Console.WriteLine($"  {mark} {item.Name} — {Format.Money(item.Price)}");
        }

        Console.WriteLine($"Разом:         {Format.Money(visit.TotalCost)}");

        if (visit.CompletedAt is { } completedAt)
        {
            Console.WriteLine($"Завершено:     {Format.DateTimeText(completedAt)}");
        }

        if (visit.ServiceDuration is { } duration)
        {
            Console.WriteLine($"Тривалість:    {Format.DurationText(duration)}");
        }

        Console.WriteLine("Історія статусів:");
        foreach (var change in visit.StatusHistory)
        {
            var previousStatus = change.From.HasValue ? change.From.Value.ToDisplayName() : "—";
            var comment = string.IsNullOrWhiteSpace(change.Comment) ? string.Empty : $"  ({change.Comment})";

            Console.WriteLine(
                $"  {Format.DateTimeText(change.ChangedAt)}  {previousStatus} → {change.To.ToDisplayName()}{comment}");
        }
    }
}
