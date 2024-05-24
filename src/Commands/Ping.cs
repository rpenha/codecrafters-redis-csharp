public sealed class Ping : Command
{
    // ReSharper disable once InconsistentNaming
    private const string PONG = nameof(PONG);
    public static readonly RespValue PongResponse = new RespString(PONG);

    public Ping(RespValue expr) : base(expr)
    {
    }

    public override Task<RespValue> ExecuteAsync(CancellationToken cancellationToken)
    {
        return Task.FromResult(PongResponse);
    }
}