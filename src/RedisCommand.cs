using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Caching.Memory;

[SuppressMessage("ReSharper", "InconsistentNaming")]
public sealed class RedisCommand
{
    private static readonly IMemoryCache Cache = new MemoryCache(new MemoryCacheOptions());

    #region Commands

    public const string ECHO = nameof(ECHO);
    public const string GET = nameof(GET);
    public const string INFO = nameof(INFO);
    public const string PING = nameof(PING);
    public const string PONG = nameof(PONG);
    public const string SET = nameof(SET);

    #endregion

    #region Command Arguments

    public const string PX = nameof(PX);
    public const string REPLICATION = nameof(REPLICATION);

    #endregion

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
            RespArray items when items[0] is RespBulkString value && value.IsInfo() => ProcessInfoCommandAsync(items, cancellationToken),
            RespArray items when items[0] is RespBulkString value && value.IsGet() => GetAsync(items[1], cancellationToken),
            RespArray items when items[0] is RespBulkString value && value.IsPing() => Task.FromResult(PongResponse),
            RespArray items when items[0] is RespBulkString value && value.IsSet() => ProcessSetCommandAsync(items, cancellationToken),
            _ => throw new NotSupportedException()
        };
    }

    private static Task<RespValue> ProcessInfoCommandAsync(RespArray items, CancellationToken cancellationToken)
    {
        var (count, replication) = (items.Count, items is [_, RespBulkString value] && value.IsPx());

        return (count, replication) switch
        {
            (2, _) => Task.FromResult((RespValue)new RespBulkString($"role:{ServerInfo.GetRole()}")),
            _ => throw new InvalidOperationException()
        };
    }

    private static Task<RespValue> ProcessSetCommandAsync(RespArray items, CancellationToken cancellationToken)
    {
        var (count, px) = (items.Count, items is [_, _, _, RespBulkString value, _] && value.IsPx());

        return (count, px) switch
        {
            (3, _) => SetAsync(items[1], items[2], null, cancellationToken),
            (5, true) => SetAsync(items[1], items[2], ParseExpiry(items[4]), cancellationToken),
            _ => throw new InvalidOperationException()
        };

        long ParseExpiry(RespValue item) => long.Parse(((RespBulkString)item).Value!);
    }

    private static Task<RespValue> SetAsync(
        RespValue key,
        RespValue value, long? expiry,
        CancellationToken cancellationToken
    )
    {
        _ = expiry.HasValue
            ? Cache.Set(key, value, TimeSpan.FromMilliseconds(expiry.Value))
            : Cache.Set(key, value);

        return Task.FromResult(Ok);
    }

    private static Task<RespValue> GetAsync(RespValue key, CancellationToken cancellationToken) =>
        !Cache.TryGetValue(key, out RespValue? output)
            ? Task.FromResult(RespBulkString.Null)
            : Task.FromResult(output ?? RespBulkString.Null);
}