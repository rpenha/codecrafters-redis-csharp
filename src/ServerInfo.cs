using System.Collections.ObjectModel;

public static class ServerInfo
{
    private static readonly Dictionary<string, string> Values = new();

    private const string Role = "role";
    private const string MasterReplId = "master_replid";
    private const string MasterReplOffset = "master_repl_offset";
    private const string MasterRole = "master";
    private const string SlaveRole = "slave";

    public static void SetMasterRole()
    {
        Values[MasterReplId] = NewMasterReplId();
        Values[MasterReplOffset] = "0";
        Values[Role] = MasterRole;
    }

    public static void SetSlaveRole()
    {
        Values[Role] = SlaveRole;
    }

    public static string GetRole() => Values[Role];

    public static bool IsMaster() => GetRole().Equals(MasterRole);
    
    public static bool IsReplica() => GetRole().Equals(SlaveRole);

    public static string GetReplId() => Values[MasterReplId];
    
    public static string GetMasterReplOffset() => Values[MasterReplOffset];

    public static IDictionary<string, string> GetValues() => new ReadOnlyDictionary<string, string>(Values);

    private static string NewMasterReplId() => $"{Guid.NewGuid():N}{Guid.NewGuid():N}"[..40];
}