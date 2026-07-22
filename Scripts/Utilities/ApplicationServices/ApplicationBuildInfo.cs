namespace GameFoundation.Scripts.Utilities.ApplicationServices
{
    using System;
    using System.Runtime.InteropServices;
    using UnityEngine;
#if UNITY_EDITOR
    using UnityEditor;
#endif

    public enum ApplicationBuildPlatform
    {
        Android,
        Ios,
        WebGL,
        Editor,
        Other,
    }

    public class ApplicationBuildInfo
    {
        public ApplicationBuildPlatform Platform;
        public string                   Version;
        public string                   BuildNumber;
        public string                   PackageIdentifier;
    }

    public interface IApplicationBuildInfoProvider
    {
        ApplicationBuildInfo GetBuildInfo();
    }

    public class UnityApplicationBuildInfoProvider : IApplicationBuildInfoProvider
    {
        public ApplicationBuildInfo GetBuildInfo()
        {
            return new ApplicationBuildInfo
            {
                Platform          = GetCurrentPlatform(),
                Version           = Application.version,
                BuildNumber       = GetRuntimeBuildNumber(),
                PackageIdentifier = Application.identifier,
            };
        }

        private static ApplicationBuildPlatform GetCurrentPlatform()
        {
#if UNITY_EDITOR
            return ApplicationBuildPlatform.Editor;
#elif UNITY_ANDROID
            return ApplicationBuildPlatform.Android;
#elif UNITY_IOS
            return ApplicationBuildPlatform.Ios;
#elif UNITY_WEBGL
            return ApplicationBuildPlatform.WebGL;
#else
            return ApplicationBuildPlatform.Other;
#endif
        }

        public static string GetRuntimeBuildNumber()
        {
#if UNITY_EDITOR
            return GetEditorBuildNumber();
#elif UNITY_ANDROID
            return GetAndroidVersionCode();
#elif UNITY_IOS
            return GetIosBuildNumber();
#else
            return string.Empty;
#endif
        }

#if UNITY_EDITOR
        private static string GetEditorBuildNumber()
        {
            return EditorUserBuildSettings.activeBuildTarget switch
            {
                BuildTarget.Android => PlayerSettings.Android.bundleVersionCode.ToString(),
                BuildTarget.iOS     => PlayerSettings.iOS.buildNumber,
                _                   => string.Empty,
            };
        }
#endif

#if UNITY_ANDROID && !UNITY_EDITOR
        private static string GetAndroidVersionCode()
        {
            try
            {
                using (var unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
                using (var activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity"))
                using (var packageManager = activity.Call<AndroidJavaObject>("getPackageManager"))
                using (var packageInfo = packageManager.Call<AndroidJavaObject>("getPackageInfo", Application.identifier, 0))
                using (var version = new AndroidJavaClass("android.os.Build$VERSION"))
                {
                    var sdkInt = version.GetStatic<int>("SDK_INT");
                    return sdkInt >= 28
                        ? packageInfo.Call<long>("getLongVersionCode").ToString()
                        : packageInfo.Get<int>("versionCode").ToString();
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[ApplicationServices] Failed to read Android versionCode: {ex.Message}");
                return string.Empty;
            }
        }
#endif

#if UNITY_IOS && !UNITY_EDITOR
        [DllImport("__Internal")]
        private static extern IntPtr GFApplicationGetIOSBuildNumber();

        private static string GetIosBuildNumber()
        {
            try
            {
                var buildNumberPtr = GFApplicationGetIOSBuildNumber();
                return buildNumberPtr == IntPtr.Zero ? string.Empty : Marshal.PtrToStringAnsi(buildNumberPtr) ?? string.Empty;
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[ApplicationServices] Failed to read iOS CFBundleVersion: {ex.Message}");
                return string.Empty;
            }
        }
#endif
    }
}
