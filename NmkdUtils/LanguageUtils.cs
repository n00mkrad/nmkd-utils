using System.Diagnostics;
using static NmkdUtils.Data.Languages;

namespace NmkdUtils
{
    public class LanguageUtils
    {
        [DebuggerDisplay("{Name}/{NativeName} - ISO 639-1 {Iso6391}, ISO 639-2 {Iso6392}, ISO 639-2/B {Iso6392B}")]
        public class Language
        {
            /// <summary> Language Family (e.g. "Indo-European") </summary>
            public string Family { get; set; }
            /// <summary> Language Name in English (e.g. "Dutch") </summary>
            public string Name { get; set; }
            /// <summary> Language Name(s) in native Language (e.g. "Nederlands") </summary>
            public string NativeName { get; set; }
            /// <summary> Language Name(s) in native Language, as List for cases where there are multiple names </summary>
            public List<string> NativeNames => NativeName.Split(",").Select(n => n.Trim()).ToList();
            /// <summary> ISO 639-1 code (2 chars, e.g. "nl") </summary>
            public string Iso6391 { get; set; }
            /// <summary> ISO 639-2 code (3 chars, based on native name, e.g. "nld") </summary>
            public string Iso6392 { get; set; }
            /// <summary> ISO 639-2/B code, if available (3 chars, based on English name, e.g. "dut") </summary>
            public string Iso6392B { get; set; }

            // Constructor to initialize the Language object
            public Language(string family, string name, string nativeName, string iso6391, string iso6392 = "", string iso6392B = "")
            {
                Family = family;
                Name = name;
                NativeName = nativeName;
                Iso6391 = iso6391;
                Iso6392 = iso6392;
                Iso6392B = iso6392B;
            }
        }

        public static Language GetUndefined() => new Language("Undefined", "Undefined", "Undefined", "un", "und", "und"); // Note: ISO-639-1 "un" code is non-standard, but ISO-639-2 "und" is.

        /// <summary> Gets a language by its English name or ISO-639 code </summary>
        public static Language GetLangByNameOrCode(string nameOrCode)
            => GetLangByName(nameOrCode) ?? GetLangByCode(nameOrCode);

        /// <summary> Gets a language by its English name </summary>
        public static Language GetLangByName(string name)
            => Iso639.Where(l => l.Name.IsNotEmpty() && l.Name.Low() == name.Low()).FirstOrDefault();

        /// <summary> Gets a language by its code (639-1, 639-2, or 639-2/B) </summary>
        public static Language GetLangByCode(string code)
        {
            code = code.Low();
            return GetLangByCodes(code, code, code);
        }

        /// <summary> Gets a language by any of its codes (639-1, 639-2, or 639-2/B) </summary>
        public static Language GetLangByCodes(string iso6391, string iso6392, string iso6392B = "")
        {
            var lang = Iso639.Where(l => l.Iso6391.IsNotEmpty() && l.Iso6391 == iso6391).FirstOrDefault();

            if (lang == null && iso6392.IsNotEmpty())
                lang = Iso639.Where(l => l.Iso6392.IsNotEmpty() && l.Iso6392 == iso6392).FirstOrDefault();

            if (lang == null && iso6392B.IsNotEmpty())
                lang = Iso639.Where(l => l.Iso6392B.IsNotEmpty() && l.Iso6392B == iso6392B).FirstOrDefault();

            return lang;
        }

        public static void PrettyPrintLangs(string filter = "", bool inclFamily = false, bool logToFile = false)
        {
            string msg = $"{"Name",-55}  {"Native Name",-40}  {"ISO 639-1",-10}  {"ISO 639-2",-10}  {"ISO 639-2/B",-10}{(inclFamily ? "  Family" : "")}";

            foreach (var lang in Iso639)
            {
                string l = $"\n{lang.Name,-55}  {lang.NativeName,-40}  {lang.Iso6391,-10}  {lang.Iso6392,-10}  {lang.Iso6392B,-10}{(inclFamily ? $"  {lang.Family}" : "")}";

                if (filter.IsNotEmpty() && !l.Contains(filter))
                    continue;

                msg += l;
            }

            Logger.Log($"Languages:\n{msg}", toFile: logToFile);
        }

        public static List<string> GetCountries(Language lang)
            => Countries.Where(kvp => kvp.Key == lang.Name).SelectMany(kvp => kvp.Value).Distinct().ToList();
    }
}
