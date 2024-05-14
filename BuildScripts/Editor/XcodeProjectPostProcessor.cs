#if UNITY_IOS
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEditor.iOS.Xcode;
using UnityEditor.iOS.Xcode.Extensions;
using UnityEngine;

namespace BuildScripts.Editor
{
    public class XcodeProjectPostProcessor
    {
        private const string TargetUnityIphonePodfileLine = "target 'Unity-iPhone' do";
        private const string EntitlementFilename          = "Production.entitlements";

        private static readonly List<string> DynamicLibrariesToEmbed = new List<string>
        {
            "DTBiOSSDK.xcframework",
            "IASDKCore.xcframework",
            "OMSDK_Ogury.xcframework",
#if IRONSOURCE
            "AppLovinSDK.xcframework"
#endif
        };

        private static readonly string[] ADNetworkIdentifiers =
        {
            "su67r6k2v3.skadnetwork", "cstr6suwn9.skadnetwork", "bvpn9ufa9b.skadnetwork", "mlmmfzh3r3.skadnetwork", "w9q455wk68.skadnetwork", "v9wttpbfk9.skadnetwork", "n38lu8286q.skadnetwork",
            "c6k4g5qg8m.skadnetwork", "f73kdq92p3.skadnetwork", "yclnxrl5pm.skadnetwork", "hs6bdukanm.skadnetwork", "44jx6755aq.skadnetwork", "lr83yxwka7.skadnetwork", "5a6flpkh64.skadnetwork",
            "7ug5zh24hu.skadnetwork", "tl55sbb4fm.skadnetwork", "5tjdwbrq8w.skadnetwork", "238da6jt44.skadnetwork", "488r3q3dtq.skadnetwork", "v79kvwwj4g.skadnetwork", "2u9pt9hc89.skadnetwork",
            "3rd42ekr43.skadnetwork", "f38h382jlk.skadnetwork", "g28c52eehv.skadnetwork", "9t245vhmpl.skadnetwork", "ppxm28t8ap.skadnetwork", "prcb7njmu6.skadnetwork", "av6w8kgt66.skadnetwork",
            "3sh42y64q3.skadnetwork", "578prtvx9j.skadnetwork", "4dzt52r2t5.skadnetwork", "4fzdc2evr5.skadnetwork", "m8dbw4sv7c.skadnetwork", "wg4vff78zm.skadnetwork", "5lm9lj6jb7.skadnetwork",
            "4468km3ulz.skadnetwork", "9rd848q2bz.skadnetwork", "glqzh8vgby.skadnetwork", "44n7hlldy6.skadnetwork", "424m5254lk.skadnetwork", "4pfyvq9l8r.skadnetwork", "wzmmz9fp6w.skadnetwork",
            "8s468mfl3y.skadnetwork", "22mmun2rn5.skadnetwork", "zmvfpc5aq8.skadnetwork", "t38b2kh725.skadnetwork", "v72qych5uu.skadnetwork", "ydx93a7ass.skadnetwork", "24t9a8vw3c.skadnetwork",
            "kbd757ywx3.skadnetwork", "3qy4746246.skadnetwork", "s39g8k73mm.skadnetwork", "2fnua5tdw4.skadnetwork", "3qcr597p9d.skadnetwork", "7rz58n8ntl.skadnetwork", "32z4fx6l9h.skadnetwork",
            "f7s53z58qe.skadnetwork", "6g9af3uyq4.skadnetwork", "cg4yq2srnc.skadnetwork", "9nlqeag3gk.skadnetwork", "u679fj5vs4.skadnetwork", "rx5hdcabgc.skadnetwork", "ejvt5qm6ak.skadnetwork",
            "275upjj5gd.skadnetwork", "klf5c3l5u5.skadnetwork", "uw77j35x4d.skadnetwork", "mtkv5xtk9e.skadnetwork", "mp6xlyr22a.skadnetwork"
        };

//         [PostProcessBuild(int.MaxValue)]
//         public static void OnPostProcessBuild(BuildTarget buildTarget, string path)
//         {
//             Debug.Log("onelog: Process Info.plist");
//             var plistPath = Path.Combine(path, "Info.plist");
//             var plist     = new PlistDocument();
//             plist.ReadFromFile(plistPath);
// #if APPSFLYER
//             plist.root.SetString("NSAdvertisingAttributionReportEndpoint", "https://appsflyer-skadnetwork.com");
// #elif ADJUST
// 		plist.root.SetString("NSAdvertisingAttributionReportEndpoint", "https://adjust-skadnetwork.com");
// #endif
//             plist.root.SetBoolean("ITSAppUsesNonExemptEncryption", false);
//
//             Debug.Log("Setup SKAdNetwork items");
//             PlistElementArray networkItems = null;
//
//             networkItems = plist.root["SKAdNetworkItems"] == null ? plist.root.CreateArray("SKAdNetworkItems") : plist.root["SKAdNetworkItems"].AsArray();
//
//             var existSkAdNetworks = new HashSet<string>();
//             foreach (var plistElement in networkItems.values)
//             {
//                 var networkItem = plistElement.AsDict();
//                 var skAdNetwork = networkItem["SKAdNetworkIdentifier"].AsString();
//                 if (existSkAdNetworks.Add(skAdNetwork))
//                 {
//                     Debug.Log("Exist sk ad network: " + skAdNetwork);
//                 }
//             }
//
//             foreach (var identifier in ADNetworkIdentifiers)
//             {
//                 if (!existSkAdNetworks.Add(identifier)) continue;
//                 var networkItemDict = networkItems.AddDict();
//                 Debug.Log("Add sk ad network: " + identifier);
//                 networkItemDict.SetString("SKAdNetworkIdentifier", identifier);
//             }
//
//             plist.root.SetString("NSUserTrackingUsageDescription", "This identifier will be used to personalized your advertising experience.");
//
//             File.WriteAllText(plistPath, plist.WriteToString());
//
//             Debug.Log("onelog:Process xcode project");
//             var projPath = PBXProject.GetPBXProjectPath(path);
//             var project  = new PBXProject();
//             project.ReadFromFile(projPath);
//             var mainTargetGuid     = project.GetUnityMainTargetGuid();
//             var unityFrameworkGuid = project.GetUnityFrameworkTargetGuid();
//             project.SetBuildProperty(mainTargetGuid, "GENERATE_INFOPLIST_FILE", "NO");
//             project.SetBuildProperty(mainTargetGuid, "ENABLE_BITCODE", "NO");
//             project.SetBuildProperty(unityFrameworkGuid, "ENABLE_BITCODE", "NO");
//             project.SetBuildProperty(mainTargetGuid, "ALWAYS_EMBED_SWIFT_STANDARD_LIBRARIES", "YES");
//             project.SetBuildProperty(unityFrameworkGuid, "ALWAYS_EMBED_SWIFT_STANDARD_LIBRARIES", "NO");
//             EmbedDynamicLibrariesIfNeeded(path, project, mainTargetGuid);
//
//             project.WriteToFile(projPath);
//
//             var capacilities = new ProjectCapabilityManager(projPath, EntitlementFilename, null, mainTargetGuid);
//             capacilities.AddBackgroundModes(BackgroundModesOptions.RemoteNotifications);
//             capacilities.AddPushNotifications(false);
//             capacilities.WriteToFile();
//         }

        private static void EmbedDynamicLibrariesIfNeeded(string buildPath, PBXProject project, string targetGuid)
        {
            // Check that the Pods directory exists (it might not if a publisher is building with Generate Podfile setting disabled in EDM).
            Debug.Log("onelog: EmbedDynamicLibrariesIfNeeded");
            var podsDirectory = Path.Combine(buildPath, "Pods");
            if (!Directory.Exists(podsDirectory)) return;
            var dynamicLibraryPathsPresentInProject = new List<string>();
            foreach (var dynamicLibraryToSearch in DynamicLibrariesToEmbed)
            {
                // both .framework and .xcframework are directories, not files
                var directories = Directory.GetDirectories(podsDirectory, dynamicLibraryToSearch, SearchOption.AllDirectories);
                if (directories.Length <= 0) continue;

                var dynamicLibraryAbsolutePath = directories[0];
                var index                      = dynamicLibraryAbsolutePath.LastIndexOf("Pods", StringComparison.Ordinal);
                var relativePath               = dynamicLibraryAbsolutePath[index..];
                dynamicLibraryPathsPresentInProject.Add(relativePath);
            }

            if (dynamicLibraryPathsPresentInProject.Count <= 0) return;

            if (!ContainsUnityIphoneTargetInPodfile(buildPath))
            {
                foreach (var dynamicLibraryPath in dynamicLibraryPathsPresentInProject)
                {
                    var fileGuid = project.AddFile(dynamicLibraryPath, dynamicLibraryPath);
                    Debug.Log("onelog: Add library for search path: " + dynamicLibraryPath);
                    project.AddFileToEmbedFrameworks(targetGuid, fileGuid);
                }
            }
        }

        private static bool ContainsUnityIphoneTargetInPodfile(string buildPath)
        {
            var podfilePath = Path.Combine(buildPath, "Podfile");
            if (!File.Exists(podfilePath)) return false;

            var lines = File.ReadAllLines(podfilePath);
            return lines.Any(line => line.Contains(TargetUnityIphonePodfileLine));
        }
    }
}
#endif