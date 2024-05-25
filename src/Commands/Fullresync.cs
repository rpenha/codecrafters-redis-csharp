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