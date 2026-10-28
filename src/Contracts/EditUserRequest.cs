using System.ComponentModel.DataAnnotations;

namespace tickets_core.src.Contracts;

public sealed record EditUserRequest(
    [Required, StringLength(256)] string Username,
    [Required, EmailAddress, StringLength(256)] string Email,
    [StringLength(256)] string? PhoneNumber = null,
    bool EmailConfirmed = false,
    bool PhoneNumberConfirmed = false,
    bool LockoutEnabled = true,
    DateTimeOffset? LockoutEnd = null,
    string? Password = null);
