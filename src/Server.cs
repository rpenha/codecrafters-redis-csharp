using System.Buffers;
using System.Net;
using System.Net.Sockets;
using System.Text;

// You can use print statements as follows for debugging, they'll be visible when running tests.
Console.WriteLine("Logs from your program will appear here!");

const int bufferSize = 4096;
var cancellationToken = CancellationToken.None;

using var server = new TcpListener(IPAddress.Any, 6379);
server.Start();

while (true)
{
    var socket = await server.AcceptSocketAsync(cancellationToken); // wait for client
    HandleAsync(socket);
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