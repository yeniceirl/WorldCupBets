namespace WorldCupBets.Application.Abstractions;

public sealed class NoopApplicationTransactionFactory : IApplicationTransactionFactory
{
    public Task<IApplicationTransaction> BeginSerializableAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult<IApplicationTransaction>(new NoopApplicationTransaction());
    }

    private sealed class NoopApplicationTransaction : IApplicationTransaction
    {
        public Task CommitAsync(CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }

        public ValueTask DisposeAsync()
        {
            return ValueTask.CompletedTask;
        }
    }
}
