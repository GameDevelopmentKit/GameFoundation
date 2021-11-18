// ReSharper disable once CheckNamespace
namespace DarkTonic.MasterAudio {
    /// <summary>
    /// This class contains various string utility methods.
    /// </summary>
    // ReSharper disable once CheckNamespace
    public static class UtilStrings {
        /// <summary>
        /// This method is used to name Sound Groups, Variations and Playlists based on the first clip dragged in. It removes spaces at the end and beginning of the audio file name. 
        /// </summary>
        /// <param name="untrimmed">The string to trim.</param>
        /// <returns>The string with no spaces.</returns>
        public static string TrimSpace(string untrimmed) {
            if (string.IsNullOrEmpty(untrimmed)) {
                return string.Empty;
            }

            return untrimmed.Trim();
        }

        /// <summary>
        /// This method is used to make sure no apostrophes or quotes are in XML attributes. 
        /// </summary>
        /// <param name="source">The string to fix.</param>
        /// <returns>The string with no illegal characters.</returns>
        public static string ReplaceUnsafeChars(string source) {
            source = source.Replace("'", "&apos;");
            source = source.Replace("\"", "&quot;");
            source = source.Replace("&", "&amp;");

            return source;
        }
    }
}