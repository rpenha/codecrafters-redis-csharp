using System.Collections.ObjectModel;

public static class ServerInfo
{
    private static readonly Dictionary<string, string> Values = new();

    private const string Role = "role";
    private const string MasterReplId = "master_replid";
    private const string MasterReplOffset = "master_repl_offset";

    public static void SetMasterRole()
    {
        Values[MasterReplId] = NewMasterReplId();
        Values[MasterReplOffset] = "0";
        Values[Role] = "master";
    }

    public static void SetSlaveRole()
    {
        Values[Role] = "slave";
    }

    public static string GetRole() => Values[Role];

    public static IDictionary<string, string> GetValues() => new ReadOnlyDictionary<string, string>(Values);

    private static string NewMasterReplId() => $"{Guid.NewGuid():N}{Guid.NewGuid():N}"[..40];
}