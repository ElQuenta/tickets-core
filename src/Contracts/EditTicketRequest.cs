using System.ComponentModel.DataAnnotations;

namespace tickets_core.src.Contracts;

public sealed record EditTicketRequest(string? Title = null, string? Description = null, string? Comment = null)
    : IValidatableObject
{
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (Title is null && Description is null)
            yield return new ValidationResult("Indica un título o una descripción para editar.",
                [nameof(Title), nameof(Description)]);
        if (Title is not null && string.IsNullOrWhiteSpace(Title))
            yield return new ValidationResult("El título no puede estar vacío.", [nameof(Title)]);
        if (Description is not null && string.IsNullOrWhiteSpace(Description))
            yield return new ValidationResult("La descripción no puede estar vacía.", [nameof(Description)]);
    }
}
