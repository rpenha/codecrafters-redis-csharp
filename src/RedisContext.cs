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
        Psync(stream);
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
    
    private static void Psync(NetworkStream stream)
    {
        var psync = new RespArray([
            new RespBulkString(PSYNC),
            new RespBulkString("?"),
            new RespBulkString("-1")
        ]);
        stream.Write(psync.Encode());
        //EnsureValidReplResponse(stream);
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
        var commandType = expr.GetCommandType();
        
        Command cmd = commandType switch
        {
            CommandType.Echo => new Echo(expr),
            CommandType.Get => new Get(expr, _cache),
            CommandType.Info => new Info(expr),
            CommandType.Ping => new Ping(),
            CommandType.Replconf => new ReplConf(),
            CommandType.Set => new Set(expr, _cache),
            //CommandType.Psync => expr,
            _ => throw new NotSupportedException()
        };

        return cmd.ExecuteAsync(cancellationToken);
    }
}