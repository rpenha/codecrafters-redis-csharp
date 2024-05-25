public sealed class Psync : Command
{
    public Psync(RespValue expr) : base(expr)
    {
        
    }
    public override Task<RespValue> ExecuteAsync(CancellationToken cancellationToken)
    {
        return Task.FromResult((RespValue)new RespRDB());
    }
}

public sealed class Fullresync : Command
{
    public Fullresync(RespValue expr) : base(expr)
    {
        
    }
    public override Task<RespValue> ExecuteAsync(CancellationToken cancellationToken)
    {
        return Task.FromResult(RespBulkString.Null);
    }
}