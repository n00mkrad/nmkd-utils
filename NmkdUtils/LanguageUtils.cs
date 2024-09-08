

namespace NmkdUtils
{
    public class LanguageUtils
    {
        public class Language
        {
            public string Family { get; set; }
            public string Name { get; set; }
            public string NativeName { get; set; }
            public string Iso6391 { get; set; }
            public string Iso6392 { get; set; }
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

        public static Language GetLangByNameOrCode(string nameOrCode)
        {
            return GetLangByName(nameOrCode) ?? GetLangByCode(nameOrCode);
        }

        public static Language GetLangByName(string name)
        {
            return Languages.Where(l => l.Name.IsNotEmpty() && l.Name.Low() == name.Low()).FirstOrDefault();
        }

        public static Language GetLangByCode(string code)
        {
            return GetLangByCodes(code, code, code);
        }

        public static Language GetLangByCodes(string iso6391, string iso6392, string iso6392B = "")
        {
            var lang = Languages.Where(l => l.Iso6391.IsNotEmpty() && l.Iso6391 == iso6391).FirstOrDefault();

            if (lang == null && iso6392.IsNotEmpty())
                lang = Languages.Where(l => l.Iso6392.IsNotEmpty() && l.Iso6392 == iso6392).FirstOrDefault();

            if (lang == null && iso6392B.IsNotEmpty())
                lang = Languages.Where(l => l.Iso6392B.IsNotEmpty() && l.Iso6392B == iso6392B).FirstOrDefault();

            return lang;
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
            new ("Indo-European", "Dutch", "Nederlands, Vlaams", "nl", "nld", "dut" ),
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
            new ("Constructed", "Interlingue", "Originally called Occidental; then Interlingue after WWII", "ie", "ile" ),
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
