using System.Buffers;
using System.Net;
using System.Net.Sockets;

// You can use print statements as follows for debugging, they'll be visible when running tests.
Console.WriteLine("Logs from your program will appear here!");

// Uncomment this block to pass the first stage
using var server = new TcpListener(IPAddress.Any, 6379);
server.Start();

const int bufferSize = 1024;

while (true)
{
    var socket = await server.AcceptSocketAsync(); // wait for client
    Handle(socket);
}

static async Task Handle(Socket socket)
{
    while (socket.Connected)
    {
        var pong = "+PONG\r\n"u8.ToArray();
        var buffer = ArrayPool<byte>.Shared.Rent(bufferSize);
        await socket.ReceiveAsync(buffer, SocketFlags.None);
        await socket.SendAsync(pong);
        ArrayPool<byte>.Shared.Return(buffer);
    }
}