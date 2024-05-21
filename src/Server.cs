using System.Buffers;
using System.Net;
using System.Net.Sockets;
using System.Text;
using Cocona;

await CoconaLiteApp.RunAsync(async (int port = 6379) =>
{
    Console.WriteLine($"Running on port {port}");

    const int bufferSize = 4096;
    var cancellationToken = CancellationToken.None;

    using var server = new TcpListener(IPAddress.Any, port);
    server.Start();

    while (true)
    {
        var socket = await server.AcceptSocketAsync(cancellationToken); // wait for client
        _ = HandleAsync(socket);
    }
    
    static async Task HandleAsync(Socket socket, CancellationToken cancellationToken = default)
    {
        try
        {
            while (socket.Connected)
            {
                var buffer = ArrayPool<byte>.Shared.Rent(bufferSize);
                var receivedBytes = await socket.ReceiveAsync(buffer, SocketFlags.None);
                Console.WriteLine($"{receivedBytes} bytes received");
        
                using var reader = new StringReader(Encoding.ASCII.GetString(buffer));
                var request = await RespDecoder.DecodeAsync(reader, cancellationToken);
                var cmd = RedisCommand.From(request);
                var result = await cmd.ExecuteAsync(cancellationToken);
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
});