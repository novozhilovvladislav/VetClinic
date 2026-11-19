using System.Text.Json.Serialization;
using VetClinic.Domain.Enums;
using VetClinic.Domain.Exceptions;
using VetClinic.Domain.Extensions;
using VetClinic.Domain.Validation;

namespace VetClinic.Domain.Entities;

/// <summary>
/// Візит до клініки: прив’язує власника і тварину, лікаря та кабінет, містить обрані процедури, поточний статус та історію його змін.
/// </summary>
public class Visit : IEntity
{
    public const int MaxCommentLength = 300;
    public const int MinRoom = 1;
    public const int MaxRoom = 999;

    private DateTime _date;
    private Guid _ownerId;
    private Guid _petId;
    private List<VisitProcedureItem> _procedures = new List<VisitProcedureItem>();
    private List<StatusChange> _statusHistory = new List<StatusChange>();

    public Visit()
    {
    }

    /// <summary>Створює новий візит зі статусом «Зареєстрований». Лікар і кабінет вказуються обов’язково </summary>
    public Visit(
        Guid ownerId,
        Guid petId,
        Guid veterinarianId,
        int room,
        DateTime date,
        IEnumerable<Procedure> procedures,
        DateTime now)
    {
        ArgumentNullException.ThrowIfNull(procedures);

        OwnerId = ownerId;
        PetId = petId;
        VeterinarianId = Guard.NotEmpty(veterinarianId, "Лікар");
        Room = NormalizeRoom(room);
        Date = date;

        foreach (var procedure in procedures)
        {
            AddProcedure(procedure);
        }

        if (_procedures.Count == 0)
        {
            throw new DomainException("Візит має містити принаймні одну процедуру.");
        }

        _statusHistory.Add(new StatusChange(null, VisitStatus.Registered, now));
    }

    /// <summary>Унікальний ідентифікатор візиту </summary>
    public Guid Id { get; init; } = Guid.NewGuid();

    /// <summary>Дата й час візиту </summary>
    public DateTime Date
    {
        get => _date;
        set
        {
            if (value == default)
            {
                throw new DomainException("Дата візиту: значення обов’язкове.");
            }

            _date = value;
        }
    }

    public Guid OwnerId
    {
        get => _ownerId;
        set => _ownerId = Guard.NotEmpty(value, "Власник");
    }

    public Guid PetId
    {
        get => _petId;
        set => _petId = Guard.NotEmpty(value, "Тварина");
    }

    /// <summary>
    /// Лікар, який проводить прийом
    /// </summary>
    [JsonInclude]
    public Guid? VeterinarianId { get; private set; }

    /// <summary>Номер кабінету </summary>
    [JsonInclude]
    public int? Room { get; private set; }

    [JsonInclude]
    public VisitStatus Status { get; private set; } = VisitStatus.Registered;

    /// <summary>Час завершення візиту, заповнюється при переході в статус «Завершено» </summary>
    [JsonInclude]
    public DateTime? CompletedAt { get; private set; }

    /// <summary>Процедури візиту </summary>
    [JsonInclude]
    public IReadOnlyList<VisitProcedureItem> Procedures
    {
        get => _procedures;
        private set => _procedures = value?.ToList() ?? new List<VisitProcedureItem>();
    }

    /// <summary>Хронологічна історія змін статусу </summary>
    [JsonInclude]
    public IReadOnlyList<StatusChange> StatusHistory
    {
        get => _statusHistory;
        private set => _statusHistory = value?.ToList() ?? new List<StatusChange>();
    }

    /// <summary>
    /// Загальна вартість візиту — сума цін процедур. Ціни зафіксовані в рядках візиту, тому після завершення візиту сума більше не змінюється
    /// </summary>
    [JsonIgnore]
    public decimal TotalCost => _procedures.Sum(p => p.Price);

    [JsonIgnore]
    public bool AllProceduresPerformed => _procedures.Count > 0 && _procedures.All(p => p.IsPerformed);

    /// <summary>Коли візит було розпочато - перехід у статус «В процесі» </summary>
    [JsonIgnore]
    public DateTime? StartedAt => _statusHistory
        .Where(h => h.To == VisitStatus.InProgress)
        .Select(h => (DateTime?)h.ChangedAt)
        .FirstOrDefault();

    /// <summary>Тривалість прийому — від початку до завершення (лише для завершених візитів) </summary>
    [JsonIgnore]
    public TimeSpan? ServiceDuration =>
        StartedAt.HasValue && CompletedAt.HasValue ? CompletedAt.Value - StartedAt.Value : null;

    public static string? NormalizeComment(string? value) =>
        Guard.OptionalText(value, "Коментар", MaxCommentLength);

    public static int NormalizeRoom(int value) =>
        Guard.InRange(value, MinRoom, MaxRoom, "Кабінет");

    /// <summary>
    /// Змінює лікаря та кабінет. Дозволено лише для візиту у статусі «Зареєстрований» — коли прийом розпочато, лікар і кабінет уже зайняті цим візитом.
    /// </summary>
    public void Reassign(Guid veterinarianId, int room)
    {
        if (Status != VisitStatus.Registered)
        {
            throw new DomainException(
                $"Змінити лікаря або кабінет можна лише у статусі «{VisitStatus.Registered.ToDisplayName()}».");
        }

        VeterinarianId = Guard.NotEmpty(veterinarianId, "Лікар");
        Room = NormalizeRoom(room);
    }

    /// <summary>Додає процедуру до візиту, лише поки візит не завершено і не скасовано </summary>
    public void AddProcedure(Procedure procedure)
    {
        ArgumentNullException.ThrowIfNull(procedure);
        EnsureNotFinal();

        if (procedure.IsBlocked)
        {
            var reason = string.IsNullOrWhiteSpace(procedure.BlockReason) ? string.Empty : $" ({procedure.BlockReason})";
            throw new DomainException($"Процедура «{procedure.Name}» тимчасово недоступна{reason}.");
        }

        if (_procedures.Any(p => p.ProcedureId == procedure.Id))
        {
            throw new DomainException($"Процедуру «{procedure.Name}» уже додано до візиту.");
        }

        _procedures.Add(new VisitProcedureItem(procedure));
    }

    /// <summary>Позначає процедуру виконаною </summary>
    public void MarkProcedurePerformed(Guid procedureId)
    {
        if (Status != VisitStatus.InProgress)
        {
            throw new DomainException(
                $"Позначати процедури виконаними можна лише у статусі «{VisitStatus.InProgress.ToDisplayName()}».");
        }

        var item = _procedures.FirstOrDefault(p => p.ProcedureId == procedureId)
                   ?? throw new DomainException("Такої процедури немає у цьому візиті.");

        if (item.IsPerformed)
        {
            throw new DomainException($"Процедуру «{item.Name}» уже позначено виконаною.");
        }

        item.IsPerformed = true;
    }

    /// <summary>Розпочинає прийом. Для цього мають бути вказані лікар і кабінет </summary>
    public void Start(DateTime now, string? comment = null)
    {
        EnsureCanTransitionTo(VisitStatus.InProgress);

        if (VeterinarianId is null || Room is null)
        {
            throw new DomainException(
                "Неможливо розпочати візит: не вказано лікаря або кабінет. Призначте їх у меню «Змінити лікаря або кабінет».");
        }

        ChangeStatus(VisitStatus.InProgress, now, comment);
    }

    /// <summary>
    /// Перевіряє, що всі процедури виконані, і фіксує час завершення
    /// </summary>
    public void Complete(DateTime now, string? comment = null)
    {
        EnsureCanTransitionTo(VisitStatus.Completed);

        var notPerformed = _procedures.Where(p => !p.IsPerformed).ToList();
        if (notPerformed.Count > 0)
        {
            var names = string.Join(", ", notPerformed.Select(p => $"«{p.Name}»"));
            throw new DomainException($"Неможливо завершити візит: не виконано процедури — {names}.");
        }

        ChangeStatus(VisitStatus.Completed, now, comment);
    }

    /// <summary>Скасувати візит </summary>
    public void Cancel(DateTime now, string? reason = null)
    {
        ChangeStatus(VisitStatus.Cancelled, now, reason);
    }

    public void Validate()
    {
        if (Id == Guid.Empty)
        {
            throw new DomainException("Візит повинен мати ідентифікатор.");
        }

        if (_ownerId == Guid.Empty || _petId == Guid.Empty)
        {
            throw new DomainException("Візит має бути прив’язаний до власника і тварини.");
        }

        if (_date == default)
        {
            throw new DomainException("Дата візиту: значення обов’язкове.");
        }

        if (_procedures.Count == 0)
        {
            throw new DomainException("Візит має містити принаймні одну процедуру.");
        }

        if (Status == VisitStatus.Completed && CompletedAt is null)
        {
            throw new DomainException("Для завершеного візиту має бути вказано час завершення.");
        }
    }

    private void ChangeStatus(VisitStatus newStatus, DateTime now, string? comment)
    {
        EnsureCanTransitionTo(newStatus);

        var previous = Status;
        var normalizedComment = NormalizeComment(comment);

        Status = newStatus;
        if (newStatus == VisitStatus.Completed)
        {
            CompletedAt = now;
        }

        _statusHistory.Add(new StatusChange(previous, newStatus, now, normalizedComment));
    }

    private void EnsureCanTransitionTo(VisitStatus newStatus)
    {
        if (!Status.CanTransitionTo(newStatus))
        {
            throw new DomainException(
                $"Недопустима зміна статусу: «{Status.ToDisplayName()}» → «{newStatus.ToDisplayName()}».");
        }
    }

    private void EnsureNotFinal()
    {
        if (Status.IsFinal())
        {
            throw new DomainException(
                $"Візит зі статусом «{Status.ToDisplayName()}» змінювати не можна.");
        }
    }
}
