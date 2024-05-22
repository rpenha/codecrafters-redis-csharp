using System.Text;

public static class ServerInfoExtensions
{
    private const string CRLF = "\r\n";

    public static RespBulkString GetInfoResponse(this IDictionary<string, string> self)
    {
        var sb = new StringBuilder();
        var count = self.Count;
        var index = 0;

        foreach (var (key, value) in self)
        {
            sb.Append($"{key}:{value}");
            index++;
            
            if (index == count)
                break;
            
            sb.Append(CRLF);
        }

        return new RespBulkString(sb.ToString());
    }
}