using System.Text;

public sealed record RespBulkString : RespValue
{
    private const char ByteType = '$';
    public static readonly RespValue Null = new RespBulkString(default(string));
    private static readonly byte[] RespNullBulkString = "$-1\r\n"u8.ToArray();

    public RespBulkString() : this(default(string))
    {
    }
    
    public RespBulkString(string? value)
    {
        Value = value;
    }

    public string? Value { get; }

    public int Length => Value?.Length ?? 0;

    public static implicit operator string?(RespBulkString input) => input.Value;

    public override ArraySegment<byte> Encode()
    {
        if (Value is null)
        {
            return RespNullBulkString;
        }

        var result = new StringBuilder()
            .Append(ByteType)
            .Append(Length)
            .Append(CRLF)
            .Append(Value)
            .Append(CRLF);

        return Encoding.ASCII.GetBytes(result.ToString());
    }
}