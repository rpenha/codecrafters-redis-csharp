using Microsoft.Extensions.Caching.Memory;

public sealed class Get : Command
{
    private readonly RespValue _key;
    private readonly IMemoryCache _cache;

    public Get(RespValue expr, IMemoryCache cache) : base(expr)
    {
        ArgumentNullException.ThrowIfNull(expr);
        ArgumentNullException.ThrowIfNull(cache);
        
        _key = expr switch
        {
            RespArray and [RespBulkString type, RespBulkString key] when type.Is(CommandType.Get) => key,
            _ => throw new ArgumentException("Invalid GET command expression")
        };

        _cache = cache;
    }

    public override Task<RespValue> ExecuteAsync(CancellationToken cancellationToken)
    {
        return !_cache.TryGetValue(_key, out RespValue? output)
            ? Task.FromResult(RespBulkString.Null)
            : Task.FromResult(output ?? RespBulkString.Null);
    }
}