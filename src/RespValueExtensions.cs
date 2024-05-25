using static Constants.CommandArguments;

// ReSharper disable InconsistentNaming
public static class RespValueExtensions
{
    private static bool Is(this RespBulkString input, string commandName) =>
        input.Value?.Equals(commandName, StringComparison.OrdinalIgnoreCase) ?? false;

    #region Commands

    public static CommandType GetCommandType(this RespValue self)
    {
        return self switch
        {
            RespString { Value: not null } input
                when input.Value
                    .Split(' ', StringSplitOptions.RemoveEmptyEntries)[0]
                    .Equals(Constants.Commands.FULLRESYNC, StringComparison.OrdinalIgnoreCase) => CommandType.Fullresync,
            RespBulkString type => Enum.Parse<CommandType>(type!, true),
            RespArray { Count: > 0 } items when items[0] is RespBulkString type => Enum.Parse<CommandType>(type!, true),
            _ => CommandType.NotSupported
        };
    }

    public static bool Is(this RespValue self, CommandType type) => self.GetCommandType().Equals(type);

    #endregion

    #region Command Arguments

    public static bool IsPx(this RespBulkString input) => input.Is(PX);

    public static bool IsReplication(this RespBulkString input) => input.Is(REPLICATION);

    #endregion
}