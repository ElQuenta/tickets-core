using System.ComponentModel.DataAnnotations;

namespace tickets_core.src.Contracts;

public sealed record CreateUserRoleRequest(Guid RoleId) : IValidatableObject
{
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (RoleId == Guid.Empty)
            yield return new ValidationResult("El rol es obligatorio.", [nameof(RoleId)]);
    }
}
