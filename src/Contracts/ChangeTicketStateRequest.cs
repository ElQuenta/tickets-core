using System.ComponentModel.DataAnnotations;

namespace tickets_core.src.Contracts;

public sealed record ChangeTicketStateRequest(Guid StateId, string? Comment = null) : IValidatableObject
{
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (StateId == Guid.Empty)
            yield return new ValidationResult("El estado es obligatorio.", [nameof(StateId)]);
    }
}
