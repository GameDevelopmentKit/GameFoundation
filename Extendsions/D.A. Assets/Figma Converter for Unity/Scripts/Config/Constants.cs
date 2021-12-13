#if UNITY_EDITOR
namespace DA_Assets
{
    public class Constants
    {
        public const string PRODUCT_VERSION = "1.0.7";
        public const string PRODUCT_NAME = "Figma Converter for Unity";
        public const string PUBLISHER = "D.A. Assets";
        public const string JSON_FILE_NAME = "JsonResponse.txt";
        public const string LOCALIZATION_FILE_NAME = "localization.csv";
        public const string TG_LINK = "t.me/da_assets_publisher";

        public const string JSONNET_DEFINE = "JSON_NET_EXISTS";
        public const string TRUESHADOW_DEFINE = "TRUESHADOW_EXISTS";
        public const string TEXTMESHPRO_DEFINE = "TMPRO_EXISTS";
        public const string MPUIKIT_DEFINE = "MPUIKIT_EXISTS";
        public const string PUI_DEFINE = "PUI_EXISTS";
        public const string I2LOC_DEFINE = "I2LOC_EXISTS";

        public const string EVENT_SYSTEM_GAMEOBJECT_NAME = "EventSystem";
        public const string CANVAS_GAMEOBJECT_NAME = "Canvas";
        public const string I2LOC_GAMEOBJECT_NAME = "script / Language Source";
        public const float PROBABILITY_MATCHING_TAGS = 0.8f;
        public const float PROBABILITY_MATCHING_FONS = 0.7f;
        public const int GAMEOBJECT_NAME_MAX_LENGHT = 32;

        internal const string API_LINK = "https://api.figma.com/v1/files/{0}?geometry=paths";
        internal const string CLIENT_ID = "LaB1ONuPoY7QCdfshDbQbT";
        internal const string CLIENT_SECRET = "E9PblceydtAyE7Onhg5FHLmnvingDp";
        internal const string REDIRECT_URI = "http://localhost:1923/";
        internal const string AUTH_URL = "https://www.figma.com/api/oauth/token?client_id={0}&client_secret={1}&redirect_uri={2}&code={3}&grant_type=authorization_code";
        internal const string OAUTH_URL = "https://www.figma.com/oauth?client_id={0}&redirect_uri={1}&scope=file_read&state={2}&response_type=code";
    }
}
#endif