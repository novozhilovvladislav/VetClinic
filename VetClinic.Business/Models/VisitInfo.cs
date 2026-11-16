using VetClinic.Domain.Entities;

namespace VetClinic.Business.Models;

/// <summary>Візит разом з іменами власника та тварини — зручно для відображення у списках.</summary>
public sealed record VisitInfo(Visit Visit, string OwnerName, string PetName);
