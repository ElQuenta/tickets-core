namespace tickets_core.src.Contracts;

public sealed record RoleResponse(Guid Id, string Name, DateTime CreatedAt, DateTime UpdatedAt);
