using VetClinic.Business.Services;
using VetClinic.ConsoleApp.Infrastructure;
using VetClinic.Domain.Entities;

namespace VetClinic.ConsoleApp.Screens;

/// <summary>Процедури </summary>
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
        new MenuItem("Редагувати процедуру", () => Ui.Run(Edit)),
        new MenuItem("Видалити процедуру", () => Ui.Run(Delete))
    });

    private void Add()
    {
        Ui.FormHeader("Нова процедура");

        var name = Ui.Read("Назва", Procedure.NormalizeName);
        var price = Ui.Read("Вартість, грн", ParsePrice);

        var procedure = _procedures.Create(name, price);
        Ui.Success($"Процедуру «{procedure.Name}» додано (вартість {Format.Money(procedure.Price)}).");
    }

    private void List()
    {
        Ui.Header("Список процедур");
        Ui.PrintList(_procedures.GetAll(), Format.ProcedureLine, "Процедур ще немає.");
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
        var price = Ui.ReadKeep("Вартість, грн", Format.Money(procedure.Price), procedure.Price, ParsePrice);

        var updated = _procedures.Update(procedure.Id, name, price);
        Ui.Success($"Процедуру «{updated.Name}» оновлено. Уже зареєстровані візити зберігають стару вартість.");
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
        _pickers.ChooseProcedure(_procedures.GetAll(), "Процедур ще немає.");

    private static decimal ParsePrice(string input) =>
        Procedure.NormalizePrice(Ui.ParseDecimal(input));
}
