namespace WorldCupBets.Application.Abstractions;

public sealed class PersistenceConflictException(string message, Exception? innerException = null) : Exception(message, innerException);
