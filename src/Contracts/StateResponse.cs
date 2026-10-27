namespace tickets_core.src.Contracts;

public sealed record StateResponse(Guid Id, string Name, DateTime CreatedAt, DateTime UpdatedAt);
