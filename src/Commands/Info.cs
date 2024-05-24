public sealed class Info : Command
{
    private readonly RespArray _items;

    public Info(RespValue expr)
    {
        ArgumentNullException.ThrowIfNull(expr);
        _items = expr switch
        {
            RespArray items and [RespBulkString type, RespBulkString] when type.Is(CommandType.Info) => items,
            _ => throw new ArgumentException("Invalid INFO command expression")
        };
    }

    public override Task<RespValue> ExecuteAsync(CancellationToken cancellationToken)
    {
        var (count, replication) = (_items.Count, _items is [_, RespBulkString value] && value.IsPx());

        return (count, replication) switch
        {
            (2, _) => Task.FromResult((RespValue)ServerInfo.GetValues().GetInfoResponse()),
            _ => throw new InvalidOperationException()
        };
    }
}