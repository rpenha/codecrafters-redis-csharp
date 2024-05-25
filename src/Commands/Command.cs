public abstract class Command
{
    protected Command(RespValue expr)
    {
        ArgumentNullException.ThrowIfNull(expr);
        Expr = expr;
    }

    public RespValue Expr { get; }

    public abstract Task<RespValue> ExecuteAsync(CancellationToken cancellationToken);

    public virtual bool IsWrite() => false;
}

public enum CommandType
{
    NotSupported,
    Echo,
    Fullresync,
    Get,
    Info,
    Ping,
    Psync,
    Replconf,
    Set
}