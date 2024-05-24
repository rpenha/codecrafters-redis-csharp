public sealed class Echo : Command
{
    private readonly RespBulkString _value;

    public Echo(RespValue expr) : base(expr)
    {
        ArgumentNullException.ThrowIfNull(expr);
        _value = expr switch
        {
            RespArray and [RespBulkString type, RespBulkString value] when type.Is(CommandType.Echo) => value,
            _ => throw new ArgumentException("Invalid ECHO command expression")
        };
    }

    public override Task<RespValue> ExecuteAsync(CancellationToken cancellationToken)
    {
        return Task.FromResult((RespValue)_value);
    }
}