using static Constants.Commands;
using static Constants.CommandArguments;

// ReSharper disable InconsistentNaming
public static class RedisCommandNameExtensions
{
    private static bool Is(this RespBulkString input, string commandName) => 
        input.Value?.Equals(commandName, StringComparison.OrdinalIgnoreCase) ?? false;

    #region Commands

    public static bool IsEcho(this RespBulkString input) => input.Is(ECHO);
    
    public static bool IsInfo(this RespBulkString input) => input.Is(INFO);

    public static bool IsGet(this RespBulkString input) => input.Is(GET);

    public static bool IsReplConf(this RespBulkString input) => input.Is(REPLCONF);
    
    public static bool IsSet(this RespBulkString input) => input.Is(SET);

    public static bool IsPing(this RespBulkString input) => input.Is(PING);

    #endregion

    #region Command Arguments

    public static bool IsPx(this RespBulkString input) => input.Is(PX);
    
    public static bool IsReplication(this RespBulkString input) => input.Is(REPLICATION);

    #endregion
}