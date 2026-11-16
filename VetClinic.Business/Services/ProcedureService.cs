using VetClinic.Business.Exceptions;
using VetClinic.Domain.Entities;
using VetClinic.Persistence;

namespace VetClinic.Business.Services;

/// <summary>CRUD для довідника процедур (послуг) клініки.</summary>
public class ProcedureService
{
    private readonly IRepository<Procedure> _procedures;

    public ProcedureService(IRepository<Procedure> procedures)
    {
        _procedures = procedures;
    }

    public IReadOnlyList<Procedure> GetAll() =>
        _procedures.GetAll().OrderBy(p => p.Name, StringComparer.CurrentCultureIgnoreCase).ToList();

    public Procedure GetById(Guid id) =>
        _procedures.GetById(id) ?? throw new BusinessRuleException("Процедуру не знайдено.");

    public Procedure Create(string name, decimal price)
    {
        var procedure = new Procedure(name, price);
        procedure.Validate();
        EnsureNameIsUnique(procedure);

        _procedures.Add(procedure);
        return procedure;
    }

    public Procedure Update(Guid id, string name, decimal price)
    {
        var procedure = GetById(id);
        procedure.Name = name;
        procedure.Price = price;

        procedure.Validate();
        EnsureNameIsUnique(procedure);

        _procedures.Update(procedure);
        return procedure;
    }

    /// <summary>
    /// Видаляє процедуру з довідника. Уже зареєстровані візити не постраждають:
    /// вони зберігають власну копію назви й ціни процедури.
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
