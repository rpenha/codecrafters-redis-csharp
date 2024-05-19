using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;

[SuppressMessage("ReSharper", "InconsistentNaming")]
public sealed class RedisCommand
{
    private static readonly ConcurrentDictionary<RespValue, RespValue> Dictionary = new();
    
    public const string ECHO = nameof(ECHO);
    public const string PING = nameof(PING);
    public const string PONG = nameof(PONG);
    public const string SET = nameof(SET);
    public const string GET = nameof(GET);
    
    private static readonly RespValue PongResponse = new RespString(PONG);
    private static readonly RespValue Ok = new RespString("OK");
    
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
            RespArray items when items[0] is RespBulkString value && value.IsSet() => SetAsync(items[1], items[2], cancellationToken),
            RespArray items when items[0] is RespBulkString value && value.IsGet() => GetAsync(items[1], cancellationToken),
            _ => throw new NotSupportedException()
        };
    }

    private static Task<RespValue> SetAsync(RespValue key, RespValue value, CancellationToken cancellationToken)
    {
        Dictionary[key] = value;
        return Task.FromResult(Ok);
    }

    private static Task<RespValue> GetAsync(RespValue key, CancellationToken cancellationToken)
    {
        return Task.FromResult(Dictionary.TryGetValue(key, out var output) ? output : RespBulkString.Null);
    }
}