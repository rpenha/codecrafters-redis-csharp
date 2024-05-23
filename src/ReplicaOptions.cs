public record RedisOptions
{
    public required int Port { get; init; } = 6379;
    
    public ReplicaOptions? ReplicaOptions { get; init; }
}

public record ReplicaOptions
{
    private ReplicaOptions(string masterHost, int masterPort)
    {
        MasterHost = masterHost;
        MasterPort = masterPort;
    }

    public static ReplicaOptions? Parse(string? input)
    {
        if (string.IsNullOrEmpty(input))
            return null;

        try
        {
            var options = input.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            var host = options[0];
            var port = int.Parse(options[1]);
            return new ReplicaOptions(host, port);
        }
        catch
        {
            throw new ArgumentException($"Invalid replica options: {input}", nameof(input));
        }
    }

    public string MasterHost { get; }

    public int MasterPort { get; }

    public void Deconstruct(out string masterHost, out int masterPort)
    {
        masterHost = MasterHost;
        masterPort = MasterPort;
    }
}