using VetClinic.Business.Services;
using VetClinic.ConsoleApp.Infrastructure;
using VetClinic.Domain.Entities;

namespace VetClinic.ConsoleApp.Screens;

/// <summary>Процедури: CRUD, собівартість, теги, блокування </summary>
public sealed class ProceduresScreen
{
    private readonly ProcedureService _procedures;
    private readonly Pickers _pickers;

    public ProceduresScreen(ProcedureService procedures, Pickers pickers)
    {
        _procedures = procedures;
        _pickers = pickers;
    }

    public void Show() => Ui.Menu("Процедури (послуги клініки)", new[]
    {
        new MenuItem("Додати процедуру", () => Ui.Run(Add)),
        new MenuItem("Список процедур", () => Ui.Run(List)),
        new MenuItem("Пошук процедур за тегом", () => Ui.Run(FindByTag)),
        new MenuItem("Редагувати процедуру", () => Ui.Run(Edit)),
        new MenuItem("Заблокувати процедуру (тимчасово недоступна)", () => Ui.Run(Block)),
        new MenuItem("Розблокувати процедуру", () => Ui.Run(Unblock)),
        new MenuItem("Видалити процедуру", () => Ui.Run(Delete))
    });

    private void Add()
    {
        Ui.FormHeader("Нова процедура");

        var name = Ui.Read("Назва", Procedure.NormalizeName);
        var price = Ui.Read("Вартість для клієнта, грн", ParseMoney);
        var costPrice = Ui.Read("Собівартість, грн (0 — якщо невідома)", ParseCost);
        PrintKnownTags();
        var tags = Ui.Read("Теги через кому (наприклад: хірургія, діагностика; необов’язково)", Procedure.ParseTags);

        var procedure = _procedures.Create(name, price, costPrice, tags);
        Ui.Success($"Процедуру «{procedure.Name}» додано (вартість {Format.Money(procedure.Price)}).");
        WarnIfUnprofitable(procedure);
    }

    private void List()
    {
        Ui.Header("Список процедур");
        Ui.PrintList(_procedures.GetAll(), Format.ProcedureFullLine, "Процедур ще немає.");
    }

    private void FindByTag()
    {
        Ui.FormHeader("Пошук процедур за тегом");

        PrintKnownTags();
        var tag = Ui.ReadLine("Тег");

        Console.WriteLine();
        Ui.PrintList(_procedures.FindByTag(tag), Format.ProcedureFullLine, "Процедур з таким тегом немає.");
    }

    private void Edit()
    {
        Ui.FormHeader("Редагування процедури");

        var procedure = ChooseProcedure();
        if (procedure is null)
        {
            return;
        }

        Console.WriteLine();
        var name = Ui.ReadKeep("Назва", procedure.Name, procedure.Name, Procedure.NormalizeName);
        var price = Ui.ReadKeep("Вартість для клієнта, грн", Format.Money(procedure.Price), procedure.Price, ParseMoney);
        var costPrice = Ui.ReadKeep("Собівартість, грн", Format.Money(procedure.CostPrice), procedure.CostPrice, ParseCost);
        PrintKnownTags();
        var tags = Ui.ReadKeep("Теги", Format.TagsText(procedure.Tags), procedure.Tags, Procedure.ParseTags);

        var updated = _procedures.Update(procedure.Id, name, price, costPrice, tags);
        Ui.Success($"Процедуру «{updated.Name}» оновлено. Уже зареєстровані візити зберігають стару вартість і собівартість.");
        WarnIfUnprofitable(updated);
    }

    private void Block()
    {
        Ui.FormHeader("Блокування процедури");

        var procedure = ChooseProcedure(_procedures.GetAvailable(), "Немає процедур, які можна заблокувати.");
        if (procedure is null)
        {
            return;
        }

        Console.WriteLine();
        var reason = Ui.Read("Причина (наприклад, немає препарату; необов’язково)", Procedure.NormalizeBlockReason);

        _procedures.Block(procedure.Id, reason);
        Ui.Success($"Процедуру «{procedure.Name}» заблоковано: її не можна додати до візиту чи позначити виконаною.");
    }

    private void Unblock()
    {
        Ui.FormHeader("Розблокування процедури");

        var blocked = _procedures.GetAll().Where(p => p.IsBlocked).ToList();
        var procedure = ChooseProcedure(blocked, "Заблокованих процедур немає.");
        if (procedure is null)
        {
            return;
        }

        _procedures.Unblock(procedure.Id);
        Ui.Success($"Процедуру «{procedure.Name}» розблоковано.");
    }

    private void Delete()
    {
        Ui.FormHeader("Видалення процедури");

        var procedure = ChooseProcedure();
        if (procedure is null)
        {
            return;
        }

        if (!Ui.Confirm($"Видалити процедуру «{procedure.Name}»?"))
        {
            Ui.Warn("Видалення скасовано.");
            return;
        }

        _procedures.Delete(procedure.Id);
        Ui.Success("Процедуру видалено.");
    }

    private Procedure? ChooseProcedure() =>
        ChooseProcedure(_procedures.GetAll(), "Процедур ще немає.");

    private Procedure? ChooseProcedure(IReadOnlyList<Procedure> procedures, string emptyMessage) =>
        _pickers.ChooseProcedure(procedures, emptyMessage);

    /// <summary>Підказує теги, що вже використовуються, щоб не плодити різні написання одного й того ж.</summary>
    private void PrintKnownTags()
    {
        var known = _procedures.GetAllTags();
        if (known.Count > 0)
        {
            Ui.Info($"Наявні теги: {string.Join(", ", known)}");
        }
    }

    private static void WarnIfUnprofitable(Procedure procedure)
    {
        if (procedure.CostPrice > procedure.Price)
        {
            Ui.Warn("Увага: собівартість вища за вартість для клієнта — процедура збиткова.");
        }
    }

    private static decimal ParseMoney(string input) =>
        Procedure.NormalizePrice(Ui.ParseDecimal(input));

    private static decimal ParseCost(string input) =>
        Procedure.NormalizeCostPrice(Ui.ParseDecimal(input));
}
