

using System.Diagnostics;

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

        public static Language GetUndefined ()
        {
            return new Language("Undefined", "Undefined", "Undefined", "un", "und", "und"); // Note: ISO-639-1 "un" code is non-standard, but ISO-639-2 "und" is.
        }

        /// <summary> Gets a language by its English name or ISO-639 code </summary>
        public static Language GetLangByNameOrCode(string nameOrCode)
        {
            return GetLangByName(nameOrCode) ?? GetLangByCode(nameOrCode);
        }

        /// <summary> Gets a language by its English name </summary>
        public static Language GetLangByName(string name)
        {
            return Languages.Where(l => l.Name.IsNotEmpty() && l.Name.Low() == name.Low()).FirstOrDefault();
        }

        /// <summary> Gets a language by its code (639-1, 639-2, or 639-2/B) </summary>
        public static Language GetLangByCode(string code)
        {
            code = code.Low();
            return GetLangByCodes(code, code, code);
        }

        /// <summary> Gets a language by its code (639-1, 639-2, or 639-2/B) </summary>
        public static Language GetLangByCodes(string iso6391, string iso6392, string iso6392B = "")
        {
            var lang = Languages.Where(l => l.Iso6391.IsNotEmpty() && l.Iso6391 == iso6391).FirstOrDefault();

            if (lang == null && iso6392.IsNotEmpty())
                lang = Languages.Where(l => l.Iso6392.IsNotEmpty() && l.Iso6392 == iso6392).FirstOrDefault();

            if (lang == null && iso6392B.IsNotEmpty())
                lang = Languages.Where(l => l.Iso6392B.IsNotEmpty() && l.Iso6392B == iso6392B).FirstOrDefault();

            return lang;
        }

        public static void PrettyPrintLangs(string filter = "", bool includeFamily = false)
        {
            string msg = $"{"Name",-55}  {"Native Name",-40}  {"ISO 639-1",-10}  {"ISO 639-2",-10}  {"ISO 639-2/B",-10}";

            if (includeFamily)
            {
                msg += "  Family";
            }

            foreach (var lang in Languages)
            {
                string l = $"\n{lang.Name,-55}  {lang.NativeName,-40}  {lang.Iso6391,-10}  {lang.Iso6392,-10}  {lang.Iso6392B,-10}{(includeFamily ? $"  {lang.Family}" : "")}";

                if (filter.IsNotEmpty() && !l.Contains(filter))
                {
                    continue;
                }

                msg += l;
            }

            Logger.Log($"Languages\n{msg}");
        }

        public static readonly List<Language> Languages = new()
        {
            new ("Northwest Caucasian", "Abkhaz", "аҧсуа бызшәа, аҧсшәа", "ab", "abk" ),
            new ("Afro-Asiatic", "Afar", "Afaraf", "aa", "aar"),
            new ("Indo-European", "Afrikaans", "Afrikaans", "af", "afr" ),
            new ("Niger–Congo", "Akan", "Akan", "ak", "aka" ),
            new ("Indo-European", "Albanian", "Shqip", "sq", "sqi", "alb" ),
            new ("Afro-Asiatic", "Amharic", "አማርኛ", "am", "amh"),
            new ("Afro-Asiatic", "Arabic", "العربية", "ar", "ara"),
            new ("Indo-European", "Aragonese", "aragonés", "an", "arg"),
            new ("Indo-European", "Armenian", "Հայերեն", "hy", "hye", "arm" ),
            new ("Indo-European", "Assamese", "অসমীয়া", "as", "asm"),
            new ("Northeast Caucasian", "Avaric", "авар мацӀ, магӀарул мацӀ", "av", "ava" ),
            new ("Indo-European", "Avestan", "avesta", "ae", "ave"),
            new ("Aymaran", "Aymara", "aymar aru", "ay", "aym"),
            new ("Turkic", "Azerbaijani", "azərbaycan dili", "az", "aze"),
            new ("Niger–Congo", "Bambara", "bamanankan", "bm", "bam"),
            new ("Turkic", "Bashkir", "башҡорт теле", "ba", "bak" ),
            new ("Language isolate", "Basque", "euskara, euskera", "eu", "eus", "baq" ),
            new ("Indo-European", "Belarusian", "беларуская мова", "be", "bel"),
            new ("Indo-European", "Bengali, Bangla", "বাংলা", "bn", "ben" ),
            new ("Indo-European", "Bihari", "भोजपुरी", "bh", "bih"),
            new ("Creole", "Bislama", "Bislama", "bi", "bis"),
            new ("Indo-European", "Bosnian", "bosanski jezik", "bs", "bos"),
            new ("Indo-European", "Breton", "brezhoneg", "br", "bre"),
            new ("Indo-European", "Bulgarian", "български език", "bg", "bul"),
            new ("Sino-Tibetan", "Burmese", "ဗမာစာ", "my", "mya", "bur"),
            new ("Indo-European", "Catalan", "català", "ca", "cat"),
            new ("Austronesian", "Chamorro", "Chamoru", "ch", "cha" ),
            new ("Northeast Caucasian", "Chechen", "нохчийн мотт", "ce", "che"),
            new ("Niger–Congo", "Chichewa, Chewa, Nyanja", "chiCheŵa, chinyanja", "ny", "nya" ),
            new ("Sino-Tibetan", "Chinese", "中文 (Zhōngwén), 汉语, 漢語", "zh", "zho", "chi"),
            new ("Turkic", "Chuvash", "чӑваш чӗлхи", "cv", "chv"),
            new ("Indo-European", "Cornish", "Kernewek", "kw", "cor"),
            new ("Indo-European", "Corsican", "corsu, lingua corsa", "co", "cos"),
            new ("Algonquian", "Cree", "ᓀᐦᐃᔭᐍᐏᐣ", "cr", "cre"),
            new ("Indo-European", "Croatian", "hrvatski jezik", "hr", "hrv" ),
            new ("Indo-European", "Czech", "čeština, český jazyk", "cs", "ces", "cze" ),
            new ("Indo-European", "Danish", "dansk", "da", "dan"),
            new ("Indo-European", "Divehi, Dhivehi, Maldivian", "ދިވެހި", "dv", "div"),
            new ("Indo-European", "Dutch", "Nederlands", "nl", "nld", "dut" ),
            new ("Sino-Tibetan", "Dzongkha", "རྫོང་ཁ", "dz", "dzo" ),
            new ("Indo-European", "English", "English", "en", "eng" ),
            new ("Constructed", "Esperanto", "Esperanto", "eo", "epo" ),
            new ("Uralic", "Estonian", "eesti, eesti keel", "et", "est" ),
            new ("Niger–Congo", "Ewe", "Eʋegbe", "ee", "ewe"),
            new ("Indo-European", "Faroese", "føroyskt", "fo", "fao"),
            new ("Austronesian", "Fijian", "vosa Vakaviti", "fj", "fij" ),
            new ("Austronesian", "Filipino", "Wikang Filipino", "", "fil" ),
            new ("Uralic", "Finnish", "suomi, suomen kieli", "fi", "fin"),
            new ("Indo-European", "French", "français, langue française", "fr", "fra", "fre"),
            new ("Niger–Congo", "Fula, Fulah, Pulaar, Pular", "Fulfulde, Pulaar, Pular", "ff", "ful"),
            new ("Indo-European", "Galician", "galego", "gl", "glg" ),
            new ("South Caucasian", "Georgian", "ქართული", "ka", "kat", "geo" ),
            new ("Indo-European", "German", "Deutsch", "de", "deu", "ger" ),
            new ("Indo-European", "Greek (modern)", "ελληνικά", "el", "ell", "gre"),
            new ("Tupian", "Guaraní", "Avañe'ẽ", "gn", "grn"),
            new ("Indo-European", "Gujarati", "ગુજરાતી", "gu", "guj" ),
            new ("Creole", "Haitian, Haitian Creole", "Kreyòl ayisyen", "ht", "hat" ),
            new ("Afro-Asiatic", "Hausa", "(Hausa) هَوُسَ", "ha", "hau"),
            new ("Afro-Asiatic", "Hebrew (modern)", "עברית", "he", "heb"),
            new ("Niger–Congo", "Herero", "Otjiherero", "hz", "her" ),
            new ("Indo-European", "Hindi", "हिन्दी, हिंदी", "hi", "hin" ),
            new ("Austronesian", "Hiri Motu", "Hiri Motu", "ho", "hmo"),
            new ("Uralic", "Hungarian", "magyar", "hu", "hun" ),
            new ("Constructed", "Interlingua", "Interlingua", "ia", "ina" ),
            new ("Austronesian", "Indonesian", "Bahasa Indonesia", "id", "ind"),
            new ("Constructed", "Interlingue", "Formerly Occidental", "ie", "ile" ),
            new ("Indo-European", "Irish", "Gaeilge", "ga", "gle" ),
            new ("Niger–Congo", "Igbo", "Asụsụ Igbo", "ig", "ibo" ),
            new ("Eskimo–Aleut", "Inupiaq", "Iñupiaq, Iñupiatun", "ik", "ipk" ),
            new ("Constructed", "Ido", "Ido", "io", "ido" ),
            new ("Indo-European", "Icelandic", "Íslenska", "is", "isl", "ice" ),
            new ("Indo-European", "Italian", "Italiano", "it", "ita"),
            new ("Eskimo–Aleut", "Inuktitut", "ᐃᓄᒃᑎᑐᑦ", "iu", "iku"),
            new ("Japonic", "Japanese", "日本語 (にほんご)", "ja", "jpn" ),
            new ("Austronesian", "Javanese", "ꦧꦱꦗꦮ, Basa Jawa", "jv", "jav"),
            new ("Eskimo–Aleut", "Kalaallisut, Greenlandic", "kalaallisut, kalaallit oqaasii", "kl", "kal"),
            new ("Dravidian", "Kannada", "ಕನ್ನಡ", "kn", "kan" ),
            new ("Nilo-Saharan", "Kanuri", "Kanuri", "kr", "kau"),
            new ("Indo-European", "Kashmiri", "कश्मीरी, كشميري‎", "ks", "kas"),
            new ("Turkic", "Kazakh", "қазақ тілі", "kk", "kaz"),
            new ("Austroasiatic", "Khmer", "ខ្មែរ, ខេមរភាសា, ភាសាខ្មែរ", "km", "khm" ),
            new ("Niger–Congo", "Kikuyu, Gikuyu", "Gĩkũyũ", "ki", "kik" ),
            new ("Niger–Congo", "Kinyarwanda", "Ikinyarwanda", "rw", "kin"),
            new ("Turkic", "Kyrgyz", "Кыргызча, Кыргыз тили", "ky", "kir" ),
            new ("Uralic", "Komi", "коми кыв", "kv", "kom"),
            new ("Niger–Congo", "Kongo", "Kikongo", "kg", "kon" ),
            new ("Koreanic", "Korean", "한국어", "ko", "kor" ),
            new ("Indo-European", "Kurdish", "Kurdî, كوردی‎", "ku", "kur"),
            new ("Niger–Congo", "Kwanyama, Kuanyama", "Kuanyama", "kj", "kua" ),
            new ("Indo-European", "Latin", "latine, lingua latina", "la", "lat" ),
            new ("Indo-European", "Luxembourgish, Letzeburgesch", "Lëtzebuergesch", "lb", "ltz" ),
            new ("Niger–Congo", "Ganda", "Luganda", "lg", "lug" ),
            new ("Indo-European", "Limburgish, Limburgan, Limburger", "Limburgs", "li", "lim" ),
            new ("Niger–Congo", "Lingala", "Lingála", "ln", "lin" ),
            new ("Tai–Kadai", "Lao", "ພາສາລາວ", "lo", "lao" ),
            new ("Indo-European", "Lithuanian", "lietuvių kalba", "lt", "lit" ),
            new ("Niger–Congo", "Luba-Katanga", "Tshiluba", "lu", "lub" ),
            new ("Indo-European", "Latvian", "latviešu valoda", "lv", "lav" ),
            new ("Indo-European", "Manx", "Gaelg, Gailck", "gv", "glv"),
            new ("Indo-European", "Macedonian", "македонски јазик", "mk", "mkd", "mac"),
            new ("Austronesian", "Malagasy", "fiteny malagasy", "mg", "mlg" ),
            new ("Austronesian", "Malay", "bahasa Melayu, بهاس ملايو‎", "ms", "msa", "may"),
            new ("Dravidian", "Malayalam", "മലയാളം", "ml", "mal" ),
            new ("Afro-Asiatic", "Maltese", "Malti", "mt", "mlt"),
            new ("Austronesian", "Māori", "te reo Māori", "mi", "mri", "mao"),
            new ("Indo-European", "Marathi (Marāṭhī)", "मराठी", "mr", "mar"),
            new ("Austronesian", "Marshallese", "Kajin M̧ajeļ", "mh", "mah"),
            new ("Mongolic", "Mongolian", "Монгол хэл", "mn", "mon" ),
            new ("Austronesian", "Nauruan", "Dorerin Naoero", "na", "nau" ),
            new ("Dené–Yeniseian", "Navajo, Navaho", "Diné bizaad", "nv", "nav" ),
            new ("Niger–Congo", "Northern Ndebele", "isiNdebele", "nd", "nde" ),
            new ("Indo-European", "Nepali", "नेपाली", "ne", "nep" ),
            new ("Niger–Congo", "Ndonga", "Owambo", "ng", "ndo" ),
            new ("Indo-European", "Norwegian Bokmål", "Norsk bokmål", "nb", "nob" ),
            new ("Indo-European", "Norwegian Nynorsk", "Norsk nynorsk", "nn", "nno" ),
            new ("Indo-European", "Norwegian", "Norsk", "no", "nor" ),
            new ("Sino-Tibetan", "Nuosu", "ꆈꌠ꒿ Nuosuhxop", "ii", "iii" ),
            new ("Niger–Congo", "Southern Ndebele", "isiNdebele", "nr", "nbl" ),
            new ("Indo-European", "Occitan", "occitan, lenga d'òc", "oc", "oci" ),
            new ("Algonquian", "Ojibwe, Ojibwa", "ᐊᓂᔑᓈᐯᒧᐎᓐ", "oj", "oji" ),
            new ("Indo-European", "Old Church Slavonic, Church Slavonic, Old Bulgarian", "ѩзыкъ словѣньскъ", "cu", "chu"),
            new ("Afro-Asiatic", "Oromo", "Afaan Oromoo", "om", "orm" ),
            new ("Indo-European", "Oriya", "ଓଡ଼ିଆ", "or", "ori"),
            new ("Indo-European", "Ossetian, Ossetic", "ирон æвзаг", "os", "oss"),
            new ("Indo-European", "(Eastern) Punjabi", "ਪੰਜਾਬੀ", "pa", "pan"),
            new ("Indo-European", "Pāli", "पाऴि", "pi", "pli" ),
            new ("Indo-European", "Persian (Farsi)", "فارسی", "fa", "fas", "per" ),
            new ("Indo-European", "Polish", "język polski, polszczyzna", "pl", "pol"),
            new ("Indo-European", "Pashto, Pushto", "پښتو", "ps", "pus"),
            new ("Indo-European", "Portuguese", "Português", "pt", "por"),
            new ("Quechuan", "Quechua", "Runa Simi, Kichwa", "qu", "que"),
            new ("Indo-European", "Romansh", "rumantsch grischun", "rm", "roh"),
            new ("Niger–Congo", "Kirundi", "Ikirundi", "rn", "run"),
            new ("Indo-European", "Romanian", "Română", "ro", "ron", "rum"),
            new ("Indo-European", "Russian", "Русский", "ru", "rus" ),
            new ("Indo-European", "Sanskrit (Saṁskṛta)", "संस्कृतम्", "sa", "san"),
            new ("Indo-European", "Sardinian", "sardu", "sc", "srd" ),
            new ("Indo-European", "Sindhi", "सिन्धी, سنڌي، سندھی‎", "sd", "snd"),
            new ("Uralic", "Northern Sami", "Davvisámegiella", "se", "sme"),
            new ("Austronesian", "Samoan", "gagana fa'a Samoa", "sm", "smo" ),
            new ("Creole", "Sango", "yângâ tî sängö", "sg", "sag" ),
            new ("Indo-European", "Serbian", "српски језик", "sr", "srp"),
            new ("Indo-European", "Scottish Gaelic, Gaelic", "Gàidhlig", "gd", "gla"),
            new ("Niger–Congo", "Shona", "chiShona", "sn", "sna"),
            new ("Indo-European", "Sinhalese, Sinhala", "සිංහල", "si", "sin"),
            new ("Indo-European", "Slovak", "slovenčina, slovenský jazyk", "sk", "slk", "slo" ),
            new ("Indo-European", "Slovene", "slovenski jezik, slovenščina", "sl", "slv"),
            new ("Afro-Asiatic", "Somali", "Soomaaliga, af Soomaali", "so", "som" ),
            new ("Niger–Congo", "Southern Sotho", "Sesotho", "st", "sot"),
            new ("Indo-European", "Spanish", "Español", "es", "spa" ),
            new ("Austronesian", "Sundanese", "Basa Sunda", "su", "sun" ),
            new ("Niger–Congo", "Swahili", "Kiswahili", "sw", "swa" ),
            new ("Niger–Congo", "Swati", "SiSwati", "ss", "ssw" ),
            new ("Indo-European", "Swedish", "svenska", "sv", "swe" ),
            new ("Dravidian", "Tamil", "தமிழ்", "ta", "tam" ),
            new ("Dravidian", "Telugu", "తెలుగు", "te", "tel" ),
            new ("Indo-European", "Tajik", "тоҷикӣ, toçikī, تاجیکی‎", "tg", "tgk" ),
            new ("Tai–Kadai", "Thai", "ไทย", "th", "tha"),
            new ("Afro-Asiatic", "Tigrinya", "ትግርኛ", "ti", "tir"),
            new ("Sino-Tibetan", "Tibetan Standard, Tibetan, Central", "བོད་ཡིག", "bo", "bod", "tib"),
            new ("Turkic", "Turkmen", "Türkmen, Түркмен", "tk", "tuk" ),
            new ("Austronesian", "Tagalog", "Wikang Tagalog", "tl", "tgl" ),
            new ("Niger–Congo", "Tswana", "Setswana", "tn", "tsn" ),
            new ("Austronesian", "Tonga (Tonga Islands)", "faka Tonga", "to", "ton" ),
            new ("Turkic", "Turkish", "Türkçe", "tr", "tur" ),
            new ("Niger–Congo", "Tsonga", "Xitsonga", "ts", "tso" ),
            new ("Turkic", "Tatar", "татар теле, tatar tele", "tt", "tat" ),
            new ("Niger–Congo", "Twi", "Twi", "tw", "twi" ),
            new ("Austronesian", "Tahitian", "Reo Tahiti", "ty", "tah"),
            new ("Turkic", "Uyghur", "ئۇيغۇرچە‎, Uyghurche", "ug", "uig"),
            new ("Indo-European", "Ukrainian", "Українська", "uk", "ukr"),
            new ("Indo-European", "Urdu", "اردو", "ur", "urd" ),
            new ("Turkic", "Uzbek", "Oʻzbek, Ўзбек, أۇزبېك‎", "uz", "uzb" ),
            new ("Niger–Congo", "Venda", "Tshivenḓa", "ve", "ven" ),
            new ("Austroasiatic", "Vietnamese", "Tiếng Việt", "vi", "vie" ),
            new ("Constructed", "Volapük", "Volapük", "vo", "vol" ),
            new ("Indo-European", "Walloon", "walon", "wa", "wln" ),
            new ("Indo-European", "Welsh", "Cymraeg", "cy", "cym", "wel"),
            new ("Niger–Congo", "Wolof", "Wollof", "wo", "wol"),
            new ("Indo-European", "Western Frisian", "Frysk", "fy", "fry" ),
            new ("Niger–Congo", "Xhosa", "isiXhosa", "xh", "xho"),
            new ("Indo-European", "Yiddish", "ייִדיש", "yi", "yid"),
            new ("Niger–Congo", "Yoruba", "Yorùbá", "yo", "yor" ),
            new ("Tai–Kadai", "Zhuang, Chuang", "Saɯ cueŋƅ, Saw cuengh", "za", "zha"),
            new ("Niger–Congo", "Zulu", "isiZulu", "zu", "zul"),
        };
    }
}
