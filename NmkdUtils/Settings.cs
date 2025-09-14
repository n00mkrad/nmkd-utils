namespace NmkdUtils
{
    public class Settings
    {
        public static Func<Dictionary<string, string>>? CommandPathsProvider { get; set; }

        private static readonly Lazy<Dictionary<string, string>> _commandPathsLazy = new(() => CommandPathsProvider?.Invoke() ?? new());
        public static Dictionary<string, string> CommandPaths => _commandPathsLazy.Value;
    }
}
