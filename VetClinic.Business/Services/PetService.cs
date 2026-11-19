using VetClinic.Business.Exceptions;
using VetClinic.Domain.Entities;
using VetClinic.Persistence;

namespace VetClinic.Business.Services;

/// <summary>Операції з тваринами (пацієнтами).</summary>
public class PetService
{
    private readonly IRepository<Pet> _pets;
    private readonly IRepository<Owner> _owners;
    private readonly IRepository<Visit> _visits;
    private readonly IRepository<PetPassport> _passports;

    public PetService(
        IRepository<Pet> pets,
        IRepository<Owner> owners,
        IRepository<Visit> visits,
        IRepository<PetPassport> passports)
    {
        _pets = pets;
        _owners = owners;
        _visits = visits;
        _passports = passports;
    }

    public IReadOnlyList<Pet> GetAll() =>
        _pets.GetAll().OrderBy(p => p.Name, StringComparer.CurrentCultureIgnoreCase).ToList();

    public IReadOnlyList<Pet> GetByOwner(Guid ownerId) =>
        GetAll().Where(p => p.OwnerId == ownerId).ToList();

    public Pet GetById(Guid id) =>
        _pets.GetById(id) ?? throw new BusinessRuleException("Тварину не знайдено.");

    public Pet Create(Guid ownerId, string name, string species, string? breed, int age)
    {
        EnsureOwnerExists(ownerId);

        var pet = new Pet(ownerId, name, species, breed, age);
        pet.Validate();
        EnsureNameIsUniqueForOwner(pet);

        _pets.Add(pet);
        return pet;
    }

    public Pet Update(Guid id, string name, string species, string? breed, int age)
    {
        var pet = GetById(id);
        pet.Name = name;
        pet.Species = species;
        pet.Breed = breed;
        pet.Age = age;

        pet.Validate();
        EnsureNameIsUniqueForOwner(pet);

        _pets.Update(pet);
        return pet;
    }

    public void Delete(Guid id)
    {
        var pet = GetById(id);

        if (_visits.GetAll().Any(v => v.PetId == id))
        {
            throw new BusinessRuleException(
                $"Неможливо видалити тварину «{pet.Name}»: для неї існують візити.");
        }

        _pets.Remove(id);

        // Паспорт належить тварині й без неї не має сенсу — видаляємо разом з нею.
        foreach (var passport in _passports.GetAll().Where(p => p.PetId == id))
        {
            _passports.Remove(passport.Id);
        }
    }

    private void EnsureOwnerExists(Guid ownerId)
    {
        if (_owners.GetById(ownerId) is null)
        {
            throw new BusinessRuleException("Власника не знайдено.");
        }
    }

    private void EnsureNameIsUniqueForOwner(Pet pet)
    {
        var duplicate = _pets.GetAll().Any(p =>
            p.Id != pet.Id &&
            p.OwnerId == pet.OwnerId &&
            string.Equals(p.Name, pet.Name, StringComparison.CurrentCultureIgnoreCase) &&
            string.Equals(p.Species, pet.Species, StringComparison.CurrentCultureIgnoreCase));

        if (duplicate)
        {
            throw new BusinessRuleException(
                $"У цього власника вже є тварина «{pet.Name}» (вид: {pet.Species}).");
        }
    }
}
