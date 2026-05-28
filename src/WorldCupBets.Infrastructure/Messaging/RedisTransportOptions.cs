namespace WorldCupBets.Infrastructure.Messaging;

public sealed class RedisTransportOptions
{
    public string ConnectionString { get; init; } = string.Empty;
}
