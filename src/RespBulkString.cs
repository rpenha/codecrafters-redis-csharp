using System.Text;

public sealed record RespBulkString : RespValue
{
    private const char ByteType = '$';
    
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
            return RespNull.Instance.Encode();
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