#if UNITY_IOS
namespace BuildScripts.Editor
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using GameFoundation.BuildScripts.Runtime;
    using UnityEditor;
    using UnityEditor.Callbacks;
    using UnityEditor.iOS.Xcode;
    using UnityEngine;

    public class IOSPostProcessingBuildTool
    {
        #region SKNetworks

        private static readonly List<string> SkNetworks = new()
        {
            "22mmun2rn5", "238da6jt44", "24t9a8vw3c", "24zw6aqk47", "252b5q8x7y", "275upjj5gd", "294l99pt4k", "2fnua5tdw4", "2q884k2j68", "2rq3zucswp", "2tdux39lx8", "2u9pt9hc89", "32z4fx6l9h",
            "33r6p7g8nc", "3cgn6rq224", "3l6bd9hu43", "3qcr597p9d", "3qy4746246", "3rd42ekr43", "3sh42y64q3", "424m5254lk", "4468km3ulz", "44jx6755aq", "44n7hlldy6", "47vhws6wlr", "488r3q3dtq",
            "4dzt52r2t5", "4fzdc2evr5", "4mn522wn87", "4pfyvq9l8r", "4w7y6s5ca2", "523jb4fst2", "52fl2v3hgk", "54nzkqm89y", "578prtvx9j", "5a6flpkh64", "5ghnmfs3dh", "5l3tpt7t6e", "5lm9lj6jb7",
            "5mv394q32t", "5tjdwbrq8w", "627r9wr2y5", "633vhxswh4", "67369282zy", "6964rsfnh4", "6g9af3uyq4", "6p4ks3rnbw", "6qx585k4p6", "6v7lgmsu45", "6xzpu9s2p8", "6yxyv74ff7", "737z793b9f",
            "74b6s63p6l", "7953jerfzd", "79pbpufp6p", "79w64w269u", "7fbxrn65az", "7fmhfwg9en", "7k3cvf297u", "7rz58n8ntl", "7tnzynbdc7", "7ug5zh24hu", "84993kbrcf", "866k9ut3g3", "88k8774x49",
            "899vrgt9g8", "89z7zv988g", "8c4e2ghe7u", "8m87ys6875", "8qiegk9qfv", "8r8llnkz5a", "8s468mfl3y", "8w3np9l82g", "97r2b46745", "9b89h5y424", "9g2aggbj52", "9nlqeag3gk", "9rd848q2bz",
            "9t245vhmpl", "9vvzujtq5s", "9wsyqb3ku7", "9yg77x724h", "a2p9lx4jpn", "a7xqa6mtl2", "a8cz6cu7e5", "au67k4efj4", "av6w8kgt66", "axh5283zss", "b55w3d8y8z", "b9bk5wbcq9", "bvpn9ufa9b",
            "bxvub5ada5", "c3frkrj4fj", "c6k4g5qg8m", "c7g47wypnu", "cad8qz2s3j", "cg4yq2srnc", "cj5566h2ga", "cp8zw746q7", "cs644xg564", "cstr6suwn9", "d7g9azk84q", "dbu4b84rxf", "dd3a75yxkv",
            "dkc879ngq3", "dmv22haz9p", "dn942472g5", "dr774724x4", "dticjx1a9i", "dzg6xy7pwj", "e5fvkxwrpn", "ecpz2srf59", "eh6m2bh4zr", "ejvt5qm6ak", "eqhxz8m8av", "f38h382jlk", "f73kdq92p3",
            "f7s53z58qe", "feyaarzu9v", "fkak3gfpt6", "fz2k2k5tej", "g28c52eehv", "g2y4y55b64", "g69uk9uh2b", "gfat3222tu", "ggvn48r87g", "glqzh8vgby", "gta8lk7p23", "gta9lk7p23", "gvmwg8q7h5",
            "h5jmj969g5", "h65wbv5k3f", "h8vml93bkz", "hb56zgv37p", "hdw39hrw9y", "hjevpa356n", "hs6bdukanm", "jb7bn6koa5", "k674qkevps", "kbd757ywx3", "kbmxgpxpgc", "klf5c3l5u5", "krvm3zuq6h",
            "l6nv3x923s", "l93v5h6a4m", "ln5gz23vtd", "lr83yxwka7", "ludvb6z3bs", "m297p6643m", "m5mvw97r93", "m8dbw4sv7c", "mj797d8u6f", "mlmmfzh3r3", "mls7yz5dvl", "mp6xlyr22a", "mtkv5xtk9e",
            "n38lu8286q", "n66cz3y3bx", "n6fk4nfna4", "n9x2a789qt", "nfqy3847ph", "nrt9jy4kw9", "nu4557a4je", "nzq8sh4pbs", "p78axxw29g", "pd25vrrwzn", "ppxm28t8ap", "prcb7njmu6", "pu4na253f3",
            "pwa73g5rt2", "pwdxu55a5a", "qlbq5gtkt8", "qqp299437r", "qu637u8glc", "r26jy69rpl", "r45fhb6rf7", "rvh3l7un93", "rx5hdcabgc", "s39g8k73mm", "s69wq72ugq", "sczv5946wb", "su67r6k2v3",
            "t38b2kh725", "t3b3f7n3x8", "t6d3zquu66", "t7ky8fmwkd", "tl55sbb4fm", "tmhh9296z4", "u679fj5vs4", "uw77j35x4d", "uzqba5354d", "v4nxqhlyqp", "v72qych5uu", "v7896pgt74", "v79kvwwj4g",
            "v9wttpbfk9", "vc83br9sjg", "vcra2ehyfk", "vutu7akeur", "w28pnjg2k4", "w9q455wk68", "wg4vff78zm", "wzmmz9fp6w", "x2jnk7ly8j", "x44k69ngh6", "x5854y7y24", "x5l83yy675", "x8jxxk4ff5",
            "x8uqf25wch", "xmn954pzmp", "xx9sdjej2w", "xy9t38ct57", "y45688jllp", "y5ghdn5j9k", "y755zyxw56", "yclnxrl5pm", "ydx93a7ass", "yrqqpx2mcb", "z4gj7hsk7h", "z5b3gh5ugf", "z959bm4gru",
            "zh3b7bxvad", "zmvfpc5aq8", "zq492l623r"
        };

        #endregion

        [PostProcessBuild(int.MaxValue)]
        public static void OnPostProcessBuild(BuildTarget buildTarget, string pathToBuiltProject)
        {
            try
            {
                SetPlistConfig(pathToBuiltProject);
                SetProjectConfig(pathToBuiltProject);

                Debug.Log("onelog: IOSPostProcessingBuildTool OnPostProcessBuild Success");
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                throw;
            }
        }

        #region Main

        private static void SetProjectConfig(string pathToBuiltProject)
        {
            var projectPath = pathToBuiltProject + "/Unity-iPhone.xcodeproj/project.pbxproj";

            var pbxProject = new PBXProject();
            pbxProject.ReadFromString(File.ReadAllText(projectPath));

            var mainTargetGuid           = pbxProject.GetUnityMainTargetGuid();
            var testTargetGuid           = pbxProject.TargetGuidByName(PBXProject.GetUnityTestTargetName());
            var unityFrameworkTargetGuid = pbxProject.GetUnityFrameworkTargetGuid();
            var projectGuid              = pbxProject.ProjectGuid();
            var pbxProjectPath           = PBXProject.GetPBXProjectPath(pathToBuiltProject);

            SetProjectConfig(pbxProject, mainTargetGuid, testTargetGuid, unityFrameworkTargetGuid, projectGuid);
            SetCapability(pbxProjectPath, mainTargetGuid);

            File.WriteAllText(projectPath, pbxProject.WriteToString());
            Debug.Log("onelog: IOSPostProcessingBuildTool SetProjectConfig Success");
        }

        private static void SetPlistConfig(string pathToBuiltProject)
        {
            var plistPath = pathToBuiltProject + "/Info.plist";
            var plist     = new PlistDocument();
            plist.ReadFromString(File.ReadAllText(plistPath));
            var rootDict = plist.root;

            // Disable Firebase screen view tracking
            rootDict.SetBoolean("FirebaseAutomaticScreenReportingEnabled", false);
            rootDict.SetBoolean("FirebaseAppStoreReceiptURLCheckEnabled", false);

            // add this to use google mobile ads (iron source mediation)
            rootDict.SetBoolean("GADIsAdManagerApp", true);

            // add NSUserTrackingUsageDescription for iOS 14
            rootDict.SetString("NSUserTrackingUsageDescription", "This identifier will be used to personalized your advertising experience.");

#if APPSFLYER
		    rootDict.SetString("NSAdvertisingAttributionReportEndpoint", "https://appsflyer-skadnetwork.com");
#elif ADJUST
		    rootDict.SetString("NSAdvertisingAttributionReportEndpoint", "https://adjust-skadnetwork.com");
#elif IRONSOURCE
            rootDict.SetString("NSAdvertisingAttributionReportEndpoint", "https://postbacks-is.com");
#endif

            // add IOS 14 Network Support
            var array = rootDict.CreateArray("SKAdNetworkItems");

            foreach (var skNetwork in SkNetworks)
            {
                var item = array.AddDict();
                item.SetString("SKAdNetworkIdentifier", $"{skNetwork}.skadnetwork"); //ironSource
            }

            // bypass Provide Export Compliance in Appstore Connect
            rootDict.SetBoolean("ITSAppUsesNonExemptEncryption", false);
            
            // set build version
            rootDict.SetString("CFBundleVersion",GameVersion.BuildNumber.ToString());

            // allow insecure http IOS
#if ALLOW_INSECURE_HTTP_LOAD
            try
            {
                PlistElementDict dictNSAppTransportSecurity = (PlistElementDict)rootDict["NSAppTransportSecurity"];
                PlistElementDict dictNSExceptionDomains = dictNSAppTransportSecurity.CreateDict("NSExceptionDomains");
                PlistElementDict dictDomain = dictNSExceptionDomains.CreateDict("ip-api.com");
                dictDomain.SetBoolean("NSExceptionAllowsInsecureHTTPLoads", true);
            }
            catch (Exception e)
            {
                Debug.Log("Add allow insecure http IOS has exception. " + e);
            }
#endif
            
            // URL Scheme
            var urlTypeArray = rootDict.CreateArray("CFBundleURLTypes");
            var urlTypeSubDict = urlTypeArray.AddDict();
            var urlSchemeArray = urlTypeSubDict.CreateArray("CFBundleURLSchemes");
            urlTypeSubDict.SetString("CFBundleURLName", PlayerSettings.applicationIdentifier);
            urlSchemeArray.AddString(PlayerSettings.applicationIdentifier);
            
            // Write to file
            File.WriteAllText(plistPath, plist.WriteToString());
            Debug.Log($"onelog: IOSPostProcessingBuildTool End SetPlistConfig");
        }

        #endregion

        #region Set Project Config function

        private static void SetProjectConfig(PBXProject pbxProject, string mainTargetGuid, string testTargetGuid, string frameworkTargetGuid, string projectGuid)
        {
            // disable bitcode by default, reduce app size
            pbxProject.SetBuildProperty(mainTargetGuid, "ENABLE_BITCODE", "NO");
            pbxProject.SetBuildProperty(testTargetGuid, "ENABLE_BITCODE", "NO");
            pbxProject.SetBuildProperty(frameworkTargetGuid, "ENABLE_BITCODE", "NO");
            pbxProject.SetBuildProperty(projectGuid, "ENABLE_BITCODE", "NO");

            pbxProject.AddFrameworkToProject(mainTargetGuid, "iAd.framework", false);       // for Appsflyer tracking search ads
            pbxProject.AddFrameworkToProject(mainTargetGuid, "AdSupport.framework", false); // Add framework for (iron source mediation)

            pbxProject.AddBuildProperty(mainTargetGuid, "OTHER_LDFLAGS", "-lxml2"); // Add '-lxml2' of facebook to "Other Linker Flags"
            pbxProject.SetBuildProperty(mainTargetGuid, "ARCHS", "arm64");

            // Disable Unity Framework Target
            pbxProject.SetBuildProperty(mainTargetGuid, "ALWAYS_EMBED_SWIFT_STANDARD_LIBRARIES", "NO");
            pbxProject.SetBuildProperty(testTargetGuid, "ALWAYS_EMBED_SWIFT_STANDARD_LIBRARIES", "NO");
            pbxProject.SetBuildProperty(frameworkTargetGuid, "ALWAYS_EMBED_SWIFT_STANDARD_LIBRARIES", "NO");
            pbxProject.SetBuildProperty(projectGuid, "ALWAYS_EMBED_SWIFT_STANDARD_LIBRARIES", "NO");
        }

        private static void SetCapability(string pbxProjectPath, string mainTargetGuid)
        {
            var projectCapabilityManager = new ProjectCapabilityManager(pbxProjectPath, "Production.entitlements", null, mainTargetGuid);
#if THEONE_SIGN_IN
            projectCapabilityManager.AddSignInWithApple();
#endif
#if THEONE_IAP
            projectCapabilityManager.AddInAppPurchase();
#endif
            projectCapabilityManager.AddBackgroundModes(BackgroundModesOptions.RemoteNotifications);
            projectCapabilityManager.AddPushNotifications(false);
            projectCapabilityManager.WriteToFile();
            Debug.Log("onelog:  OnPostProcessBuild SetCapability");
        }

        #endregion
    }
}
#endif