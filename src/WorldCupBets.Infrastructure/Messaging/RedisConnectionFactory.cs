namespace WorldCupBets.Infrastructure.Messaging;

public sealed class RedisConnectionFactory(RedisTransportOptions options)
{
    public string GetConnectionString()
    {
        return options.ConnectionString;
    }
}
