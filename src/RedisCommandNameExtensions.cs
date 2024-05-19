public static class RedisCommandNameExtensions
{
    private static bool Is(this RespBulkString input, string commandName) => 
        input.Value?.Equals(commandName, StringComparison.OrdinalIgnoreCase) ?? false;

    public static bool IsEcho(this RespBulkString input) => input.Is(RedisCommand.ECHO);
    
    public static bool IsPing(this RespBulkString input) => input.Is(RedisCommand.PING);
    
    public static bool IsSet(this RespBulkString input) => input.Is(RedisCommand.SET);
    
    public static bool IsGet(this RespBulkString input) => input.Is(RedisCommand.GET);
}