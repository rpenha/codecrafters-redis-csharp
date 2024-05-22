using System.Collections.ObjectModel;

public static class ServerInfo
{
    private static readonly Dictionary<string, string> Values = new();
    private const string Role = "role";

    public static void SetMasterRole()
    {
        Values[Role] = "master";
    }

    public static void SetSlaveRole()
    {
        Values[Role] = "slave";
    }

    public static string GetRole() => Values[Role];
    
    public static IDictionary<string, string> GetValues() => new ReadOnlyDictionary<string, string>(Values);

    public static ReplicaOptions? GetReplicaOptions(string? replicaOf) => ReplicaOptions.Parse(replicaOf);
}