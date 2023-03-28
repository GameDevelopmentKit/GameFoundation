#if UNITY_IOS ||UNITY_IPHONE
namespace BuildScripts.Editor
{
    using System.Collections.Generic;
    using System.IO;
    using UnityEditor;
    using UnityEditor.Callbacks;
    using UnityEditor.iOS.Xcode;

    public class IOSPostProcessingBuildTool
    {
        #region SKNetworks

        private static readonly List<string> SkNetworks = new()
        {
            "su67r6k2v3", "f7s53z58qe", "2u9pt9hc89", "hs6bdukanm", "8s468mfl3y", "c6k4g5qg8m", "v72qych5uu", "44jx6755aq", "prcb7njmu6", "m8dbw4sv7c", "3rd42ekr43", "4fzdc2evr5", "t38b2kh725",
            "f38h382jlk", "424m5254lk", "ppxm28t8ap", "av6w8kgt66", "cp8zw746q7", "4468km3ulz", "e5fvkxwrpn", "22mmun2rn5", "s39g8k73mm", "yclnxrl5pm", "3qy4746246", "k674qkevps", "kbmxgpxpgc",
            "9nlqeag3gk", "32z4fx6l9h", "252b5q8x7y", "kbd757ywx3", "4pfyvq9l8r", "tl55sbb4fm", "mlmmfzh3r3", "klf5c3l5u5", "9t245vhmpl", "9rd848q2bz", "7ug5zh24hu", "7rz58n8ntl", "ejvt5qm6ak",
            "5lm9lj6jb7", "mtkv5xtk9e", "6g9af3uyq4", "uw77j35x4d", "u679fj5vs4", "rx5hdcabgc", "g28c52eehv", "cg4yq2srnc", "275upjj5gd", "wg4vff78zm", "qqp299437r", "2fnua5tdw4", "3qcr597p9d",
            "3sh42y64q3", "5a6flpkh64", "cstr6suwn9", "n6fk4nfna4", "p78axxw29g", "wzmmz9fp6w", "ydx93a7ass", "zq492l623r", "24t9a8vw3c", "523jb4fst2", "54nzkqm89y", "578prtvx9j", "5l3tpt7t6e",
            "6xzpu9s2p8", "79pbpufp6p", "9b89h5y424", "cj5566h2ga", "feyaarzu9v", "ggvn48r87g", "glqzh8vgby", "gta9lk7p23", "ludvb6z3bs", "n9x2a789qt", "pwa73g5rt2", "xy9t38ct57", "zmvfpc5aq8",
            "294l99pt4k", "44n7hlldy6", "4dzt52r2t5", "4w7y6s5ca2", "5tjdwbrq8w", "6964rsfnh4", "6p4ks3rnbw", "737z793b9f", "74b6s63p6l", "84993kbrcf", "97r2b46745", "a7xqa6mtl2", "b9bk5wbcq9",
            "bxvub5ada5", "dzg6xy7pwj", "f73kdq92p3", "g2y4y55b64", "hdw39hrw9y", "lr83yxwka7", "mls7yz5dvl", "mp6xlyr22a", "pwdxu55a5a", "r45fhb6rf7", "rvh3l7un93", "s69wq72ugq", "w9q455wk68",
            "x44k69ngh6", "x8uqf25wch", "y45688jllp", "238da6jt44", "x2jnk7ly8j", "v9wttpbfk9", "n38lu8286q", "9g2aggbj52", "wzmmZ9fp6w", "nu4557a4je", "v4nxqhlyqp", "24zw6aqk47", "cs644xg564",
            "9vvzujtq5s", "c3frkrj4fj", "a8cz6cu7e5", "r26jy69rpl", "3l6bd9hu43", "488r3q3dtq", "52fl2v3hgk", "6v7lgmsu45", "89z7zv988g", "8m87ys6875", "hb56zgv37p", "m297p6643m", "m5mvw97r93",
            "vcra2ehyfk", "9yg77x724h", "ecpz2srf59", "gvmwg8q7h5", "n66cz3y3bx", "nzq8sh4pbs", "pu4na253f3", "v79kvwwj4g", "yrqqpx2mcb", "z4gj7hsk7h", "7953jerfzd"
        };

        #endregion

        #region Main Process

        [PostProcessBuild]
        public static void ChangeXcodePlist(BuildTarget buildTarget, string pathToBuiltProject)
        {
            if (buildTarget != BuildTarget.iOS) return;

            var plistPath = pathToBuiltProject + "/Info.plist";
            var plist     = new PlistDocument();
            plist.ReadFromString(File.ReadAllText(plistPath));
            var rootDict = plist.root;

            SetPlistConfig(rootDict);

            // Write to file
            File.WriteAllText(plistPath, plist.WriteToString());

            //Setting project
            SettingProjectIOS(buildTarget, pathToBuiltProject);
        }

        private static void SettingProjectIOS(BuildTarget buildTarget, string pathToBuiltProject)
        {
            var projectPath = pathToBuiltProject + "/Unity-iPhone.xcodeproj/project.pbxproj";

            var pbxProject = new PBXProject();
            pbxProject.ReadFromString(File.ReadAllText(projectPath));

            var mainTargetGuid      = pbxProject.GetUnityMainTargetGuid();
            var testTargetGuid      = pbxProject.TargetGuidByName(PBXProject.GetUnityTestTargetName());
            var frameworkTargetGuid = pbxProject.GetUnityFrameworkTargetGuid();

            SetProjectConfig(pbxProject, mainTargetGuid, testTargetGuid, frameworkTargetGuid);

            File.WriteAllText(projectPath, pbxProject.WriteToString());
        }

        #endregion

        private static void SetPlistConfig(PlistElementDict rootDict)
        {
            // Disable Firebase screen view tracking
            rootDict.SetBoolean("FirebaseAutomaticScreenReportingEnabled", false);
            rootDict.SetBoolean("FirebaseAppStoreReceiptURLCheckEnabled", false);

            // add this to use google mobile ads (iron source mediation)
            rootDict.SetBoolean("GADIsAdManagerApp", true);

            // add NSUserTrackingUsageDescription for iOS 14
            rootDict.SetString("NSUserTrackingUsageDescription", "This identifier will be used to personalized your advertising experience.");
            rootDict.SetString("NSAdvertisingAttributionReportEndpoint", "https://postbacks-is.com");

            // add IOS 14 Network Support
            var array = rootDict.CreateArray("SKAdNetworkItems");

            foreach (var skNetwork in SkNetworks)
            {
                var item = array.AddDict();
                item.SetString("SKAdNetworkIdentifier", $"{skNetwork}.skadnetwork"); //ironSource
            }

            // allow insecure http IOS
            // try
            // {
            //     PlistElementDict dictNSAppTransportSecurity = (PlistElementDict)rootDict["NSAppTransportSecurity"];
            //     PlistElementDict dictNSExceptionDomains = dictNSAppTransportSecurity.CreateDict("NSExceptionDomains");
            //     PlistElementDict dictDomain = dictNSExceptionDomains.CreateDict("ip-api.com");
            //     dictDomain.SetBoolean("NSExceptionAllowsInsecureHTTPLoads", true);
            // }
            // catch (Exception e)
            // {
            //     Debug.Log("Add allow insecure http IOS has exception. " + e);
            // }
        }

        private static void SetProjectConfig(PBXProject pbxProject, string mainTargetGuid, string testTargetGuid, string frameworkTargetGuid)
        {
            pbxProject.SetBuildProperty(mainTargetGuid, "ENABLE_BITCODE", "NO");      // disable bitcode by default, reduce app size
            pbxProject.SetBuildProperty(testTargetGuid, "ENABLE_BITCODE", "NO");      // disable bitcode by default, reduce app size
            pbxProject.SetBuildProperty(frameworkTargetGuid, "ENABLE_BITCODE", "NO"); // disable bitcode by default, reduce app size

            pbxProject.AddCapability(mainTargetGuid, PBXCapabilityType.PushNotifications);  // turn on push notification
            pbxProject.AddCapability(mainTargetGuid, PBXCapabilityType.InAppPurchase);      // turn on IAP IOS
            pbxProject.AddFrameworkToProject(mainTargetGuid, "iAd.framework", false);       // for Appsflyer tracking search ads
            pbxProject.AddFrameworkToProject(mainTargetGuid, "AdSupport.framework", false); // Add framework for (iron source mediation)

            pbxProject.AddBuildProperty(mainTargetGuid, "OTHER_LDFLAGS", "-lxml2"); // Add '-lxml2' of facebook to "Other Linker Flags"
            pbxProject.SetBuildProperty(mainTargetGuid, "ARCHS", "arm64");
        }
    }
}
#endif