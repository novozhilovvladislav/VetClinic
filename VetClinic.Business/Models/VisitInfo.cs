using VetClinic.Domain.Entities;

namespace VetClinic.Business.Models;

/// <summary>Візит разом з іменами власника, тварини та лікаря — зручно для відображення у списках.</summary>
public sealed record VisitInfo(Visit Visit, string OwnerName, string PetName, string VeterinarianName);
