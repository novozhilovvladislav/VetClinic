using VetClinic.Domain.Entities;

namespace VetClinic.Business.Models;

/// <summary>Паспорт разом з даними тварини та її власника — усе, що потрібно для відображення.</summary>
public sealed record PetPassportInfo(Pet Pet, Owner Owner, PetPassport Passport);
