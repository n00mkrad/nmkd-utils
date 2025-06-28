﻿using static NmkdUtils.CodeUtils;
using static NmkdUtils.Enums;

namespace NmkdUtils
{
    public static class ConfigMgr<T> where T : new()
    {
        /// <summary> Config file path </summary>
        public static string CfgFile = "";

        private static T? _config = default;

        /// <summary> Config object </summary>
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
            CfgFile = Path.Combine(PathUtils.ExeDir, "config.json");
        }

        /// <summary> Reads the config from disk, if it does not exist, a new config object will be returned. </summary>
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

                _config = IoUtils.ReadTextFile(CfgFile).FromJson<T>(exHandling: ExceptionHandling.Throw);

                if (_config == null)
                {
                    Try(() => File.Move(CfgFile, CfgFile + ".invalid", true));
                    Logger.LogErr($"Config file '{CfgFile}' is invalid, using new config.");
                    _config = new T();
                }
            }
            catch (Exception ex)
            {
                Logger.Log(ex, "Failed to read config");
            }
        }

        /// <summary> Writes the config to disk. </summary>
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
