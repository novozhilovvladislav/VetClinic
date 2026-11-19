using VetClinic.Domain.Entities;

namespace VetClinic.Persistence;

public interface IRepository<T> where T : class, IEntity
{
    IReadOnlyList<T> GetAll();

    T? GetById(Guid id);

    void Add(T entity);

    void Update(T entity);

    bool Remove(Guid id);
}
