#if UNITY_IOS
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEditor.iOS.Xcode;
using System.IO;

namespace DataManager.LocalData.Editor
{
    /// <summary>
    /// Automatically configures iOS Info.plist for Export Compliance.
    /// By setting ITSAppUsesNonExemptEncryption to false, we declare that our AES encryption
    /// (used for local save data) falls under the EAR exemption.
    /// This bypasses the manual export compliance questionnaire in App Store Connect.
    /// </summary>
    public class IOSExportCompliancePostProcessor
    {
        // Execute after the primary Xcode project generation
        [PostProcessBuild(100)]
        public static void OnPostProcessBuild(BuildTarget target, string path)
        {
            if (target != BuildTarget.iOS)
            {
                return;
            }

            var plistPath = Path.Combine(path, "Info.plist");

            if (!File.Exists(plistPath))
            {
                UnityEngine.Debug.LogWarning("[IOSExportCompliancePostProcessor] Info.plist not found. Could not set ITSAppUsesNonExemptEncryption.");
                return;
            }

            // Read the plist file
            var plist = new PlistDocument();
            plist.ReadFromFile(plistPath);

            // Set the export compliance flag to false
            plist.root.SetBoolean("ITSAppUsesNonExemptEncryption", false);

            // Save the modifications
            plist.WriteToFile(plistPath);
            
            UnityEngine.Debug.Log("[IOSExportCompliancePostProcessor] Successfully appended ITSAppUsesNonExemptEncryption=false to Info.plist.");
        }
    }
}
#endif
