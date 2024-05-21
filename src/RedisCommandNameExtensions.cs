public static class RedisCommandNameExtensions
{
    private static bool Is(this RespBulkString input, string commandName) => 
        input.Value?.Equals(commandName, StringComparison.OrdinalIgnoreCase) ?? false;

    #region Commands

    public static bool IsEcho(this RespBulkString input) => input.Is(RedisCommand.ECHO);
    
    public static bool IsInfo(this RespBulkString input) => input.Is(RedisCommand.INFO);

    public static bool IsGet(this RespBulkString input) => input.Is(RedisCommand.GET);

    public static bool IsSet(this RespBulkString input) => input.Is(RedisCommand.SET);

    public static bool IsPing(this RespBulkString input) => input.Is(RedisCommand.PING);

    #endregion

    #region Command Arguments

    public static bool IsPx(this RespBulkString input) => input.Is(RedisCommand.PX);
    
    public static bool IsReplication(this RespBulkString input) => input.Is(RedisCommand.REPLICATION);

    #endregion
}