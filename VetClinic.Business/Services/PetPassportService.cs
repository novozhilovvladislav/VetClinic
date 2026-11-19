using VetClinic.Business.Exceptions;
using VetClinic.Business.Models;
using VetClinic.Domain.Entities;
using VetClinic.Domain.Enums;
using VetClinic.Persistence;

namespace VetClinic.Business.Services;

/// <summary>
/// Паспорти тварин: перегляд, додавання та видалення записів (вакцинації, протипаразитні обробки,
/// історія хвороб), а також перевірка обраних процедур на збіг з обмеженнями з паспорта.
/// Паспорт створюється під час додавання першого запису; до цього тварина має «порожній» паспорт.
/// </summary>
public class PetPassportService
{
    private readonly IRepository<PetPassport> _passports;
    private readonly IRepository<Pet> _pets;
    private readonly IRepository<Owner> _owners;
    private readonly TimeProvider _timeProvider;

    public PetPassportService(
        IRepository<PetPassport> passports,
        IRepository<Pet> pets,
        IRepository<Owner> owners,
        TimeProvider? timeProvider = null)
    {
        _passports = passports;
        _pets = pets;
        _owners = owners;
        _timeProvider = timeProvider ?? TimeProvider.System;
    }

    private DateOnly Today => DateOnly.FromDateTime(_timeProvider.GetLocalNow().DateTime);

    /// <summary>Паспорт тварини з даними тварини та власника (для відображення).</summary>
    public PetPassportInfo GetInfo(Guid petId)
    {
        var pet = GetPet(petId);
        var owner = GetOwner(pet);
        var passport = FindPassport(petId) ?? new PetPassport(petId);

        return new PetPassportInfo(pet, owner, passport);
    }

    public PetPassportInfo AddVaccination(
        Guid petId, string vaccine, DateOnly date, DateOnly? nextDueDate, string? note) =>
        Modify(petId, (passport, today) => passport.AddVaccination(vaccine, date, nextDueDate, note, today));

    public PetPassportInfo AddAntiparasitic(
        Guid petId, AntiparasiticKind kind, string preparation, DateOnly date, DateOnly? nextDueDate, string? note) =>
        Modify(petId, (passport, today) =>
            passport.AddAntiparasitic(kind, preparation, date, nextDueDate, note, today));

    public PetPassportInfo AddRestriction(
        Guid petId,
        RestrictionType type,
        string title,
        DateOnly? date,
        string? note,
        IEnumerable<string>? restrictedTags) =>
        Modify(petId, (passport, today) =>
            passport.AddRestriction(type, title, date, note, restrictedTags, today));

    public PetPassportInfo RemoveVaccination(Guid petId, Guid recordId) =>
        Modify(petId, (passport, _) => EnsureRemoved(passport.RemoveVaccination(recordId)));

    public PetPassportInfo RemoveAntiparasitic(Guid petId, Guid recordId) =>
        Modify(petId, (passport, _) => EnsureRemoved(passport.RemoveAntiparasitic(recordId)));

    public PetPassportInfo RemoveRestriction(Guid petId, Guid recordId) =>
        Modify(petId, (passport, _) => EnsureRemoved(passport.RemoveRestriction(recordId)));

    /// <summary>
    /// Шукає обмеження з паспорта, що стосуються обраних процедур: обмеження і процедура
    /// мають спільний тег. Порожній результат означає, що конфліктів немає.
    /// </summary>
    public IReadOnlyList<RestrictionConflict> FindConflicts(Guid petId, IEnumerable<Procedure> procedures)
    {
        ArgumentNullException.ThrowIfNull(procedures);

        var conflicts = new List<RestrictionConflict>();

        var passport = FindPassport(petId);
        if (passport is null || passport.Restrictions.Count == 0)
        {
            return conflicts;
        }

        foreach (var procedure in procedures)
        {
            foreach (var restriction in passport.Restrictions)
            {
                var sharedTag = restriction.RestrictedTags.FirstOrDefault(tag => procedure.HasTag(tag));
                if (sharedTag is not null)
                {
                    conflicts.Add(new RestrictionConflict(
                        procedure.Name, restriction.Type, restriction.Title, sharedTag));
                }
            }
        }

        return conflicts;
    }

    private PetPassportInfo Modify(Guid petId, Action<PetPassport, DateOnly> change)
    {
        var pet = GetPet(petId);
        var owner = GetOwner(pet);

        var existing = FindPassport(petId);
        var passport = existing ?? new PetPassport(petId);

        change(passport, Today);
        passport.Validate();

        if (existing is null)
        {
            _passports.Add(passport);
        }
        else
        {
            _passports.Update(passport);
        }

        return new PetPassportInfo(pet, owner, passport);
    }

    private static void EnsureRemoved(bool removed)
    {
        if (!removed)
        {
            throw new BusinessRuleException("Запис не знайдено.");
        }
    }

    private PetPassport? FindPassport(Guid petId) =>
        _passports.GetAll().FirstOrDefault(p => p.PetId == petId);

    private Pet GetPet(Guid petId) =>
        _pets.GetById(petId) ?? throw new BusinessRuleException("Тварину не знайдено.");

    private Owner GetOwner(Pet pet) =>
        _owners.GetById(pet.OwnerId) ?? throw new BusinessRuleException("Власника тварини не знайдено.");
}
