public static class Constants
{
    public static class Commands
    {
        #region Commands

        public const string ECHO = nameof(ECHO);
        public const string GET = nameof(GET);
        public const string INFO = nameof(INFO);
        public const string PING = nameof(PING);
        public const string SET = nameof(SET);

        #endregion
    }

    public static class CommandArguments
    {
        #region Command Arguments

        public const string PX = nameof(PX);
        public const string REPLICATION = nameof(REPLICATION);
        
        #endregion
    }
}