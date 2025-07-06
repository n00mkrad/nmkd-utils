using NDesk.Options;
using static NmkdUtils.CodeUtils;
using static NmkdUtils.Media.StreamFiltering;

namespace NmkdUtils.Extensions
{
    public static class ArgParseExtensions
    {
        public class Options
        {
            public OptionSet? OptionsSet { get; set; } = null;
            public string BasicUsage { get; set; } = "";
            public string AdditionalHelpText { get; set; } = "";
            public bool AddHelpArg { get; set; } = true;
            public bool AlwaysPrintHelp { get; set; } = false;
            public bool PrintLooseArgs { get; set; } = true;
            public bool PrintHelpIfNoArgs { get; set; } = true;
            public List<string> InvalidArgs { get; set; } = [];

            public Options(string basicUsage = "", OptionSet? options = null, string additionalHelp = "", bool addHelpArg = true, bool alwaysPrintHelp = false, bool printLooseArgs = true, bool printHelpIfNoArgs = true)
            {
                BasicUsage = basicUsage;
                OptionsSet = options;
                AdditionalHelpText = additionalHelp;
                AddHelpArg = addHelpArg;
                AlwaysPrintHelp = alwaysPrintHelp;
                PrintLooseArgs = printLooseArgs;
                PrintHelpIfNoArgs = printHelpIfNoArgs;
            }
        }

        public class PathConfig
        {

            public List<string> Paths = new();
            public bool AllowRecurse { get; set; } = false;
            public int MaxRecurseDepth { get; set; } = 20;
            public bool AllowEmptyFiles { get; set; } = false;
            public string FilesWildcard { get; set; } = "*";
            public Enums.Sort Sort { get; set; } = Enums.Sort.AToZ;

            public List<string> GetValidFiles()
            {
                Paths = Paths.Select(p => p.Replace("\"", "").Trim().TrimEnd('\\')).Distinct().ToList(); // Remove trailing backslashes & remove duplicates
                var validDirs = Paths.Where(dirPath => IoUtils.ValidatePath(dirPath, IoUtils.PathType.Dir)).Select(Path.GetFullPath).ToList(); // Collect valid directories from input paths
                validDirs.ForEach(d => Paths.AddRange(IoUtils.GetFilePaths(d, AllowRecurse, FilesWildcard))); // Add files from directories to the list of paths
                var existMode = AllowEmptyFiles ? IoUtils.ExistMode.MustExist : IoUtils.ExistMode.NotEmpty;
                var validFiles = Paths.Where(p => IoUtils.ValidatePath(p, IoUtils.PathType.File, existMode)).Select(Path.GetFullPath).Distinct(); // Filter out invalid files, get full paths, remove duplicates

                IoUtils.SortFiles(validFiles, Sort);
                return validFiles.ToList();
            }

            public List<FileInfo> GetValidFileInfos() => GetValidFiles().Select(f => new FileInfo(f)).ToList();
        }

        public static PathConfig AddPathConfig(this Options opts, Enums.Sort? sort = null, bool? allowEmptyFiles = null)
        {
            opts.AddHelpArgIfNotPresent();
            var pathCfg = new PathConfig();

            void AddPathWithBasicValidation (string path)
            {
                if(Assert(path.StartsWith('-'), () => opts.InvalidArgs.Add(path))) // Assume this is an argument; paths starting with a hyphen must be wrapped in quotes
                    return;

                // If path has no slashes, it must be relative, which we can check for in advance
                if (!path.Replace('/', '\\').Contains('\\'))
                {
                    if (Assert(!File.Exists(path) || !Directory.Exists(path), () => opts.InvalidArgs.Add(path)))
                        return;
                }

                pathCfg.Paths.Add(path);
            }

            opts.OptionsSet.Add($"title__{nameof(PathConfig)}", $"File Search:", v => { });
            opts.OptionsSet.Add("p_r|recurse", $"Allow recursive search for files - Only relevant when passing directory paths", v => pathCfg.AllowRecurse = v != null);
            opts.OptionsSet.Add("p_d|recurse_depth=", $"Maximum recursion depth for file search - Only relevant if recurse is enabled|INT", v => pathCfg.AllowRecurse = v != null);
            opts.OptionsSet.Add("p_wc|file_wildcard=", $"Filter files using a wildcard pattern - Only relevant when passing directory paths|WC", v => pathCfg.FilesWildcard = v.Trim());
            opts.OptionsSet.Add("<>", "File/directory paths - Wrap paths starting with a hyphen or containing spaces in quotes!", v => AddPathWithBasicValidation(v.Trim().TrimEnd('\\')));
            if (sort.HasValue) pathCfg.Sort = sort.Value;
            if (allowEmptyFiles.HasValue) pathCfg.AllowEmptyFiles = allowEmptyFiles.Value;
            return pathCfg;
        }

        public static void PromptForPathConfigOptions(ref PathConfig pathCfg)
        {
            if (pathCfg.Paths.All(File.Exists)) // All paths are files, so directory search options are not relevant
                return;

            pathCfg.AllowRecurse = CliUtils.ReadBool("Allow recursive search for files");

            if (pathCfg.AllowRecurse)
            {
                int? depth = CliUtils.ReadInt($"Maximum recursion depth for file search (Default = {pathCfg.MaxRecurseDepth}):");
                pathCfg.FilesWildcard = CliUtils.ReadLine("Filter files using a wildcard pattern (Default = All):").Trim();
                pathCfg.MaxRecurseDepth = depth.GetValueOrDefault(pathCfg.MaxRecurseDepth).Clamp(1, 100);
            }
        }

        public static void PrintHelp(this Options opts, bool colors = true, bool toFile = false)
        {
            string s = opts.GetHelpStr();

            if (!colors)
            {
                Logger.Log(s, toFile: toFile);
                return;
            }

            var lines = s.SplitIntoLines();

            Logger.Log(lines[0], toFile: toFile); // "Usage:" line
            Logger.Log(lines[1], customColor: ConsoleColor.Green, toFile: toFile); // Basic Usage description

            // Merge consecutive argument lines into single strings to reduce log calls; color others (title lines, etc.) differently
            for (int i = 2; i < lines.Length; i++)
            {
                if (lines[i].Contains(" : "))
                {
                    var line = lines[i];
                    while (i + 1 < lines.Length && lines[i + 1].Contains(" : "))
                    {
                        line += Environment.NewLine + lines[i + 1];
                        i++;
                    }
                    Logger.Log(line, customColor: ConsoleColor.White, toFile: toFile);
                }
                else
                {
                    Logger.Log(lines[i], customColor: ConsoleColor.DarkGray, toFile: toFile);
                }
            }

        }

        private const string TitlePfx = "-title__";

        public static string GetHelpStr(this Options opts, bool pad = true, bool linebreaks = false, bool newLines = false)
        {
            var lines = new List<string>();
            string looseStr = "<>";
            var allNames = new List<List<string>>();

            if (newLines)
            {
                lines.Add("");
            }

            // Collect and filter each option's names
            foreach (var opt in opts.OptionsSet)
            {
                var namesList = opt.GetNames().Select(s => $"-{s}".Replace("-<>", looseStr)).ToList();

                if (namesList.Contains(looseStr) && !opts.PrintLooseArgs)
                    continue;

                allNames.Add(namesList);
            }

            // Compute maximum width for each alias column
            var colWidths = new List<int>();
            if (allNames.Any())
            {
                int maxCols = allNames.Max(n => n.Count);
                for (int i = 0; i < maxCols; i++)
                    colWidths.Add(0);

                foreach (var namesList in allNames)
                {
                    if (namesList[0].StartsWith(TitlePfx)) // Ignore title lines
                        continue;

                    for (int i = 0; i < namesList.Count; i++)
                        colWidths[i] = Math.Max(colWidths[i], namesList[i].Length);
                }
            }

            // Build each help line with proper padding
            foreach (var opt in opts.OptionsSet)
            {
                var names = opt.GetNames().Select(s => $"-{s}".Replace("-<>", looseStr)).ToList();

                if (names.Contains(looseStr) && !opts.PrintLooseArgs)
                    continue;

                string namesStr;

                if (pad)
                {
                    var padded = new List<string>();
                    for (int i = 0; i < names.Count; i++)
                        padded.Add(names[i].PadRight(colWidths[i]));
                    // add empty columns for missing aliases
                    for (int i = names.Count; i < colWidths.Count; i++)
                        padded.Add(new string(' ', colWidths[i]));
                    namesStr = padded.Join(" ");
                }
                else
                {
                    namesStr = names.Join(" ");
                }

                if (namesStr.StartsWith(TitlePfx))
                {
                    lines.Add(opt.Description);
                    continue;
                }

                string v = "";

                if(opt.OptionValueType != OptionValueType.None)
                {
                    if(opt.Description.EndsWith("|INT")) v = " <INT>";
                    else if(opt.Description.EndsWith("|FLT")) v = " <FLOAT>";
                    else if(opt.Description.EndsWith("|WC")) v = " <WCARD>";
                    // else if(opt.Description.EndsWith("|STR")) v = " <STRING>";
                    else v = " <VALUE>";
                }

                string desc = opt.Description.IsEmpty() ? "?" : opt.Description.Split('|')[0];
                lines.Add($"{namesStr}{v.PadRight(8)} : {desc}");

                if (newLines)
                {
                    lines.AddRange(["", ""]);
                }
            }

            string s = $"Usage:{Environment.NewLine}{PathUtils.ExeName} {opts.BasicUsage}{Environment.NewLine}{lines.Join(Environment.NewLine)}{Environment.NewLine}";

            if (linebreaks)
            {
                s = s.Replace(" : ", Environment.NewLine);
            }

            return s;
        }

        public static void AddHelpArgIfNotPresent(this Options opts)
        {
            bool alreadyHasHelpArg = false;
            bool hasLooseArgsEntry = false;

            foreach (var opt in opts.OptionsSet)
            {
                if (!hasLooseArgsEntry && opt.GetNames().Contains("<>"))
                {
                    hasLooseArgsEntry = true;
                }

                if (opt.Description.Low() == "show help" || opt.GetNames().Contains("help") || opt.GetNames().Any(n => n == "h"))
                {
                    alreadyHasHelpArg = true;
                    break;
                }
            }

            if (!alreadyHasHelpArg)
            {
                var helpItem = new OptionSet() { { "h|help", "Show help", v => opts.PrintHelp() } }.First();
                int insertIdx = hasLooseArgsEntry ? opts.OptionsSet.Count - 1 : opts.OptionsSet.Count;
                opts.OptionsSet.Insert(insertIdx, helpItem);
            }
        }

        public static bool TryParseOptions(this Options opts, IEnumerable<string> args, bool returnFalseIfShowHelp = true)
        {
            try
            {
                if (opts.AddHelpArg)
                {
                    opts.AddHelpArgIfNotPresent();
                }

                Logger.WaitForEmptyQueue();

                bool hasAnyArgs = args.Where(a => a.Trim() != "/?").Any();

                if (hasAnyArgs)
                {
                    var parsed = opts.OptionsSet.Parse(args);
                }

                if (opts.AlwaysPrintHelp || (!hasAnyArgs && opts.PrintHelpIfNoArgs))
                {
                    opts.PrintHelp();

                    if (returnFalseIfShowHelp)
                        return false;
                }

                if (opts.InvalidArgs.Any())
                {
                    Logger.LogErr($"Invalid arguments: {opts.InvalidArgs.Join(" ")}");
                    return false;
                }

                return hasAnyArgs;
            }
            catch (Exception ex)
            {
                Logger.LogErr($"Failed to parse CLI options! ({ex.Message})");
                Logger.Log(ex.StackTrace, level: Logger.Level.Error, print: false);
                return false;
            }
        }
    }
}
