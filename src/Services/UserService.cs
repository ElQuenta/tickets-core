using tickets_core.src.Contracts;
using tickets_core.src.Repositories;

namespace tickets_core.src.Services;

public sealed class UserService(UserRepository repository)
{
    public Task<IReadOnlyList<UserResponse>> GetAllAsync(CancellationToken cancellationToken = default) =>
        repository.GetAllAsync(cancellationToken);

    public async Task<EditUserResult> EditAsync(Guid userId, EditUserRequest request,
        CancellationToken cancellationToken = default)
    {
        var user = await repository.FindAsync(userId, cancellationToken)
            ?? throw new KeyNotFoundException("El usuario no existe.");

        user.UserName = request.Username;
        user.Email = request.Email;
        user.PhoneNumber = string.IsNullOrWhiteSpace(request.PhoneNumber) ? null : request.PhoneNumber;
        user.EmailConfirmed = request.EmailConfirmed;
        user.PhoneNumberConfirmed = user.PhoneNumber is not null && request.PhoneNumberConfirmed;
        user.LockoutEnabled = request.LockoutEnabled;
        user.LockoutEnd = request.LockoutEnd;

        var result = await repository.UpdateAsync(user, request.Password, cancellationToken);
        if (!result.Succeeded)
            return new(null, result.Errors.GroupBy(error => error.Code).ToDictionary(
                group => group.Key, group => group.Select(error => error.Description).ToArray()));

        return new(await repository.GetResponseAsync(userId, cancellationToken));
    }
}

public sealed record EditUserResult(UserResponse? Response, Dictionary<string, string[]>? Errors = null);
