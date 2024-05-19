using System.Diagnostics.CodeAnalysis;

[SuppressMessage("ReSharper", "InconsistentNaming")]
public sealed class RedisCommand
{
    public const string ECHO = nameof(ECHO);
    public const string PING = nameof(PING);
    public const string PONG = nameof(PONG);
    
    private static readonly RespValue PongResponse = new RespString(PONG);
    private readonly RespValue _commandValue;

    private RedisCommand(RespValue commandValue)
    {
        _commandValue = commandValue;
    }

    public static RedisCommand From(RespValue commandValue)
    {
        ArgumentNullException.ThrowIfNull(commandValue);
        return new RedisCommand(commandValue);
    }

    public Task<RespValue> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        return _commandValue switch
        {
            RespArray items when items[0] is RespBulkString value && value.IsEcho() => Task.FromResult(items[1]),
            RespArray items when items[0] is RespBulkString value && value.IsPing() => Task.FromResult(PongResponse),
            _ => throw new NotSupportedException()
        };
    }
}