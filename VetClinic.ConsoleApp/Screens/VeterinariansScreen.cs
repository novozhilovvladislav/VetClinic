using VetClinic.Business.Services;
using VetClinic.ConsoleApp.Infrastructure;
using VetClinic.Domain.Entities;

namespace VetClinic.ConsoleApp.Screens;

/// <summary>Лікарі: додавання, перегляд, редагування, видалення </summary>
public sealed class VeterinariansScreen
{
    private readonly VeterinarianService _veterinarians;
    private readonly Pickers _pickers;

    public VeterinariansScreen(VeterinarianService veterinarians, Pickers pickers)
    {
        _veterinarians = veterinarians;
        _pickers = pickers;
    }

    public void Show() => Ui.Menu("Лікарі", new[]
    {
        new MenuItem("Додати лікаря", () => Ui.Run(Add)),
        new MenuItem("Список лікарів", () => Ui.Run(List)),
        new MenuItem("Редагувати лікаря", () => Ui.Run(Edit)),
        new MenuItem("Видалити лікаря", () => Ui.Run(Delete))
    });

    private void Add()
    {
        Ui.FormHeader("Новий лікар");

        var name = Ui.Read("Ім’я та прізвище", Veterinarian.NormalizeName);
        var specialization = Ui.Read("Спеціалізація (наприклад: терапевт, хірург)", Veterinarian.NormalizeSpecialization);

        var veterinarian = _veterinarians.Create(name, specialization);
        Ui.Success($"Лікаря «{veterinarian.Name}» додано.");
    }

    private void List()
    {
        Ui.Header("Список лікарів");
        Ui.PrintList(_veterinarians.GetAll(), Format.VeterinarianLine, "Лікарів ще немає.");
    }

    private void Edit()
    {
        Ui.FormHeader("Редагування лікаря");

        var veterinarian = _pickers.ChooseVeterinarian();
        if (veterinarian is null)
        {
            return;
        }

        Console.WriteLine();
        var name = Ui.ReadKeep("Ім’я та прізвище", veterinarian.Name, veterinarian.Name, Veterinarian.NormalizeName);
        var specialization = Ui.ReadKeep("Спеціалізація", veterinarian.Specialization, veterinarian.Specialization,
            Veterinarian.NormalizeSpecialization);

        var updated = _veterinarians.Update(veterinarian.Id, name, specialization);
        Ui.Success($"Дані лікаря «{updated.Name}» оновлено.");
    }

    private void Delete()
    {
        Ui.FormHeader("Видалення лікаря");

        var veterinarian = _pickers.ChooseVeterinarian();
        if (veterinarian is null)
        {
            return;
        }

        if (!Ui.Confirm($"Видалити лікаря «{veterinarian.Name}»?"))
        {
            Ui.Warn("Видалення скасовано.");
            return;
        }

        _veterinarians.Delete(veterinarian.Id);
        Ui.Success("Лікаря видалено.");
    }
}
