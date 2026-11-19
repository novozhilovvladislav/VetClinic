using VetClinic.Business.Exceptions;
using VetClinic.Business.Models;
using VetClinic.Domain.Entities;
using VetClinic.Domain.Enums;
using VetClinic.Domain.Extensions;
using VetClinic.Persistence;

namespace VetClinic.Business.Services;

/// <summary>
/// Реєстрація візитів, зміна їх статусів, додавання процедур і закриття прийомів.
/// Правила переходів між статусами інкапсульовані у <see cref="Visit"/>, сервіс лише
/// завантажує візит, викликає потрібну операцію та зберігає результат.
/// Додатково сервіс стежить, щоб кабінет і лікар не були зайняті двома прийомами одночасно.
/// </summary>
public class VisitService
{
    private readonly IRepository<Visit> _visits;
    private readonly IRepository<Owner> _owners;
    private readonly IRepository<Pet> _pets;
    private readonly IRepository<Procedure> _procedures;
    private readonly IRepository<Veterinarian> _veterinarians;
    private readonly TimeProvider _timeProvider;

    public VisitService(
        IRepository<Visit> visits,
        IRepository<Owner> owners,
        IRepository<Pet> pets,
        IRepository<Procedure> procedures,
        IRepository<Veterinarian> veterinarians,
        TimeProvider? timeProvider = null)
    {
        _visits = visits;
        _owners = owners;
        _pets = pets;
        _procedures = procedures;
        _veterinarians = veterinarians;
        _timeProvider = timeProvider ?? TimeProvider.System;
    }

    private DateTime Now => _timeProvider.GetLocalNow().DateTime;

    /// <summary>Усі візити: найновіші — першими.</summary>
    public IReadOnlyList<VisitInfo> GetAll() =>
        ToInfo(_visits.GetAll().OrderByDescending(v => v.Date));

    /// <summary>Візити, які ще не завершені і не скасовані: найближчі — першими.</summary>
    public IReadOnlyList<VisitInfo> GetActive() =>
        ToInfo(_visits.GetAll().Where(v => !v.Status.IsFinal()).OrderBy(v => v.Date));

    public IReadOnlyList<VisitInfo> GetByStatus(VisitStatus status) =>
        ToInfo(_visits.GetAll().Where(v => v.Status == status).OrderByDescending(v => v.Date));

    public VisitInfo GetById(Guid id) => ToInfo(GetVisit(id));

    /// <summary>
    /// Реєструє візит: прив’язує власника, тварину, лікаря та кабінет, обирає процедури,
    /// генерує унікальний ідентифікатор і ставить статус «Зареєстрований».
    /// </summary>
    public VisitInfo Register(
        Guid ownerId,
        Guid petId,
        Guid veterinarianId,
        int room,
        DateTime date,
        IEnumerable<Guid> procedureIds)
    {
        ArgumentNullException.ThrowIfNull(procedureIds);

        var owner = _owners.GetById(ownerId)
                    ?? throw new BusinessRuleException("Власника не знайдено.");
        var pet = _pets.GetById(petId)
                  ?? throw new BusinessRuleException("Тварину не знайдено.");

        if (pet.OwnerId != owner.Id)
        {
            throw new BusinessRuleException(
                $"Тварина «{pet.Name}» не належить власнику «{owner.Name}».");
        }

        var veterinarian = GetVeterinarian(veterinarianId);

        EnsureDateIsReasonable(date);

        var ids = procedureIds.Distinct().ToList();
        if (ids.Count == 0)
        {
            throw new BusinessRuleException("Оберіть принаймні одну процедуру.");
        }

        var procedures = new List<Procedure>();
        foreach (var id in ids)
        {
            procedures.Add(_procedures.GetById(id)
                           ?? throw new BusinessRuleException("Одну з обраних процедур не знайдено."));
        }

        // Заблоковані процедури відхиляє сам візит (Visit.AddProcedure).
        var visit = new Visit(owner.Id, pet.Id, veterinarian.Id, room, date, procedures, Now);
        visit.Validate();

        _visits.Add(visit);
        return ToInfo(visit);
    }

    /// <summary>
    /// Змінює лікаря та кабінет візиту. Можливо лише поки візит «Зареєстрований»;
    /// також дозволяє призначити лікаря й кабінет візитам, створеним у ранніх версіях системи.
    /// </summary>
    public VisitInfo Reassign(Guid visitId, Guid veterinarianId, int room)
    {
        var veterinarian = GetVeterinarian(veterinarianId);

        return Modify(visitId, visit => visit.Reassign(veterinarian.Id, room));
    }

    /// <summary>Додає процедуру до візиту, який ще не завершено і не скасовано.</summary>
    public VisitInfo AddProcedure(Guid visitId, Guid procedureId)
    {
        var procedure = _procedures.GetById(procedureId)
                        ?? throw new BusinessRuleException("Процедуру не знайдено.");

        return Modify(visitId, visit => visit.AddProcedure(procedure));
    }

    /// <summary>
    /// Позначає процедуру візиту виконаною. Якщо процедуру в довіднику заблоковано
    /// (наприклад, немає препаратів), виконати її неможливо, доки її не розблоковано.
    /// </summary>
    public VisitInfo MarkProcedurePerformed(Guid visitId, Guid procedureId)
    {
        var procedure = _procedures.GetById(procedureId);
        if (procedure is not null && procedure.IsBlocked)
        {
            var reason = string.IsNullOrWhiteSpace(procedure.BlockReason) ? string.Empty : $" ({procedure.BlockReason})";
            throw new BusinessRuleException(
                $"Процедура «{procedure.Name}» тимчасово недоступна{reason}: " +
                "її не можна виконати, доки вона заблокована.");
        }

        return Modify(visitId, visit => visit.MarkProcedurePerformed(procedureId));
    }

    /// <summary>
    /// Змінює статус візиту. Для «В процесі» перевіряє, що кабінет і лікар вільні;
    /// для «Завершено» — що всі процедури виконані, і фіксує час завершення.
    /// Кожна зміна потрапляє в історію статусів.
    /// </summary>
    public VisitInfo ChangeStatus(Guid visitId, VisitStatus newStatus, string? comment = null)
    {
        var now = Now;

        return Modify(visitId, visit =>
        {
            switch (newStatus)
            {
                case VisitStatus.InProgress:
                    EnsureRoomAndVeterinarianAreFree(visit);
                    visit.Start(now, comment);
                    break;
                case VisitStatus.Completed:
                    visit.Complete(now, comment);
                    break;
                case VisitStatus.Cancelled:
                    visit.Cancel(now, comment);
                    break;
                default:
                    throw new BusinessRuleException(
                        $"Повернути візит у статус «{newStatus.ToDisplayName()}» не можна.");
            }
        });
    }

    private VisitInfo Modify(Guid visitId, Action<Visit> change)
    {
        var visit = GetVisit(visitId);

        change(visit);
        visit.Validate();

        _visits.Update(visit);
        return ToInfo(visit);
    }

    private Visit GetVisit(Guid id) =>
        _visits.GetById(id) ?? throw new BusinessRuleException("Візит не знайдено.");

    private Veterinarian GetVeterinarian(Guid id) =>
        _veterinarians.GetById(id) ?? throw new BusinessRuleException("Лікаря не знайдено.");

    /// <summary>
    /// Один кабінет не може приймати двох пацієнтів одночасно, а лікар — вести два прийоми одночасно.
    /// Якщо лікаря чи кабінет не вказано, відмову повертає <see cref="Visit.Start"/>.
    /// </summary>
    private void EnsureRoomAndVeterinarianAreFree(Visit visit)
    {
        var running = _visits.GetAll()
            .Where(v => v.Status == VisitStatus.InProgress && v.Id != visit.Id)
            .ToList();

        if (visit.Room.HasValue)
        {
            var sameRoom = running.FirstOrDefault(v => v.Room == visit.Room);
            if (sameRoom is not null)
            {
                throw new BusinessRuleException(
                    $"Кабінет {visit.Room.Value} зайнятий: триває візит #{ShortId(sameRoom.Id)}. " +
                    "Дочекайтеся завершення або оберіть інший кабінет.");
            }
        }

        if (visit.VeterinarianId.HasValue)
        {
            var sameVeterinarian = running.FirstOrDefault(v => v.VeterinarianId == visit.VeterinarianId);
            if (sameVeterinarian is not null)
            {
                throw new BusinessRuleException(
                    $"Лікар зайнятий: триває візит #{ShortId(sameVeterinarian.Id)}. " +
                    "Дочекайтеся завершення або призначте іншого лікаря.");
            }
        }
    }

    private static string ShortId(Guid id) => id.ToString("D").Substring(0, 8);

    /// <summary>
    /// Дата візиту має бути «розумною»: не раніше ніж рік тому (внесення минулих записів)
    /// і не пізніше ніж за рік наперед.
    /// </summary>
    private void EnsureDateIsReasonable(DateTime date)
    {
        var now = Now;

        if (date < now.AddYears(-1))
        {
            throw new BusinessRuleException("Дата візиту не може бути раніше ніж рік тому.");
        }

        if (date > now.AddYears(1))
        {
            throw new BusinessRuleException("Дата візиту не може бути пізніше ніж через рік.");
        }
    }

    private VisitInfo ToInfo(Visit visit) => ToInfo(new[] { visit })[0];

    private IReadOnlyList<VisitInfo> ToInfo(IEnumerable<Visit> visits)
    {
        var owners = _owners.GetAll().ToDictionary(o => o.Id, o => o.Name);
        var pets = _pets.GetAll().ToDictionary(p => p.Id, p => p.Name);
        var veterinarians = _veterinarians.GetAll().ToDictionary(v => v.Id, v => v.Name);

        var result = new List<VisitInfo>();
        foreach (var visit in visits)
        {
            owners.TryGetValue(visit.OwnerId, out var ownerName);
            pets.TryGetValue(visit.PetId, out var petName);

            string? veterinarianName = null;
            if (visit.VeterinarianId.HasValue)
            {
                veterinarians.TryGetValue(visit.VeterinarianId.Value, out veterinarianName);
            }

            result.Add(new VisitInfo(visit, ownerName ?? "—", petName ?? "—", veterinarianName ?? "—"));
        }

        return result;
    }
}
