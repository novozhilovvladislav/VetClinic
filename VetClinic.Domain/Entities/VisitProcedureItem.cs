namespace VetClinic.Domain.Entities;

/// <summary>
/// Рядок процедури всередині візиту.
/// Назва, ціна й собівартість копіюються на момент додавання, тому подальша зміна
/// або видалення довідникової процедури не змінює вартість і прибуток уже зареєстрованих візитів.
/// </summary>
public class VisitProcedureItem
{
    /// <summary>Порожній конструктор потрібен для десеріалізації з JSON</summary>
    public VisitProcedureItem()
    {
    }

    public VisitProcedureItem(Procedure procedure)
    {
        ArgumentNullException.ThrowIfNull(procedure);

        ProcedureId = procedure.Id;
        Name = procedure.Name;
        Price = procedure.Price;
        CostPrice = procedure.CostPrice;
        IsPerformed = false;
    }

    public Guid ProcedureId { get; set; }

    public string Name { get; set; } = string.Empty;

    public decimal Price { get; set; }

    /// <summary>Собівартість на момент додавання. Для візитів із ранніх версій системи дорівнює 0.</summary>
    public decimal CostPrice { get; set; }

    /// <summary>Чи виконано процедуру </summary>
    public bool IsPerformed { get; set; }
}
