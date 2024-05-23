using System.Buffers;
using System.Net;
using System.Net.Sockets;
using System.Text;
using Cocona;
using Microsoft.Extensions.Caching.Memory;

const int bufferSize = 4096;
var cts = new CancellationTokenSource();
var cancellationToken = cts.Token;

static async Task HandleAsync(Socket socket, RedisContext context, CancellationToken cancellationToken)
{
    try
    {
        while (socket.Connected)
        {
            var buffer = ArrayPool<byte>.Shared.Rent(bufferSize);
            var receivedBytes = await socket.ReceiveAsync(buffer, SocketFlags.None);
            if (receivedBytes == 0) continue;
            
            var expr = Encoding.ASCII.GetString(buffer);
            
            Console.WriteLine($"{receivedBytes} bytes received");
            Console.WriteLine("---");
            Console.Write(expr);
            Console.WriteLine("---");
            
            using var reader = new StringReader(expr);
            var request = await RespDecoder.DecodeAsync(reader, cancellationToken);
            var result = await context.ExecuteAsync(request, cancellationToken);
            var response = result.Encode();

            await socket.SendAsync(response);
            ArrayPool<byte>.Shared.Return(buffer);
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine(ex);
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
            var socket = await server.AcceptSocketAsync(cancellationToken); // wait for client
            _ = HandleAsync(socket, ctx, cancellationToken);
        }
    });
}, cancellationToken: cancellationToken);

Console.Read();
cts.Cancel();
Console.WriteLine("Good bye!");
Environment.Exit(0);