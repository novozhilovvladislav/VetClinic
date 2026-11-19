using VetClinic.Domain.Exceptions;
using VetClinic.Domain.Validation;

namespace VetClinic.Domain.Entities;

/// <summary>
/// Спільна основа записів паспорта тварини - власний ідентифікатор і примітка
/// </summary>
public abstract class PassportRecord
{
    public const int MaxNoteLength = 300;

    private string? _note;

    public Guid Id { get; init; } = Guid.NewGuid();

    /// <summary>Примітка (необов’язково) </summary>
    public string? Note
    {
        get => _note;
        set => _note = NormalizeNote(value);
    }

    public static string? NormalizeNote(string? value) =>
        Guard.OptionalText(value, "Примітка", MaxNoteLength);

    /// <summary>Перевірка цілісності запису </summary>
    public abstract void Validate();

    protected void ValidateId()
    {
        if (Id == Guid.Empty)
        {
            throw new DomainException("Запис паспорта повинен мати ідентифікатор.");
        }
    }
}
