using VetClinic.Business.Services;
using VetClinic.ConsoleApp.Infrastructure;
using VetClinic.Domain.Entities;

namespace VetClinic.ConsoleApp.Screens;

/// <summary>Власники: додавання, перегляд, редагування, видалення </summary>
public sealed class OwnersScreen
{
    private readonly OwnerService _owners;
    private readonly Pickers _pickers;

    public OwnersScreen(OwnerService owners, Pickers pickers)
    {
        _owners = owners;
        _pickers = pickers;
    }

    public void Show() => Ui.Menu("Власники тварин", new[]
    {
        new MenuItem("Додати власника", () => Ui.Run(Add)),
        new MenuItem("Список власників", () => Ui.Run(List)),
        new MenuItem("Редагувати власника", () => Ui.Run(Edit)),
        new MenuItem("Видалити власника", () => Ui.Run(Delete))
    });

    private void Add()
    {
        Ui.FormHeader("Новий власник");

        var name = Ui.Read("Ім’я та прізвище", Owner.NormalizeName);
        var phone = Ui.Read("Телефон (наприклад, +380501234567)", Owner.NormalizePhone);
        var email = Ui.Read("Email (необов’язково)", Owner.NormalizeEmail);

        var owner = _owners.Create(name, phone, email);
        Ui.Success($"Власника «{owner.Name}» додано.");
    }

    private void List()
    {
        Ui.Header("Список власників");
        Ui.PrintList(_owners.GetAll(), Format.OwnerLine, "Власників ще немає.");
    }

    private void Edit()
    {
        Ui.FormHeader("Редагування власника");

        var owner = _pickers.ChooseOwner();
        if (owner is null)
        {
            return;
        }

        Console.WriteLine();
        var name = Ui.ReadKeep("Ім’я та прізвище", owner.Name, owner.Name, Owner.NormalizeName);
        var phone = Ui.ReadKeep("Телефон", owner.Phone, owner.Phone, Owner.NormalizePhone);
        var email = Ui.ReadKeep("Email", Format.Dash(owner.Email), owner.Email, Owner.NormalizeEmail);

        var updated = _owners.Update(owner.Id, name, phone, email);
        Ui.Success($"Дані власника «{updated.Name}» оновлено.");
    }

    private void Delete()
    {
        Ui.FormHeader("Видалення власника");

        var owner = _pickers.ChooseOwner();
        if (owner is null)
        {
            return;
        }

        if (!Ui.Confirm($"Видалити власника «{owner.Name}»?"))
        {
            Ui.Warn("Видалення скасовано.");
            return;
        }

        _owners.Delete(owner.Id);
        Ui.Success("Власника видалено.");
    }
}
