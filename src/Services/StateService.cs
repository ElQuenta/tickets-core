using tickets_core.src.Contracts;
using tickets_core.src.Repositories;

namespace tickets_core.src.Services;

public sealed class StateService(StateRepository repository)
{
    private readonly StateRepository _repository = repository;
    public async Task<IReadOnlyList<StateResponse>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var states = await _repository.GetAllAsync(cancellationToken);
        return states.Select(state => new StateResponse(state.Id, state.Name,
            state.CreatedAt, state.UpdatedAt)).ToList();
    }
}
