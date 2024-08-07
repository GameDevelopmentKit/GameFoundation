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
            "22mmun2rn5","2u9pt9hc89","3qy4746246","3rd42ekr43","3sh42y64q3","4468km3ulz","44jx6755aq","4fzdc2evr5","523jb4fst2","5a6flpkh64","5l3tpt7t6e","5lm9lj6jb7","737z793b9f","7953jerfzd","7rz58n8ntl","7ug5zh24hu","8s468mfl3y","97r2b46745","9rd848q2bz","9t245vhmpl","av6w8kgt66","bvpn9ufa9b","c6k4g5qg8m","cg4yq2srnc","cj5566h2ga","f38h382jlk","f73kdq92p3","ggvn48r87g","gvmwg8q7h5","hs6bdukanm","kbd757ywx3","klf5c3l5u5","m8dbw4sv7c","mlmmfzh3r3","mls7yz5dvl","mtkv5xtk9e","n66cz3y3bx","n9x2a789qt","nzq8sh4pbs","p78axxw29g","ppxm28t8ap","prcb7njmu6","pu4na253f3","t38b2kh725","u679fj5vs4","uw77j35x4d","v72qych5uu","w9q455wk68","wzmmz9fp6w","xy9t38ct57","yclnxrl5pm","z4gj7hsk7h","wg4vff78zm","ydx93a7ass","4pfyvq9l8r","ejvt5qm6ak","578prtvx9j","8m87ys6875","488r3q3dtq","tl55sbb4fm","zmvfpc5aq8","6xzpu9s2p8","a8cz6cu7e5","glqzh8vgby","feyaarzu9v","424m5254lk","s39g8k73mm","33r6p7g8nc","g28c52eehv","52fl2v3hgk","pwa73g5rt2","9nlqeag3gk","24t9a8vw3c","gta9lk7p23","5tjdwbrq8w","6g9af3uyq4","275upjj5gd","rx5hdcabgc","x44k69ngh6","2fnua5tdw4","g69uk9uh2b","zq492l623r","9b89h5y424","bxvub5ada5","cstr6suwn9","54nzkqm89y","32z4fx6l9h","79pbpufp6p","kbmxgpxpgc","rvh3l7un93","qqp299437r","294l99pt4k","74b6s63p6l","b9bk5wbcq9","44n7hlldy6","6p4ks3rnbw","g2y4y55b64","ludvb6z3bs","su67r6k2v3","n6fk4nfna4","e5fvkxwrpn","r45fhb6rf7","c3frkrj4fj","6rd35atwn8","z959bm4gru","7fmhfwg9en","ln5gz23vtd","k674qkevps","qwpu75vrh2","238da6jt44","24zw6aqk47","252b5q8x7y","2q884k2j68","2rq3zucswp","2tdux39lx8","3cgn6rq224","3l6bd9hu43","3qcr597p9d","47vhws6wlr","4dzt52r2t5","4mn522wn87","4w7y6s5ca2","5ghnmfs3dh","5mv394q32t","627r9wr2y5","633vhxswh4","67369282zy","6964rsfnh4","6qx585k4p6","6v7lgmsu45","6yxyv74ff7","79w64w269u","7fbxrn65az","7k3cvf297u","7tnzynbdc7","84993kbrcf","866k9ut3g3","88k8774x49","899vrgt9g8","89z7zv988g","8c4e2ghe7u","8qiegk9qfv","8r8llnkz5a","8w3np9l82g","9g2aggbj52","9vvzujtq5s","9wsyqb3ku7","9yg77x724h","a2p9lx4jpn","a7xqa6mtl2","au67k4efj4","axh5283zss","b55w3d8y8z","c7g47wypnu","cad8qz2s3j","cp8zw746q7","cs644xg564","d7g9azk84q","dbu4b84rxf","dd3a75yxkv","dkc879ngq3","dmv22haz9p","dn942472g5","dr774724x4","dticjx1a9i","dzg6xy7pwj","ecpz2srf59","eh6m2bh4zr","eqhxz8m8av","f7s53z58qe","fkak3gfpt6","fz2k2k5tej","gfat3222tu","gta8lk7p23","h5jmj969g5","h65wbv5k3f","h8vml93bkz","hb56zgv37p","hdw39hrw9y","hjevpa356n","jb7bn6koa5","krvm3zuq6h","l6nv3x923s","l93v5h6a4m","lr83yxwka7","m297p6643m","m5mvw97r93","mj797d8u6f","mp6xlyr22a","n38lu8286q","nfqy3847ph","nrt9jy4kw9","nu4557a4je","pd25vrrwzn","pwdxu55a5a","qlbq5gtkt8","qu637u8glc","r26jy69rpl","s69wq72ugq","sczv5946wb","t3b3f7n3x8","t6d3zquu66","t7ky8fmwkd","tmhh9296z4","uzqba5354d","v4nxqhlyqp","v7896pgt74","v79kvwwj4g","v9wttpbfk9","vc83br9sjg","vcra2ehyfk","vutu7akeur","w28pnjg2k4","x2jnk7ly8j","x5854y7y24","x5l83yy675","x8jxxk4ff5","x8uqf25wch","xmn954pzmp","xx9sdjej2w","y45688jllp","y5ghdn5j9k","y755zyxw56","yrqqpx2mcb","z5b3gh5ugf","zh3b7bxvad"
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