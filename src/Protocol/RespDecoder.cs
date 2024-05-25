using System.Text;

public static class RespDecoder
{
    private const char RespArray = '*';
    private const char RespString = '+';
    private const char RespBulkString = '$';
    private const byte CR = 0x0D;
    private const byte LF = 0x0A;


    public static async IAsyncEnumerable<RespValue> DecodeAsync(StringReader reader,
        CancellationToken cancellationToken = default)
    {
        int chr;

        while ((chr = reader.Read()) > 0)
        {
            yield return chr switch
            {
                RespArray => await DecodeArray(reader, cancellationToken),
                RespString => await DecodeString(reader, cancellationToken),
                RespBulkString => await DecodeBulkString(reader, cancellationToken),
                _ => throw new NotSupportedException()
            };
        }
    }
    
    private static async Task<RespString> DecodeString(StringReader reader, CancellationToken cancellationToken)
    {
        var value = await reader.ReadLineAsync(cancellationToken);
        return new RespString(value);
    }

    private static async Task<RespBulkString> DecodeBulkString(StringReader reader, CancellationToken cancellationToken)
    {
        if (!int.TryParse(await reader.ReadLineAsync(cancellationToken), out _))
        {
            throw new ArgumentException($"Invalid {nameof(RespBulkString)} input", nameof(reader));
        }

        var value = await reader.ReadLineAsync(cancellationToken);

        return new RespBulkString(value);
    }

    private static async Task<RespArray> DecodeArray(StringReader reader, CancellationToken cancellationToken = default)
    {
        if (!int.TryParse(await reader.ReadLineAsync(cancellationToken), out var length))
        {
            throw new ArgumentException($"Invalid {nameof(RespArray)} input", nameof(reader));
        }

        var list = new List<RespValue>();

        while (list.Count < length)
        {
            var decoded = DecodeAsync(reader, cancellationToken)
                .ToBlockingEnumerable(cancellationToken)
                .First();
            
            list.Add(decoded);
        }

        return new RespArray(list.AsReadOnly());
    }
}