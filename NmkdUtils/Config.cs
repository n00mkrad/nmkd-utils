
namespace NmkdUtils
{
    public static class ConfigMgr<T> where T : new()
    {
        public static string CfgFile = "";

        private static T? _config = default;

        public static T Config
        {
            get
            {
                if (_config == null)
                {
                    Read();
                }

                return _config ?? new T();
            }
        }

        static ConfigMgr()
        {
            CfgFile = Path.Combine(AppContext.BaseDirectory, "config.json");
        }

        public static void Read()
        {
            try
            {
                if (!File.Exists(CfgFile))
                {
                    Logger.LogWrn($"No config found.");
                    _config = new T();
                    return;
                }

                _config = File.ReadAllText(CfgFile).FromJson<T>();

                if (_config == null)
                {
                    throw new Exception("Cfg is null!");
                }
            }
            catch (Exception ex)
            {
                Logger.Log(ex, "Failed to read config");
            }
        }

        public static void Write()
        {
            if (_config == null)
                return;

            try
            {
                File.WriteAllText(CfgFile, _config.ToJson(true));
            }
            catch (Exception ex)
            {
                Logger.Log(ex, "Failed to write config");
            }
        }
    }
}
