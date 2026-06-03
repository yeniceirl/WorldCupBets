using System.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Npgsql;
using WorldCupBets.Application.Abstractions;

namespace WorldCupBets.Infrastructure.Persistence;

public sealed class EfApplicationTransactionFactory(AppDbContext dbContext) : IApplicationTransactionFactory
{
    public async Task<IApplicationTransaction> BeginSerializableAsync(CancellationToken cancellationToken = default)
    {
        var transaction = await dbContext.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);
        return new EfApplicationTransaction(transaction);
    }

    private sealed class EfApplicationTransaction(IDbContextTransaction transaction) : IApplicationTransaction
    {
        public Task CommitAsync(CancellationToken cancellationToken = default)
        {
            return CommitCoreAsync(cancellationToken);
        }

        public ValueTask DisposeAsync()
        {
            return transaction.DisposeAsync();
        }

        private async Task CommitCoreAsync(CancellationToken cancellationToken)
        {
            try
            {
                await transaction.CommitAsync(cancellationToken);
            }
            catch (PostgresException exception) when (exception.SqlState is PostgresErrorCodes.SerializationFailure or PostgresErrorCodes.DeadlockDetected)
            {
                throw new PersistenceConflictException("The requested operation conflicted with another concurrent update.", exception);
            }
        }
    }
}
