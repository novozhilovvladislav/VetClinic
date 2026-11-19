using VetClinic.Domain.Enums;

namespace VetClinic.Business.Models;

/// <summary>
/// Обмеження з паспорта тварини, що стосується обраної процедури
/// (у них збігається принаймні один тег).
/// </summary>
public sealed record RestrictionConflict(string ProcedureName, RestrictionType Type, string Title, string Tag);
