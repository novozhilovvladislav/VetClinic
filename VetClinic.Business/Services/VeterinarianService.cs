using VetClinic.Business.Exceptions;
using VetClinic.Domain.Entities;
using VetClinic.Persistence;

namespace VetClinic.Business.Services;

/// <summary>Операції з лікарями клініки.</summary>
public class VeterinarianService
{
    private readonly IRepository<Veterinarian> _veterinarians;
    private readonly IRepository<Visit> _visits;

    public VeterinarianService(IRepository<Veterinarian> veterinarians, IRepository<Visit> visits)
    {
        _veterinarians = veterinarians;
        _visits = visits;
    }

    public IReadOnlyList<Veterinarian> GetAll() =>
        _veterinarians.GetAll().OrderBy(v => v.Name, StringComparer.CurrentCultureIgnoreCase).ToList();

    public Veterinarian GetById(Guid id) =>
        _veterinarians.GetById(id) ?? throw new BusinessRuleException("Лікаря не знайдено.");

    public Veterinarian Create(string name, string specialization)
    {
        var veterinarian = new Veterinarian(name, specialization);
        veterinarian.Validate();
        EnsureIsUnique(veterinarian);

        _veterinarians.Add(veterinarian);
        return veterinarian;
    }

    public Veterinarian Update(Guid id, string name, string specialization)
    {
        var veterinarian = GetById(id);
        veterinarian.Name = name;
        veterinarian.Specialization = specialization;

        veterinarian.Validate();
        EnsureIsUnique(veterinarian);

        _veterinarians.Update(veterinarian);
        return veterinarian;
    }

    public void Delete(Guid id)
    {
        var veterinarian = GetById(id);

        if (_visits.GetAll().Any(v => v.VeterinarianId == id))
        {
            throw new BusinessRuleException(
                $"Неможливо видалити лікаря «{veterinarian.Name}»: за ним закріплені візити.");
        }

        _veterinarians.Remove(id);
    }

    private void EnsureIsUnique(Veterinarian veterinarian)
    {
        var duplicate = _veterinarians.GetAll().Any(v =>
            v.Id != veterinarian.Id &&
            string.Equals(v.Name, veterinarian.Name, StringComparison.CurrentCultureIgnoreCase) &&
            string.Equals(v.Specialization, veterinarian.Specialization, StringComparison.CurrentCultureIgnoreCase));

        if (duplicate)
        {
            throw new BusinessRuleException(
                $"Лікар «{veterinarian.Name}» зі спеціалізацією «{veterinarian.Specialization}» уже існує.");
        }
    }
}
