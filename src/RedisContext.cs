using System.Buffers;
using System.Net.Sockets;
using System.Text;
using Microsoft.Extensions.Caching.Memory;
using static Constants.CommandArguments;
using static Constants.Commands;
using Timer = System.Timers.Timer;

public sealed class RedisContext : IDisposable
{
    private readonly HashSet<Socket> _replicas = [];
    private readonly IMemoryCache _cache;
    private TcpClient? _master;

    private Func<Command, Task>? OnCommandExecuted { get; }

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

         var timer = new Timer(30000)
         {
             AutoReset = false
         };
        
         timer.Elapsed += async (_, _) =>
         {
             var expr = new RespArray([
                 new RespBulkString(REPLCONF),
                 new RespBulkString(GETACK),
                 new RespBulkString("*")
             ]);
             foreach (var replica in _replicas)
             {
                 try
                 {
                     var encoded = expr.Encode();
                     await replica.SendAsync(encoded);
                     Console.WriteLine($"{ServerInfo.GetRole()}: sent: {Encoding.ASCII.GetString(encoded).Replace("\r", "\\r").Replace("\n", "\\n")}");
                 }
                 catch (Exception ex)
                 {
                     Console.WriteLine(ex);
                 }
                 finally
                 {
                     timer.Start();
                 }
             }
         };
        
        //timer.Start();
    }

    private async Task PropagateCommandAsync(Command command)
    {
        if (!command.IsWrite()) return;

        foreach (var replica in _replicas)
        {
            try
            {
                var encoded = command.Expr.Encode();
                await replica.SendAsync(encoded);
                Console.WriteLine($"{ServerInfo.GetRole()}: sent: {Encoding.ASCII.GetString(encoded).Replace("\r", "\\r").Replace("\n", "\\n")}");
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
        HandleReplicatedCommands();
    }

    private void HandleReplicatedCommands()
    {
        if (_master is null) return;
        
        _ = Task.Factory.StartNew(async () =>
        {
            const int capacity = 4096;
            var client = _master.Client;

            while (client.Connected)
            {
                var buffer = ArrayPool<byte>.Shared.Rent(capacity);
                
                try
                {
                    var receivedBytes = await client.ReceiveAsync(buffer, SocketFlags.None);
                    if (receivedBytes == 0) continue;
                    
                    var received = Encoding.ASCII.GetString(buffer, 0, receivedBytes);
                    Console.WriteLine($"{ServerInfo.GetRole()}: received: {received.Replace("\r", "\\r").Replace("\n", "\\n")}");
                    using var reader = new StringReader(received);
                    
                    await foreach (var request in RespDecoder.DecodeAsync(reader))
                    {
                        // TODO: Receiving Ok... Need to investigate
                        if (request.Equals(RespString.Ok)) continue;
                        Console.WriteLine($"Request: {request}");
                        var response = await ExecuteAsync(request, client);

                        if (response is RespArray array 
                            && array[0] is RespBulkString first 
                            && first.Value!.Equals(REPLCONF, StringComparison.OrdinalIgnoreCase))
                        {
                            await client.SendAsync(response.Encode(), CancellationToken.None);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex);
                }
                finally
                {
                    ArrayPool<byte>.Shared.Return(buffer, true);
                }
            }
        });
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

        var decoded = RespDecoder.DecodeAsync(reader)
            .ToBlockingEnumerable()
            .First();

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
        var encoded = replConfListeningPort.Encode();
        stream.Write(encoded);
        Console.WriteLine($"{ServerInfo.GetRole()}: sent: {Encoding.ASCII.GetString(encoded).Replace("\r", "\\r").Replace("\n", "\\n")}");
        EnsureValidReplResponse(stream);
    }

    private static void ReplCapabilities(NetworkStream stream)
    {
        var replConfCapa = new RespArray([
            new RespBulkString(REPLCONF),
            new RespBulkString("capa"),
            new RespBulkString("psync2")
        ]);
        var encoded = replConfCapa.Encode();
        stream.Write(encoded);
        Console.WriteLine($"{ServerInfo.GetRole()}: sent: {Encoding.ASCII.GetString(encoded).Replace("\r", "\\r").Replace("\n", "\\n")}");
        EnsureValidReplResponse(stream);
    }

    private static void Psync(NetworkStream stream)
    {
        var psync = new RespArray([
            new RespBulkString(PSYNC),
            new RespBulkString("?"),
            new RespBulkString("-1")
        ]);
        var encoded = psync.Encode();
        stream.Write(encoded);
        Console.WriteLine($"{ServerInfo.GetRole()}: sent: {Encoding.ASCII.GetString(encoded).Replace("\r", "\\r").Replace("\n", "\\n")}");
        EnsureValidPsyncResponse(stream);
    }

    private static void EnsureValidPsyncResponse(NetworkStream stream)
    {
        var buffer = new byte[8192];

        if (stream.Read(buffer) <= 0)
            throw new InvalidOperationException($"No {PSYNC} response from master");

        var expr = Encoding.ASCII.GetString(buffer);
        Console.WriteLine($"{ServerInfo.GetRole()}: received: {expr.Replace("\r", "\\r").Replace("\n", "\\n")}");
        using var reader = new StringReader(expr);
        
        var decoded = RespDecoder.DecodeAsync(reader)
            .ToBlockingEnumerable()
            .First();

        if (decoded is not RespString && !((RespString)decoded).Value!.Contains(FULLRESYNC))
            throw new InvalidOperationException($"Invalid {PSYNC} response from master");
    }

    private static void EnsureValidReplResponse(NetworkStream stream)
    {
        var buffer = new byte[16];

        if (stream.Read(buffer) <= 0)
            throw new InvalidOperationException($"No {REPLCONF} response from master");

        var expr = Encoding.ASCII.GetString(buffer);
        Console.WriteLine($"{ServerInfo.GetRole()}: received: {expr.Replace("\r", "\\r").Replace("\n", "\\n")}");
        using var reader = new StringReader(expr);
        
        var decoded = RespDecoder.DecodeAsync(reader)
            .ToBlockingEnumerable()
            .First();

        if (!RespString.Ok.Equals(decoded))
            throw new InvalidOperationException($"Invalid {REPLCONF} response from master");
    }

    public async Task<RespValue?> ExecuteAsync(RespValue expr, Socket client, CancellationToken cancellationToken = default)
    {
        var commandType = expr.GetCommandType();
        
        Console.WriteLine($"Command Type: {expr} {commandType}");

        Command cmd = commandType switch
        {
            CommandType.Echo => new Echo(expr),
            CommandType.Get => new Get(expr, _cache),
            CommandType.Fullresync => new Fullresync(expr),
            CommandType.Info => new Info(expr),
            CommandType.Ping => new Ping(expr),
            CommandType.RdbFile => new Nop(expr),
            CommandType.Replconf => new ReplConf(expr, client),
            CommandType.Set => new Set(expr, _cache),
            CommandType.Psync => new Psync(expr),
            CommandType.NotSupported => throw new NotSupportedException(),
            _ => throw new NotSupportedException()
        };

        if (ServerInfo.IsMaster() && cmd is Psync)
        {
            RegisterReplica(client);
        }
        
        var result = await cmd.ExecuteAsync(cancellationToken);

        OnCommandExecuted?.Invoke(cmd);

        return cmd is Fullresync ? null : result;
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