using System.Buffers;

public abstract record RespValue
{
    protected const string CRLF = "\r\n";

    public abstract ArraySegment<byte> Encode();
}