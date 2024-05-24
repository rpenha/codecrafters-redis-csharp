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