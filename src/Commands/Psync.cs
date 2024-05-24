public sealed class Psync : Command
{
    public override Task<RespValue> ExecuteAsync(CancellationToken cancellationToken)
    {
        var value = $"FULLRESYNC {ServerInfo.GetReplId()} {ServerInfo.GetMasterReplOffset()}";
        return Task.FromResult((RespValue)new RespString(value));
    }
}