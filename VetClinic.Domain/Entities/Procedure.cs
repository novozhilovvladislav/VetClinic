using System.Text.Json.Serialization;
using VetClinic.Domain.Exceptions;
using VetClinic.Domain.Validation;

namespace VetClinic.Domain.Entities;


public class Procedure : IEntity
{
    public const int MaxNameLength = 100;
    public const decimal MaxPrice = 1_000_000m;
    public const int MaxBlockReasonLength = 200;

    private const string TagsField = "Теги процедури";

    private string _name = string.Empty;
    private decimal _price;
    private decimal _costPrice;
    private List<string> _tags = new List<string>();
    private string? _blockReason;

    public Procedure()
    {
    }

    public Procedure(string name, decimal price, decimal costPrice = 0m, IEnumerable<string>? tags = null)
    {
        Name = name;
        Price = price;
        CostPrice = costPrice;
        _tags = new List<string>(NormalizeTags(tags));
    }

    public Guid Id { get; init; } = Guid.NewGuid();

    /// <summary>Назва процедури </summary>
    public string Name
    {
        get => _name;
        set => _name = NormalizeName(value);
    }

    /// <summary>Вартість для клієнта у гривнях, від 0, два знаки після коми </summary>
    public decimal Price
    {
        get => _price;
        set => _price = NormalizePrice(value);
    }

    /// <summary>
    /// Собівартість — витрати клініки на виконання процедури (препарати, матеріали)
    /// </summary>
    public decimal CostPrice
    {
        get => _costPrice;
        set => _costPrice = NormalizeCostPrice(value);
    }

    /// <summary>Теги процедури («хірургія», «діагностика», «вакцинація» тощо), малими літерами </summary>
    public IReadOnlyList<string> Tags
    {
        get => _tags;
        set => _tags = new List<string>(NormalizeTags(value));
    }

    /// <summary>Процедуру тимчасово заблоковано (наприклад, ремонт обладнання) </summary>
    [JsonInclude]
    public bool IsBlocked { get; private set; }

    /// <summary>Причина блокування (необов’язково) </summary>
    [JsonInclude]
    public string? BlockReason
    {
        get => _blockReason;
        private set => _blockReason = NormalizeBlockReason(value);
    }

    public static string NormalizeName(string? value) =>
        Guard.RequiredText(value, "Назва процедури", MaxNameLength);

    public static decimal NormalizePrice(decimal value) =>
        Guard.Money(value, "Вартість", MaxPrice);

    public static decimal NormalizeCostPrice(decimal value) =>
        Guard.Money(value, "Собівартість", MaxPrice);

    public static IReadOnlyList<string> NormalizeTags(IEnumerable<string>? tags) =>
        TagNormalizer.Normalize(tags, TagsField);

    /// <summary>Розбирає рядок із тегами через кому або крапку з комою </summary>
    public static IReadOnlyList<string> ParseTags(string? text) =>
        TagNormalizer.Parse(text, TagsField);

    public static string? NormalizeBlockReason(string? value) =>
        Guard.OptionalText(value, "Причина блокування", MaxBlockReasonLength);

    /// <summary>Чи має процедура вказаний тег (регістр не важливий) </summary>
    public bool HasTag(string tag)
    {
        if (string.IsNullOrWhiteSpace(tag))
        {
            return false;
        }

        return _tags.Contains(tag.Trim().ToLowerInvariant());
    }

    /// <summary>Блокує процедуру: її не можна додавати до візитів і позначати виконаною </summary>
    public void Block(string? reason = null)
    {
        if (IsBlocked)
        {
            throw new DomainException($"Процедуру «{Name}» уже заблоковано.");
        }

        BlockReason = reason;
        IsBlocked = true;
    }

    public void Unblock()
    {
        if (!IsBlocked)
        {
            throw new DomainException($"Процедуру «{Name}» не заблоковано.");
        }

        IsBlocked = false;
        BlockReason = null;
    }

    public void Validate()
    {
        if (Id == Guid.Empty)
        {
            throw new DomainException("Процедура повинна мати ідентифікатор.");
        }

        NormalizeName(_name);
        NormalizePrice(_price);
        NormalizeCostPrice(_costPrice);
    }

    public override string ToString() => $"{Name} — {Price:0.00}";
}
