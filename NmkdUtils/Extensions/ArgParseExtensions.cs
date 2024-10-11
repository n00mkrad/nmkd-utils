

using NDesk.Options;

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

            public Options(string basicUsage = "", OptionSet options = null, string additionalHelp = "", bool addHelpArg = true, bool alwaysPrintHelp = false, bool printLooseArgs = true, bool printHelpIfNoArgs = true)
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

        public static void PrintHelp(this Options opts)
        {
            Logger.Log(opts.GetHelpStr());
        }

        public static string GetHelpStr(this Options opts, bool pad = true, bool linebreaks = false, bool newLines = false)
        {
            var lines = new List<string>();
            var lengths = new List<int>();

            if (newLines)
            {
                lines.Add("");
            }

            foreach (var opt in opts.OptionsSet)
            {
                string names = opt.GetNames().Select(s => $"-{s}").Join();
                lengths.Add(names.Length);
            }

            string looseStr = "<Loose Arguments>";
            var maxLen = Math.Max(lengths.Max(), looseStr.Length);

            foreach (var opt in opts.OptionsSet)
            {
                var names = opt.GetNames().Select(s => $"-{s}".Replace("-<>", looseStr)).ToList();
                if (names.Contains(looseStr) && !opts.PrintLooseArgs) continue;
                string v = opt.OptionValueType == NDesk.Options.OptionValueType.None ? "        " : " <VALUE>";
                string desc = opt.Description.IsEmpty() ? "?" : opt.Description;
                lines.Add(pad ? $"{names.Join().PadRight(maxLen)}{v} : {desc}" : $"{names.Join()}{v} : {desc}");

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
                var helpItem = new NDesk.Options.OptionSet() { { "h|help", "Show help", v => opts.PrintHelp() } }.First();
                int insertIdx = hasLooseArgsEntry ? opts.OptionsSet.Count - 1 : opts.OptionsSet.Count;
                opts.OptionsSet.Insert(insertIdx, helpItem);
            }
        }

        public static bool TryParseOptions(this Options opts, IEnumerable<string> args)
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
                    opts.OptionsSet.Parse(args);
                }

                if (opts.AlwaysPrintHelp || (!hasAnyArgs && opts.PrintHelpIfNoArgs))
                {
                    opts.PrintHelp();
                }

                return hasAnyArgs;
            }
            catch (Exception ex)
            {
                Logger.Log(ex, "Failed to parse options!");
                return false;
            }
        }
    }
}
