#if UNITY_EDITOR && JSON_NET_EXISTS
using DA_Assets.Exceptions;
using System;
using System.Collections.Generic;

namespace DA_Assets
{
    public class Checkers
    {
        static FigmaConverterUnity figmaConverterUnity => UnityEngine.Object.FindObjectOfType<FigmaConverterUnity>();
        public static bool IsValidSettings()
        {
            List<string> errors = new List<string>();

            bool validUrl = IsValidFigmaProjectUrl(figmaConverterUnity.mainSettings.ProjectUrl);
            if (validUrl == false)
            {
                errors.Add("Invalid figma project url.");
            }

            if (errors.Count > 0)
            {
                throw new InvalidSettingsException(errors);
            }

            return true;
        }

        public static bool IsValidFigmaProjectUrl(string url)
        {
            bool result =
                Uri.TryCreate(url, UriKind.Absolute, out Uri uriResult) &&
                (uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps) &&
                url.Contains("figma.com/file/");

            return result;
        }

        public static bool IsValidApiKey()
        {
            if (figmaConverterUnity.mainSettings.ApiKey.Length != 40)
            {
                return false;
            }

            return true;
        }
    }
}
#endif