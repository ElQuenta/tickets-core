using System.ComponentModel.DataAnnotations;

namespace tickets_core.src.Contracts;

public sealed record CreateTicketRequest(
    [Required(ErrorMessage = "El título es obligatorio.")] string Title,
    [Required(ErrorMessage = "La descripción es obligatoria.")] string Description);
