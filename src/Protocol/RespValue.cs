using System.Buffers;

public abstract record RespValue
{
    protected const string CRLF = "\r\n";
    protected const byte CR = 0x0D;
    protected const byte LF = 0x0A;

    public abstract ArraySegment<byte> Encode();
}