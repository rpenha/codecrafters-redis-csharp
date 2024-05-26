using System.Text;
using Microsoft.Extensions.Primitives;

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

        var sb = new StringBuilder();

        while ((chr = reader.Read()) > 0)
        {
            sb.Append(Convert.ToChar(chr));
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
        if (!int.TryParse(await reader.ReadLineAsync(cancellationToken), out var offset))
        {
            throw new ArgumentException($"Invalid {nameof(RespBulkString)} input", nameof(reader));
        }

        var sb = new StringBuilder();
        var count = 0;
        
        while (count < offset)
        {
            sb.Append(Convert.ToChar(reader.Read()));
            count++;
        }

        reader.Read(); // CR
        reader.Read(); // LF

        return new RespBulkString(sb.ToString());
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