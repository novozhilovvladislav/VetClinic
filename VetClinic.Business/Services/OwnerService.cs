using VetClinic.Business.Exceptions;
using VetClinic.Domain.Entities;
using VetClinic.Persistence;

namespace VetClinic.Business.Services;

/// <summary>Операції з власниками тварин.</summary>
public class OwnerService
{
    private readonly IRepository<Owner> _owners;
    private readonly IRepository<Pet> _pets;

    public OwnerService(IRepository<Owner> owners, IRepository<Pet> pets)
    {
        _owners = owners;
        _pets = pets;
    }

    public IReadOnlyList<Owner> GetAll() =>
        _owners.GetAll().OrderBy(o => o.Name, StringComparer.CurrentCultureIgnoreCase).ToList();

    public Owner GetById(Guid id) =>
        _owners.GetById(id) ?? throw new BusinessRuleException("Власника не знайдено.");

    public Owner Create(string name, string phone, string? email)
    {
        var owner = new Owner(name, phone, email);
        owner.Validate();
        EnsurePhoneIsUnique(owner);

        _owners.Add(owner);
        return owner;
    }

    public Owner Update(Guid id, string name, string phone, string? email)
    {
        var owner = GetById(id);
        owner.Name = name;
        owner.Phone = phone;
        owner.Email = email;

        owner.Validate();
        EnsurePhoneIsUnique(owner);

        _owners.Update(owner);
        return owner;
    }

    public void Delete(Guid id)
    {
        var owner = GetById(id);

        if (_pets.GetAll().Any(p => p.OwnerId == id))
        {
            throw new BusinessRuleException(
                $"Неможливо видалити власника «{owner.Name}»: за ним закріплені тварини. Спочатку видаліть їх.");
        }

        _owners.Remove(id);
    }

    private void EnsurePhoneIsUnique(Owner owner)
    {
        var duplicate = _owners.GetAll().FirstOrDefault(o => o.Id != owner.Id && o.PhoneKey == owner.PhoneKey);
        if (duplicate is not null)
        {
            throw new BusinessRuleException(
                $"Власник із таким телефоном уже існує: {duplicate.Name} ({duplicate.Phone}).");
        }
    }
}
