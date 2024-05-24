public sealed class Psync : Command
{
    public override Task<RespValue> ExecuteAsync(CancellationToken cancellationToken)
    {
        return Task.FromResult((RespValue)new RespRDB());
    }
}