using System.ComponentModel.DataAnnotations;

namespace tickets_core.src.Contracts;

public sealed record EditTicketRequest(string? Title = null, string? Description = null, string? Comment = null,
    string? Equipment = null)
    : IValidatableObject
{
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (Title is null && Description is null && Equipment is null)
            yield return new ValidationResult("Indica un título, una descripción o un equipo para editar.",
                [nameof(Title), nameof(Description), nameof(Equipment)]);
        if (Title is not null && string.IsNullOrWhiteSpace(Title))
            yield return new ValidationResult("El título no puede estar vacío.", [nameof(Title)]);
        if (Description is not null && string.IsNullOrWhiteSpace(Description))
            yield return new ValidationResult("La descripción no puede estar vacía.", [nameof(Description)]);
        if (Equipment is not null && string.IsNullOrWhiteSpace(Equipment))
            yield return new ValidationResult("El equipo no puede estar vacío.", [nameof(Equipment)]);
    }
}
