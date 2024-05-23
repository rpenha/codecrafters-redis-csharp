using System.Net.Sockets;
using System.Text;
using Microsoft.Extensions.Caching.Memory;
using static Constants.Commands;

public sealed class RedisContext
{
    private readonly IMemoryCache _cache;

    public RedisContext(IMemoryCache cache, RedisOptions options)
    {
        ArgumentNullException.ThrowIfNull(cache);
        _cache = cache;

        if (options.ReplicaOptions is not null)
        {
            MasterHandshake(options);
            return;
        }

        ServerInfo.SetMasterRole();
    }

    private static void MasterHandshake(RedisOptions options)
    {
        var (host, port) = options.ReplicaOptions!;
        ServerInfo.SetSlaveRole();
        using var master = new TcpClient(host, port);
        using var stream = master.GetStream();

        PingPong(stream);
        ReplListeningPort(options, stream);
        ReplCapabilities(stream);
    }

    private static void PingPong(NetworkStream stream)
    {
        var ping = new RespArray([new RespBulkString(PING)]);
        stream.Write(ping.Encode());

        var buffer = new byte[16];

        if (stream.Read(buffer) <= 0)
            throw new InvalidOperationException($"No {PING} response from master");

        var expr = Encoding.ASCII.GetString(buffer);
        using var reader = new StringReader(expr);
        var decoded = RespDecoder.DecodeAsync(reader).GetAwaiter().GetResult();

        if (!Ping.PongResponse.Equals(decoded))
            throw new InvalidOperationException($"Invalid {PING} response from master");
    }

    private static void ReplListeningPort(RedisOptions options, NetworkStream stream)
    {
        var replConfListeningPort = new RespArray([
            new RespBulkString(REPLCONF),
            new RespBulkString("listening-port"),
            new RespBulkString($"{options.Port}")
        ]);
        stream.Write(replConfListeningPort.Encode());
        EnsureValidReplResponse(stream);
    }

    private static void ReplCapabilities(NetworkStream stream)
    {
        var replConfCapa = new RespArray([
            new RespBulkString(REPLCONF),
            new RespBulkString("capa"),
            new RespBulkString("psync2")
        ]);
        stream.Write(replConfCapa.Encode());
        EnsureValidReplResponse(stream);
    }

    private static void EnsureValidReplResponse(NetworkStream stream)
    {
        var buffer = new byte[16];

        if (stream.Read(buffer) <= 0)
            throw new InvalidOperationException($"No {REPLCONF} response from master");

        var expr = Encoding.ASCII.GetString(buffer);
        using var reader = new StringReader(expr);
        var decoded = RespDecoder.DecodeAsync(reader).GetAwaiter().GetResult();

        if (!RespString.Ok.Equals(decoded))
            throw new InvalidOperationException($"Invalid {REPLCONF} response from master");
    }

    public Task<RespValue> ExecuteAsync(RespValue expr, CancellationToken cancellationToken = default)
    {
        RedisCommand cmd = expr switch
        {
            RespArray and [RespBulkString value, RespBulkString input] when value.IsEcho() => new Echo(input),
            RespArray items and [RespBulkString value, RespBulkString] when value.IsInfo() => new Info(items),
            RespArray and [RespBulkString value, RespBulkString input] when value.IsGet() => new Get(input, _cache),
            RespArray and [RespBulkString value] when value.IsPing() => new Ping(),
            RespArray items and [RespBulkString value, ..] when value.IsReplConf() => new ReplConf(),
            RespArray items and [RespBulkString value, ..] when value.IsSet() => new Set(items, _cache),
            _ => throw new NotSupportedException()
        };

        return cmd.ExecuteAsync(cancellationToken);
    }
}

public abstract class RedisCommand
{
    public abstract Task<RespValue> ExecuteAsync(CancellationToken cancellationToken);
}

public sealed class Echo : RedisCommand
{
    private readonly RespBulkString _value;

    public Echo(RespBulkString input)
    {
        ArgumentNullException.ThrowIfNull(input);
        _value = input;
    }

    public override Task<RespValue> ExecuteAsync(CancellationToken cancellationToken)
    {
        return Task.FromResult((RespValue)_value);
    }
}

public sealed class Get : RedisCommand
{
    private readonly RespValue _key;
    private readonly IMemoryCache _cache;

    public Get(RespValue key, IMemoryCache cache)
    {
        ArgumentNullException.ThrowIfNull(key);
        ArgumentNullException.ThrowIfNull(cache);
        _key = key;
        _cache = cache;
    }

    public override Task<RespValue> ExecuteAsync(CancellationToken cancellationToken)
    {
        return !_cache.TryGetValue(_key, out RespValue? output)
            ? Task.FromResult(RespBulkString.Null)
            : Task.FromResult(output ?? RespBulkString.Null);
    }
}

public sealed class Info : RedisCommand
{
    private readonly RespArray _items;

    public Info(RespArray input)
    {
        ArgumentNullException.ThrowIfNull(input);
        _items = input;
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

public sealed class Ping : RedisCommand
{
    // ReSharper disable once InconsistentNaming
    private const string PONG = nameof(PONG);
    public static readonly RespValue PongResponse = new RespString(PONG);

    public override Task<RespValue> ExecuteAsync(CancellationToken cancellationToken)
    {
        return Task.FromResult(PongResponse);
    }
}

public sealed class ReplConf : RedisCommand
{
    public override Task<RespValue> ExecuteAsync(CancellationToken cancellationToken)
    {
        return Task.FromResult(RespString.Ok);
    }
}

public sealed class Set : RedisCommand
{
    private readonly RespArray _input;
    private readonly IMemoryCache _cache;

    public Set(RespArray input, IMemoryCache cache)
    {
        ArgumentNullException.ThrowIfNull(input);
        ArgumentNullException.ThrowIfNull(cache);
        _input = input;
        _cache = cache;
    }

    public override Task<RespValue> ExecuteAsync(CancellationToken cancellationToken)
    {
        var (count, px) = (_input.Count, _input is [_, _, _, RespBulkString value, _] && value.IsPx());

        var output = (count, px) switch
        {
            (3, _) => SetValue(_input[1], _input[2], null),
            (5, true) => SetValue(_input[1], _input[2], ParseExpiry(_input[4])),
            _ => throw new InvalidOperationException()
        };

        return Task.FromResult(output);
    }

    private static long ParseExpiry(RespValue item) => long.Parse(((RespBulkString)item).Value!);

    private RespValue SetValue(RespValue key, RespValue value, long? expiry)
    {
        _ = expiry.HasValue
            ? _cache.Set(key, value, TimeSpan.FromMilliseconds(expiry.Value))
            : _cache.Set(key, value);

        return RespString.Ok;
    }
}