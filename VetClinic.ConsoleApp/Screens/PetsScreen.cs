using VetClinic.Business.Services;
using VetClinic.ConsoleApp.Infrastructure;
using VetClinic.Domain.Entities;

namespace VetClinic.ConsoleApp.Screens;

/// <summary>Тварини: додавання, перегляд, редагування, видалення.</summary>
public sealed class PetsScreen
{
    private readonly PetService _pets;
    private readonly OwnerService _owners;
    private readonly Pickers _pickers;

    public PetsScreen(PetService pets, OwnerService owners, Pickers pickers)
    {
        _pets = pets;
        _owners = owners;
        _pickers = pickers;
    }

    public void Show() => Ui.Menu("Тварини", new[]
    {
        new MenuItem("Додати тварину", () => Ui.Run(Add)),
        new MenuItem("Тварини власника", () => Ui.Run(ListByOwner)),
        new MenuItem("Усі тварини", () => Ui.Run(ListAll)),
        new MenuItem("Редагувати тварину", () => Ui.Run(Edit)),
        new MenuItem("Видалити тварину", () => Ui.Run(Delete))
    });

    private void Add()
    {
        Ui.FormHeader("Нова тварина");

        var owner = _pickers.ChooseOwner();
        if (owner is null)
        {
            return;
        }

        Console.WriteLine();
        var name = Ui.Read("Кличка", Pet.NormalizeName);
        var species = Ui.Read("Вид (наприклад: пес, кіт, папуга)", Pet.NormalizeSpecies);
        var breed = Ui.Read("Порода (необов’язково)", Pet.NormalizeBreed);
        var age = Ui.Read("Вік (повних років, 0 — якщо менше року)", input => Pet.NormalizeAge(Ui.ParseInt(input)));

        var pet = _pets.Create(owner.Id, name, species, breed, age);
        Ui.Success($"Тварину «{pet.Name}» додано власнику «{owner.Name}».");
    }

    private void ListByOwner()
    {
        Ui.Header("Тварини власника");

        var owner = _pickers.ChooseOwner();
        if (owner is null)
        {
            return;
        }

        Console.WriteLine();
        Ui.PrintList(_pets.GetByOwner(owner.Id), Format.PetLine, "У цього власника ще немає тварин.");
    }

    private void ListAll()
    {
        Ui.Header("Усі тварини");

        var owners = _owners.GetAll().ToDictionary(o => o.Id, o => o.Name);

        string Describe(Pet pet)
        {
            owners.TryGetValue(pet.OwnerId, out var ownerName);
            return $"{Format.PetLine(pet)} | власник: {ownerName ?? "—"}";
        }

        Ui.PrintList(_pets.GetAll(), Describe, "Тварин ще немає.");
    }

    private void Edit()
    {
        Ui.FormHeader("Редагування тварини");

        var pet = _pickers.ChooseAnyPet();
        if (pet is null)
        {
            return;
        }

        Console.WriteLine();
        var name = Ui.ReadKeep("Кличка", pet.Name, pet.Name, Pet.NormalizeName);
        var species = Ui.ReadKeep("Вид", pet.Species, pet.Species, Pet.NormalizeSpecies);
        var breed = Ui.ReadKeep("Порода", Format.Dash(pet.Breed), pet.Breed, Pet.NormalizeBreed);
        var age = Ui.ReadKeep("Вік (повних років)", pet.Age.ToString(), pet.Age,
            input => Pet.NormalizeAge(Ui.ParseInt(input)));

        var updated = _pets.Update(pet.Id, name, species, breed, age);
        Ui.Success($"Дані тварини «{updated.Name}» оновлено.");
    }

    private void Delete()
    {
        Ui.FormHeader("Видалення тварини");

        var pet = _pickers.ChooseAnyPet();
        if (pet is null)
        {
            return;
        }

        if (!Ui.Confirm($"Видалити тварину «{pet.Name}»?"))
        {
            Ui.Warn("Видалення скасовано.");
            return;
        }

        _pets.Delete(pet.Id);
        Ui.Success("Тварину видалено.");
    }
}
