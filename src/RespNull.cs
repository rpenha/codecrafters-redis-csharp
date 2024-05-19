public sealed record RespNull : RespValue
{
    private static readonly byte[] Value = @"_\r\n"u8.ToArray();
    
    public static readonly RespNull Instance = new(); 
    
    private RespNull()
    {
    }

    public override ArraySegment<byte> Encode() => Value;
}