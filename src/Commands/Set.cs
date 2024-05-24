using Microsoft.Extensions.Caching.Memory;

public sealed class Set : Command
{
    private readonly RespArray _items;
    private readonly IMemoryCache _cache;

    public Set(RespValue expr, IMemoryCache cache) : base(expr)
    {
        ArgumentNullException.ThrowIfNull(cache);
        
        _items = expr switch
        {
            RespArray items and [RespBulkString type, ..] when type.Is(CommandType.Set) => items,
            _ => throw new ArgumentException("Invalid INFO command expression")
        };
        
        _cache = cache;
    }

    public override Task<RespValue> ExecuteAsync(CancellationToken cancellationToken)
    {
        var (count, px) = (_items.Count, _items is [_, _, _, RespBulkString value, _] && value.IsPx());

        var output = (count, px) switch
        {
            (3, _) => SetValue(_items[1], _items[2], null),
            (5, true) => SetValue(_items[1], _items[2], ParseExpiry(_items[4])),
            _ => throw new InvalidOperationException()
        };

        return Task.FromResult(output);
    }

    private static long ParseExpiry(RespValue item) => long.Parse(((RespBulkString)item).Value!);

    private RespValue SetValue(RespValue key, RespValue value, long? expiry)
    {
        _ = expiry.HasValue
            ? _cache.Set(key, value, TimeSpan.FromMilliseconds(expiry.Value))
            : _cache.Set(key, value);

        return RespString.Ok;
    }

    public override bool IsWrite() => true;
}