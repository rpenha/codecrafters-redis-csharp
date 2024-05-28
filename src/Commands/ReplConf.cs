using System.Net.Sockets;
using static Constants.CommandArguments;
using static Constants.Commands;

public sealed class ReplConf : Command
{
    private readonly Socket _client;
    private readonly RespValue _expr;

    public ReplConf(RespValue expr, Socket client) : base(expr)
    {
        ArgumentNullException.ThrowIfNull(expr);
        ArgumentNullException.ThrowIfNull(client);

        _expr = expr;
        _client = client;

        // _expr = expr switch
        // {
        //     RespArray and [RespBulkString type, ..] when type.Is(CommandType.Replconf) => expr,
        // };
    }

    public override Task<RespValue> ExecuteAsync(CancellationToken cancellationToken)
    {
        return _expr switch
        {
            RespArray and [RespBulkString type, RespBulkString args1, RespBulkString args2]
                when type.Is(CommandType.Replconf)
                     && args1.Value!.Equals(GETACK, StringComparison.OrdinalIgnoreCase)
                     && args2.Value!.Equals("*") => GetAcKAsync(cancellationToken),
            _ => Task.FromResult(RespString.Ok)
        };
    }

    private async Task<RespValue> GetAcKAsync(CancellationToken cancellationToken)
    {
        var result = new RespArray([
            new RespBulkString(REPLCONF),
            new RespBulkString("ACK"),
            new RespBulkString(ServerInfo.GetOffset())
        ]);

        await _client.SendAsync(result.Encode(), cancellationToken);
        
        return result;
    }
}