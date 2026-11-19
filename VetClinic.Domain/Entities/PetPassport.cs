using System.Text.Json.Serialization;
using VetClinic.Domain.Enums;
using VetClinic.Domain.Exceptions;
using VetClinic.Domain.Validation;

namespace VetClinic.Domain.Entities;

/// <summary>
/// Паспорт тварини: проведені вакцинації, протипаразитні процедури та історія хвороб
/// (лише те, що може обмежувати майбутні процедури). Для однієї тварини існує не більше одного паспорта.
/// </summary>
public class PetPassport : IEntity
{
    private Guid _petId;
    private List<VaccinationRecord> _vaccinations = new List<VaccinationRecord>();
    private List<AntiparasiticRecord> _antiparasiticTreatments = new List<AntiparasiticRecord>();
    private List<MedicalRestriction> _restrictions = new List<MedicalRestriction>();

    public PetPassport()
    {
    }

    public PetPassport(Guid petId)
    {
        PetId = petId;
    }

    public Guid Id { get; init; } = Guid.NewGuid();

    /// <summary>Тварина, якій належить паспорт </summary>
    public Guid PetId
    {
        get => _petId;
        set => _petId = Guard.NotEmpty(value, "Тварина");
    }

    /// <summary>Вакцинації. Найновіші першими </summary>
    [JsonInclude]
    public IReadOnlyList<VaccinationRecord> Vaccinations
    {
        get => _vaccinations;
        private set => _vaccinations = value?.ToList() ?? new List<VaccinationRecord>();
    }

    /// <summary>Протипаразитні процедури. Найновіші першими </summary>
    [JsonInclude]
    public IReadOnlyList<AntiparasiticRecord> AntiparasiticTreatments
    {
        get => _antiparasiticTreatments;
        private set => _antiparasiticTreatments = value?.ToList() ?? new List<AntiparasiticRecord>();
    }

    /// <summary>Історія хвороб, що створюють обмеження. Найновіші першими </summary>
    [JsonInclude]
    public IReadOnlyList<MedicalRestriction> Restrictions
    {
        get => _restrictions;
        private set => _restrictions = value?.ToList() ?? new List<MedicalRestriction>();
    }

    /// <summary>Чи немає в паспорті жодного запису </summary>
    [JsonIgnore]
    public bool IsEmpty =>
        _vaccinations.Count == 0 && _antiparasiticTreatments.Count == 0 && _restrictions.Count == 0;

    public VaccinationRecord AddVaccination(
        string vaccine, DateOnly date, DateOnly? nextDueDate, string? note, DateOnly today)
    {
        var record = new VaccinationRecord(vaccine, date, nextDueDate, note, today);

        _vaccinations.Add(record);
        _vaccinations = _vaccinations.OrderByDescending(v => v.Date).ToList();
        return record;
    }

    public AntiparasiticRecord AddAntiparasitic(
        AntiparasiticKind kind, string preparation, DateOnly date, DateOnly? nextDueDate, string? note, DateOnly today)
    {
        var record = new AntiparasiticRecord(kind, preparation, date, nextDueDate, note, today);

        _antiparasiticTreatments.Add(record);
        _antiparasiticTreatments = _antiparasiticTreatments.OrderByDescending(a => a.Date).ToList();
        return record;
    }

    public MedicalRestriction AddRestriction(
        RestrictionType type,
        string title,
        DateOnly? date,
        string? note,
        IEnumerable<string>? restrictedTags,
        DateOnly today)
    {
        var record = new MedicalRestriction(type, title, date, note, restrictedTags, today);

        _restrictions.Add(record);
        _restrictions = _restrictions.OrderByDescending(r => r.Date ?? DateOnly.MinValue).ToList();
        return record;
    }

    public bool RemoveVaccination(Guid recordId) =>
        _vaccinations.RemoveAll(v => v.Id == recordId) > 0;

    public bool RemoveAntiparasitic(Guid recordId) =>
        _antiparasiticTreatments.RemoveAll(a => a.Id == recordId) > 0;

    public bool RemoveRestriction(Guid recordId) =>
        _restrictions.RemoveAll(r => r.Id == recordId) > 0;

    public void Validate()
    {
        if (Id == Guid.Empty)
        {
            throw new DomainException("Паспорт повинен мати ідентифікатор.");
        }

        Guard.NotEmpty(_petId, "Тварина");

        foreach (var record in _vaccinations)
        {
            record.Validate();
        }

        foreach (var record in _antiparasiticTreatments)
        {
            record.Validate();
        }

        foreach (var record in _restrictions)
        {
            record.Validate();
        }
    }
}
