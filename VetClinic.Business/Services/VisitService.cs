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
/// </summary>
public class VisitService
{
    private readonly IRepository<Visit> _visits;
    private readonly IRepository<Owner> _owners;
    private readonly IRepository<Pet> _pets;
    private readonly IRepository<Procedure> _procedures;
    private readonly TimeProvider _timeProvider;

    public VisitService(
        IRepository<Visit> visits,
        IRepository<Owner> owners,
        IRepository<Pet> pets,
        IRepository<Procedure> procedures,
        TimeProvider? timeProvider = null)
    {
        _visits = visits;
        _owners = owners;
        _pets = pets;
        _procedures = procedures;
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
    /// Реєструє візит: прив’язує власника і тварину, обирає процедури,
    /// генерує унікальний ідентифікатор і ставить статус «Зареєстрований».
    /// </summary>
    public VisitInfo Register(Guid ownerId, Guid petId, DateTime date, IEnumerable<Guid> procedureIds)
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

        var visit = new Visit(owner.Id, pet.Id, date, procedures, Now);
        visit.Validate();

        _visits.Add(visit);
        return ToInfo(visit);
    }

    /// <summary>Додає процедуру до візиту, який ще не завершено і не скасовано.</summary>
    public VisitInfo AddProcedure(Guid visitId, Guid procedureId)
    {
        var procedure = _procedures.GetById(procedureId)
                        ?? throw new BusinessRuleException("Процедуру не знайдено.");

        return Modify(visitId, visit => visit.AddProcedure(procedure));
    }

    public VisitInfo MarkProcedurePerformed(Guid visitId, Guid procedureId) =>
        Modify(visitId, visit => visit.MarkProcedurePerformed(procedureId));

    /// <summary>
    /// Змінює статус візиту. Для «Завершено» перевіряє, що всі процедури виконані,
    /// і фіксує час завершення. Кожна зміна потрапляє в історію статусів.
    /// </summary>
    public VisitInfo ChangeStatus(Guid visitId, VisitStatus newStatus, string? comment = null)
    {
        var now = Now;

        return Modify(visitId, visit =>
        {
            switch (newStatus)
            {
                case VisitStatus.InProgress:
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

        var result = new List<VisitInfo>();
        foreach (var visit in visits)
        {
            owners.TryGetValue(visit.OwnerId, out var ownerName);
            pets.TryGetValue(visit.PetId, out var petName);

            result.Add(new VisitInfo(visit, ownerName ?? "—", petName ?? "—"));
        }

        return result;
    }
}
