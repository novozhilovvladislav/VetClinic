using VetClinic.Business.Services;
using VetClinic.Domain.Entities;

namespace VetClinic.ConsoleApp.Infrastructure;

/// <summary>Інтерактивний вибір власника, тварини, лікаря та процедур зі списків </summary>
public sealed class Pickers
{
    private readonly OwnerService _owners;
    private readonly PetService _pets;
    private readonly ProcedureService _procedures;
    private readonly VeterinarianService _veterinarians;

    public Pickers(
        OwnerService owners,
        PetService pets,
        ProcedureService procedures,
        VeterinarianService veterinarians)
    {
        _owners = owners;
        _pets = pets;
        _procedures = procedures;
        _veterinarians = veterinarians;
    }

    public Owner? ChooseOwner() =>
        Ui.Choose("Оберіть власника:", _owners.GetAll(), Format.OwnerLine,
            "Власників ще немає. Спочатку додайте власника.");

    public Pet? ChoosePetOfOwner(Owner owner) =>
        Ui.Choose($"Оберіть тварину власника «{owner.Name}»:", _pets.GetByOwner(owner.Id), Format.PetLine,
            "У цього власника ще немає тварин. Додайте тварину в розділі «Тварини».");

    public Pet? ChooseAnyPet()
    {
        var owners = _owners.GetAll().ToDictionary(o => o.Id, o => o.Name);

        string Describe(Pet pet)
        {
            owners.TryGetValue(pet.OwnerId, out var ownerName);
            return $"{Format.PetLine(pet)} | власник: {ownerName ?? "—"}";
        }

        return Ui.Choose("Оберіть тварину:", _pets.GetAll(), Describe, "Тварин ще немає.");
    }

    public Veterinarian? ChooseVeterinarian() =>
        Ui.Choose("Оберіть лікаря:", _veterinarians.GetAll(), Format.VeterinarianLine,
            "Лікарів ще немає. Спочатку додайте лікаря в розділі «Лікарі».");

    public Procedure? ChooseProcedure(IReadOnlyList<Procedure> procedures, string emptyMessage) =>
        Ui.Choose("Оберіть процедуру:", procedures, Format.ProcedureLine, emptyMessage);

    /// <summary>Вибір процедур для візиту - заблоковані у список не потрапляють.</summary>
    public IReadOnlyList<Procedure> ChooseProcedures() =>
        Ui.ChooseMany("Оберіть процедури візиту:", _procedures.GetAvailable(), Format.ProcedureLine,
            "Немає доступних процедур: довідник порожній або всі процедури заблоковані.");
}
