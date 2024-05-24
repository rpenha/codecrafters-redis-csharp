public sealed class ReplConf : Command
{
    public ReplConf(RespValue expr) : base(expr)
    {
    }

    public override Task<RespValue> ExecuteAsync(CancellationToken cancellationToken)
    {
        return Task.FromResult(RespString.Ok);
    }
}