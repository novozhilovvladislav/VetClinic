namespace VetClinic.Domain.Entities;

/// <summary>Сутність із унікальним ідентифікатором </summary>
public interface IEntity
{
    Guid Id { get; }
}
