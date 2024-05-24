public sealed class ReplConf : Command
{
    public override Task<RespValue> ExecuteAsync(CancellationToken cancellationToken)
    {
        return Task.FromResult(RespString.Ok);
    }
}