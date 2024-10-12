namespace Utilities.Utils
{
    using System.Linq;
    using UnityEngine;

    public class RegionHelper
    {
        public const string COUNTRY_CODE_DEFAULT = "us";
        public const string LANG_CODE_DEFAULT    = "en";

        public static readonly string[] ListCountryCode =
        {
            "af", "za", "ar", "sa", "be", "by", "bg", "bg", "bs", "ba", "ca", "es", "cs", "cz", "da", "dk", "de", "de", "el", "gr",
            "en", "us", "es", "es", "et", "ee", "eu", "es", "fa", "ir", "fi", "fi", "fr", "fr", "gl", "es", "he", "il", "hi", "in",
            "hr", "hr", "id", "id", "is", "is", "it", "it", "ja", "jp", "ka", "ge", "km", "kh", "kn", "in", "ko", "kr", "lo", "la",
            "lt", "lt", "mi", "nz", "ml", "in", "ms", "my", "nl", "nl", "nn", "no", "no", "no", "ph", "ph", "pt", "pt", "ro", "ro",
            "ru", "ru", "sk", "sk", "sl", "si", "so", "so", "sq", "al", "sv", "se", "ta", "in", "th", "th", "tr", "tr", "uk", "ua",
            "vi", "vn", "zh", "cn",
        };

        public static readonly string[] ListLang =
        {
            "abkhaz", "afar", "afrikaans", "akan", "albanian", "amharic", "arabic", "aragonese", "armenian", "assamese", "avaric",
            "avestan", "aymara", "azerbaijani", "bambara", "bashkir", "basque", "belarusian", "bengali", "bihari", "bislama",
            "bosnian", "breton", "bulgarian", "burmese", "catalan; valencian", "chamorro", "chechen", "chichewa;", "chinese",
            "chuvash", "cornish", "corsican", "cree", "serbocroatian", "czech", "danish", "divehi", "dutch", "english", "esperanto",
            "estonian", "ewe", "faroese", "fijian", "finnish", "french", "fula", "galician", "georgian", "german", "greek",
            "guarani", "gujarati", "haitian", "hausa", "hebrew", "herero", "hindi", "hiri motu", "hungarian", "interlingua",
            "indonesian", "interlingue", "irish", "igbo", "inupiaq", "ido", "icelandic", "italian", "inuktitut", "japanese",
            "javanese", "kalaallisut", "kannada", "kanuri", "kashmiri", "kazakh", "khmer", "kikuyu", "kinyarwanda", "kirghiz", "komi",
            "kongo", "korean", "kurdish", "kwanyama", "latin", "luxembourgish", "luganda", "limburgish", "lingala", "lao",
            "lithuanian", "luba-katanga", "latvian", "manx", "macedonian", "malagasy", "malay", "malayalam", "maltese", "mäori",
            "marathi", "marshallese", "mongolian", "nauru", "navajo", "norwegian", "north ndebele", "nepali", "ndonga",
            "norwegian nynorsk", "norwegian", "nuosu", "south ndebele", "occitan", "ojibwe", "church slavic", "oromo", "oriya",
            "ossetian", "panjabi", "päli", "persian", "polish", "pashto", "portuguese", "quechua", "romansh", "kirundi", "romanian",
            "russian", "sanskrit", "sardinian", "sindhi", "northern sami", "samoan", "sango", "serbian", "scottish gaelic", "shona",
            "sinhala", "slovak", "slovenian", "somali", "southern sotho", "spanish", "sundanese", "swahili", "swati", "swedish",
            "tamil", "telugu", "tajik", "thai", "tigrinya", "tibetan standard", "turkmen", "tagalog", "tswana", "tonga", "turkish",
            "tsonga", "tatar", "twi", "tahitian", "uighur", "ukrainian", "urdu", "uzbek", "venda", "vietnamese", "volapã¼k",
            "walloon", "welsh", "wolof", "western frisian", "xhosa", "yiddish", "yoruba", "zhuang, chuang",
        };

        public static readonly string[] ListLangCode =
        {
            "ab", "aa", "af", "ak", "sq", "am", "ar", "an", "hy", "as", "av", "ae", "ay", "az", "bm", "ba", "eu", "be", "bn", "bh",
            "bi", "bs", "br", "bg", "my", "ca", "ch", "ce", "ny", "zh", "cv", "kw", "co", "cr", "hr", "cs", "da", "dv", "nl", "en",
            "eo", "et", "ee", "fo", "fj", "fi", "fr", "ff", "gl", "ka", "de", "el", "gn", "gu", "ht", "ha", "he", "hz", "hi", "ho",
            "hu", "ia", "id", "ie", "ga", "ig", "ik", "io", "is", "it", "iu", "ja", "jv", "kl", "kn", "kr", "ks", "kk", "km", "ki",
            "rw", "ky", "kv", "kg", "ko", "ku", "kj", "la", "lb", "lg", "li", "ln", "lo", "lt", "lu", "lv", "gv", "mk", "mg", "ms",
            "ml", "mt", "mi", "mr", "mh", "mn", "na", "nv", "nb", "nd", "ne", "ng", "nn", "no", "ii", "nr", "oc", "oj", "cu", "om",
            "or", "os", "pa", "pi", "fa", "pl", "ps", "pt", "qu", "rm", "rn", "ro", "ru", "sa", "sc", "sd", "se", "sm", "sg", "sr",
            "gd", "sn", "si", "sk", "sl", "so", "st", "es", "su", "sw", "ss", "sv", "ta", "te", "tg", "th", "ti", "bo", "tk", "tl",
            "tn", "to", "tr", "ts", "tt", "tw", "ty", "ug", "uk", "ur", "uz", "ve", "vi", "vo", "wa", "cy", "wo", "fy", "xh", "yi",
            "yo", "za",
        };

        public static readonly string[] ListEUCountryCode =
            { "at", "be", "bg", "hr", "cy", "cz", "dk", "ee", "fi", "fr", "de", "gr", "hu", "ie", "it", "lv", "lt", "lu", "mt", "nl", "pl", "pt", "ro", "sk", "si", "es", "se" };

        public static readonly string[] ListEEACountryCode = { "no", "is", "li" };

        public static bool IsEUAndEEACountry(string countryCode)
        {
            return ListEUCountryCode.Contains(countryCode) || ListEEACountryCode.Contains(countryCode);
        }

        public static bool IsEUAndEEACountry()
        {
            return IsEUAndEEACountry(GetCountryCodeByDeviceLang());
        }

        //
        // Summary:
        //     The country code the user's operating system is running in.
        public static string GetCountryCodeByLang(string lang)
        {
            var countryCode = COUNTRY_CODE_DEFAULT;
            var langCode    = LANG_CODE_DEFAULT;
            lang = lang.ToLower();

            for (var i = 0; i < ListLang.Length; i++)
            {
                if (!ListLang[i].Contains(lang)) continue;
                langCode = ListLangCode[i];
                break;
            }

            for (var i = 0; i < ListCountryCode.Length; i = i + 2)
            {
                if (!langCode.Equals(ListCountryCode[i])) continue;
                countryCode = ListCountryCode[i + 1];
                break;
            }

            return countryCode;
        }

        //
        // Summary:
        //     The country code the user's operating system is running in.
        public static string GetCountryCodeByDeviceLang()
        {
            return GetCountryCodeByLang(Application.systemLanguage.ToString());
        }
    }
}