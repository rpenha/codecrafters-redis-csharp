using static Constants.CommandArguments;
using static Constants.Commands;

public sealed class ReplConf : Command
{
    private readonly RespValue _expr;

    public ReplConf(RespValue expr) : base(expr)
    {
        ArgumentNullException.ThrowIfNull(expr);

        _expr = expr switch
        {
            RespArray and [RespBulkString type, ..] when type.Is(CommandType.Replconf) => expr,
            _ => throw new ArgumentException("Invalid GET command expression")
        };
    }

    public override Task<RespValue> ExecuteAsync(CancellationToken cancellationToken)
    {
        return _expr switch
        {
            RespArray and [RespBulkString type, RespBulkString args1, RespBulkString args2]
                when type.Is(CommandType.Replconf)
                     && args1.Value!.Equals(GETACK, StringComparison.OrdinalIgnoreCase)
                     && args2.Value!.Equals("*") => GetAcKAsync(),
            _ => throw new NotSupportedException()
        };
    }

    private Task<RespValue> GetAcKAsync()
    {
        return Task.FromResult<RespValue>(new RespArray([
            new RespBulkString(REPLCONF),
            new RespBulkString(GETACK),
            new RespBulkString("0")
        ]));
    }
}