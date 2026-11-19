using VetClinic.Business.Exceptions;
using VetClinic.Domain.Entities;
using VetClinic.Persistence;

namespace VetClinic.Business.Services;

/// <summary>
/// CRUD для довідника процедур (послуг) клініки, а також теги, собівартість і блокування процедур.
/// </summary>
public class ProcedureService
{
    private readonly IRepository<Procedure> _procedures;

    public ProcedureService(IRepository<Procedure> procedures)
    {
        _procedures = procedures;
    }

    public IReadOnlyList<Procedure> GetAll() =>
        _procedures.GetAll().OrderBy(p => p.Name, StringComparer.CurrentCultureIgnoreCase).ToList();

    /// <summary>Процедури, які можна обирати для візитів: заблоковані виключаються.</summary>
    public IReadOnlyList<Procedure> GetAvailable() =>
        GetAll().Where(p => !p.IsBlocked).ToList();

    /// <summary>Процедури з указаним тегом (регістр не важливий).</summary>
    public IReadOnlyList<Procedure> FindByTag(string tag)
    {
        if (string.IsNullOrWhiteSpace(tag))
        {
            return new List<Procedure>();
        }

        return GetAll().Where(p => p.HasTag(tag)).ToList();
    }

    /// <summary>Усі теги, що вже використовуються в довіднику, за алфавітом.</summary>
    public IReadOnlyList<string> GetAllTags() =>
        GetAll()
            .SelectMany(p => p.Tags)
            .Distinct()
            .OrderBy(t => t, StringComparer.CurrentCultureIgnoreCase)
            .ToList();

    public Procedure GetById(Guid id) =>
        _procedures.GetById(id) ?? throw new BusinessRuleException("Процедуру не знайдено.");

    public Procedure Create(string name, decimal price, decimal costPrice = 0m, IEnumerable<string>? tags = null)
    {
        var procedure = new Procedure(name, price, costPrice, tags);
        procedure.Validate();
        EnsureNameIsUnique(procedure);

        _procedures.Add(procedure);
        return procedure;
    }

    public Procedure Update(Guid id, string name, decimal price, decimal costPrice, IEnumerable<string>? tags)
    {
        var procedure = GetById(id);
        procedure.Name = name;
        procedure.Price = price;
        procedure.CostPrice = costPrice;
        procedure.Tags = Procedure.NormalizeTags(tags);

        procedure.Validate();
        EnsureNameIsUnique(procedure);

        _procedures.Update(procedure);
        return procedure;
    }

    /// <summary>
    /// Тимчасово блокує процедуру (наприклад, немає препаратів): її не можна додати до візиту
    /// чи позначити виконаною, доки її не розблоковано.
    /// </summary>
    public Procedure Block(Guid id, string? reason)
    {
        var procedure = GetById(id);
        procedure.Block(reason);

        _procedures.Update(procedure);
        return procedure;
    }

    public Procedure Unblock(Guid id)
    {
        var procedure = GetById(id);
        procedure.Unblock();

        _procedures.Update(procedure);
        return procedure;
    }

    /// <summary>
    /// Видаляє процедуру з довідника. Уже зареєстровані візити не постраждають:
    /// вони зберігають власну копію назви, ціни й собівартості процедури.
    /// </summary>
    public void Delete(Guid id)
    {
        GetById(id);
        _procedures.Remove(id);
    }

    private void EnsureNameIsUnique(Procedure procedure)
    {
        var duplicate = _procedures.GetAll().Any(p =>
            p.Id != procedure.Id &&
            string.Equals(p.Name, procedure.Name, StringComparison.CurrentCultureIgnoreCase));

        if (duplicate)
        {
            throw new BusinessRuleException($"Процедура «{procedure.Name}» уже існує.");
        }
    }
}
