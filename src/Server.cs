using System.Buffers;
using System.Net;
using System.Net.Sockets;
using System.Text;
using Cocona;
using Microsoft.Extensions.Caching.Memory;

const int bufferSize = 4096;
var cts = new CancellationTokenSource();
var cancellationToken = cts.Token;

static async Task HandleAsync(Socket client, RedisContext context, CancellationToken cancellationToken)
{
    while (client.Connected)
    {
        var buffer = ArrayPool<byte>.Shared.Rent(bufferSize);

        try
        {
            var receivedBytes = await client.ReceiveAsync(buffer, SocketFlags.None);
            if (receivedBytes == 0) continue;

            var expr = Encoding.ASCII.GetString(buffer);

            Console.WriteLine($"{ServerInfo.GetRole()}: received: {expr.Replace("\r", "\\r").Replace("\n", "\\n")}");

            using var reader = new StringReader(expr);
            
            await foreach(var request in RespDecoder.DecodeAsync(reader, cancellationToken))
            {
                var result = await context.ExecuteAsync(request, client, cancellationToken);
                var response = result.Encode();
                await client.SendAsync(response);
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
}

_ = CoconaLiteApp.RunAsync((int port = 6379, string? replicaof = null) =>
{
    var options = new RedisOptions
    {
        Port = port,
        ReplicaOptions = ReplicaOptions.Parse(replicaof)
    };

    var cache = new MemoryCache(new MemoryCacheOptions());
    var ctx = new RedisContext(cache, options);

    Console.WriteLine($"Running on port {port}");

    Task.Factory.StartNew(async () =>
    {
        using var server = new TcpListener(IPAddress.Any, port);
        server.Start();
        while (!cancellationToken.IsCancellationRequested)
        {
            var client = await server.AcceptSocketAsync(cancellationToken); // wait for client
            _ = HandleAsync(client, ctx, cancellationToken);
        }
    });
}, cancellationToken: cancellationToken);

Console.Read();
cts.Cancel();
Console.WriteLine("Good bye!");
Environment.Exit(0);