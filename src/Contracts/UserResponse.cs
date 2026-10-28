namespace tickets_core.src.Contracts;

public sealed record UserResponse(
    Guid Id,
    string? Username,
    string? Email,
    bool EmailConfirmed,
    string? PhoneNumber,
    bool PhoneNumberConfirmed,
    bool LockoutEnabled,
    DateTimeOffset? LockoutEnd,
    int AccessFailedCount,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    IReadOnlyList<string> Roles);
