using System.Net.Sockets;
using System.Text;
using Microsoft.Extensions.Caching.Memory;
using static Constants.Commands;

public sealed class RedisContext : IDisposable
{
    private readonly HashSet<Socket> _replicas = [];
    private readonly IMemoryCache _cache;
    private TcpClient? _master;

    private Func<Command, Task>? OnCommandExecuted { get; set; }

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

        OnCommandExecuted += PropagateCommandAsync;
    }

    private async Task PropagateCommandAsync(Command command)
    {
        if (!command.IsWrite()) return;

        foreach (var replica in _replicas)
        {
            try
            {
                await replica.SendAsync(command.Expr.Encode());
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }
    }

    private void MasterHandshake(RedisOptions options)
    {
        var (host, port) = options.ReplicaOptions!;
        ServerInfo.SetSlaveRole();
        _master = new TcpClient(host, port);
        var stream = _master.GetStream();

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

    public Task<RespValue> ExecuteAsync(RespValue expr, Socket client, CancellationToken cancellationToken = default)
    {
        var commandType = expr.GetCommandType();

        Command cmd = commandType switch
        {
            CommandType.Echo => new Echo(expr),
            CommandType.Get => new Get(expr, _cache),
            CommandType.Info => new Info(expr),
            CommandType.Ping => new Ping(expr),
            CommandType.Replconf => new ReplConf(expr),
            CommandType.Set => new Set(expr, _cache),
            CommandType.Psync => new Psync(expr),
            CommandType.NotSupported => throw new NotSupportedException(),
            _ => throw new NotSupportedException()
        };

        if (ServerInfo.IsMaster() && cmd is Psync)
        {
            RegisterReplica(client);
        }

        var result = cmd.ExecuteAsync(cancellationToken);

        OnCommandExecuted?.Invoke(cmd);

        return result;
    }

    private void RegisterReplica(Socket replica)
    {
        _replicas.Add(replica);
    }

    public void Dispose()
    {
        _master?.Dispose();
    }
}