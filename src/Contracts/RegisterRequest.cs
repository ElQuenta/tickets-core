namespace tickets_core.src.Contracts;

public sealed record RegisterRequest(string Username, string Email, string Password);
