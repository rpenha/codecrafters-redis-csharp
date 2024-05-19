using System.Text;

public sealed record RespString : RespValue
{
    private const char ByteType = '+';

    public RespString(string? value)
    {
        Value = value;
    }

    public string? Value { get; }

    public int Length => Value?.Length ?? 0;

    public static implicit operator string?(RespString input) => input.Value;

    public override ArraySegment<byte> Encode()
    {
        if (Value is null)
        {
            return RespNull.Instance.Encode();
        }
        
        var sb = new StringBuilder()
            .Append(ByteType)
            .Append(Value)
            .Append(CRLF);

        return Encoding.ASCII.GetBytes(sb.ToString());
    }
}