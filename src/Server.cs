using System.Buffers;
using System.Net;
using System.Net.Sockets;
using System.Text;
using Cocona;

const int bufferSize = 4096;
var cts = new CancellationTokenSource();
var cancellationToken = cts.Token;

_ = CoconaLiteApp.RunAsync((int port = 6379, string? replicaof = null) =>
{
    Console.WriteLine($"Running on port {port}");

    var replicaOptions = ServerInfo.GetReplicaOptions(replicaof);
    Console.WriteLine(replicaOptions);
    
    if (replicaOptions is null)
        ServerInfo.SetMasterRole();
    else
        ServerInfo.SetSlaveRole();

    Task.Factory.StartNew(async () =>
    {
        using var server = new TcpListener(IPAddress.Any, port);
        server.Start();
        while (!cancellationToken.IsCancellationRequested)
        {
            var socket = await server.AcceptSocketAsync(cancellationToken); // wait for client
            _ = HandleAsync(socket, cancellationToken);
            
            static async Task HandleAsync(Socket socket, CancellationToken cancellationToken)
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
        }
    });
}, cancellationToken: cancellationToken);

Console.Read();
cts.Cancel();
Console.WriteLine("Good bye!");
Environment.Exit(0);