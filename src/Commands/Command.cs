public abstract class Command
{
    public abstract Task<RespValue> ExecuteAsync(CancellationToken cancellationToken);
}

public enum CommandType
{
    Unsupported,
    Echo,
    Get,
    Info,
    Ping,
    Psync,
    Replconf,
    Set
}