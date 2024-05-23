using System.Text;

public sealed record RespArray : RespValue
{
    private const char ByteType = '*';

    private readonly IReadOnlyList<RespValue>? _items;

    public RespArray(IReadOnlyList<RespValue>? items)
    {
        _items = items;
    }

    public RespValue this[int index] =>
        _items?[index] ?? throw new NullReferenceException($"{nameof(RespArray)} is null");

    public int Count => _items?.Count ?? 0;

    public override ArraySegment<byte> Encode()
    {
        var data = Encoding.ASCII.GetBytes($"{ByteType}{Count}{CRLF}");

        foreach (var item in _items ?? Array.Empty<RespValue>())
        {
            var encoded = item.Encode().AsEnumerable();
            data = [..data, ..encoded];
        }

        return data;
    }
}