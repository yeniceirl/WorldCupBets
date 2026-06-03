namespace WorldCupBets.Application.Abstractions;

public interface IApplicationTransactionFactory
{
    Task<IApplicationTransaction> BeginSerializableAsync(CancellationToken cancellationToken = default);
}
