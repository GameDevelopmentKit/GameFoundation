using UnityEditor;
using UnityEngine;

namespace DarkTonic.MasterAudio.EditorScripts
{
    // ReSharper disable once CheckNamespace
    public static class MasterAudioInspectorResources
    {
        public const string PrefabFolderPartialPath = "/DarkTonic/MasterAudio/Prefabs/";
        public const string MasterAudioFolderPath = "MasterAudio";

        public static Texture LogoTexture = EditorGUIUtility.LoadRequired(string.Format("{0}/inspector_header_master_audio.png", MasterAudioFolderPath)) as Texture;
        public static Texture BAILogoTexture = EditorGUIUtility.LoadRequired(string.Format("{0}/inspector_header_bulk_audio_importer.png", MasterAudioFolderPath)) as Texture;
        public static Texture DeleteTexture = EditorGUIUtility.LoadRequired(string.Format("{0}/deleteIcon.png", MasterAudioFolderPath)) as Texture;
        public static Texture GearTexture = EditorGUIUtility.LoadRequired(string.Format("{0}/gearIcon.png", MasterAudioFolderPath)) as Texture;
        public static Texture MuteOffTexture = EditorGUIUtility.LoadRequired(string.Format("{0}/muteOff.png", MasterAudioFolderPath)) as Texture;
        public static Texture MuteOnTexture = EditorGUIUtility.LoadRequired(string.Format("{0}/muteOn.png", MasterAudioFolderPath)) as Texture;
        public static Texture NextTrackTexture = EditorGUIUtility.LoadRequired(string.Format("{0}/nextTrackIcon.png", MasterAudioFolderPath)) as Texture;
        public static Texture PauseTexture = EditorGUIUtility.LoadRequired(string.Format("{0}/pauseIcon.png", MasterAudioFolderPath)) as Texture;
        public static Texture PauseOnTexture = EditorGUIUtility.LoadRequired(string.Format("{0}/pauseIconOn.png", MasterAudioFolderPath)) as Texture;
        public static Texture PlaySongTexture = EditorGUIUtility.LoadRequired(string.Format("{0}/playIcon.png", MasterAudioFolderPath)) as Texture;
        public static Texture PreviousTrackTexture = EditorGUIUtility.LoadRequired(string.Format("{0}/prevTrackIcon.png", MasterAudioFolderPath)) as Texture;
        public static Texture RandomTrackTexture = EditorGUIUtility.LoadRequired(string.Format("{0}/randomIcon.png", MasterAudioFolderPath)) as Texture;
        public static Texture SoloOffTexture = EditorGUIUtility.LoadRequired(string.Format("{0}/soloOff.png", MasterAudioFolderPath)) as Texture;
        public static Texture SoloOnTexture = EditorGUIUtility.LoadRequired(string.Format("{0}/soloOn.png", MasterAudioFolderPath)) as Texture;
        public static Texture PreviewTexture = EditorGUIUtility.LoadRequired(string.Format("{0}/speakerIcon.png", MasterAudioFolderPath)) as Texture;
        public static Texture StopTexture = EditorGUIUtility.LoadRequired(string.Format("{0}/stopIcon.png", MasterAudioFolderPath)) as Texture;
        public static Texture CopyTexture = EditorGUIUtility.LoadRequired(string.Format("{0}/copyIcon.png", MasterAudioFolderPath)) as Texture;
        public static Texture FindTexture = EditorGUIUtility.LoadRequired(string.Format("{0}/find.png", MasterAudioFolderPath)) as Texture;
        public static Texture ReadyTexture = EditorGUIUtility.LoadRequired(string.Format("{0}/ready.png", MasterAudioFolderPath)) as Texture;
        public static Texture UpArrowTexture = EditorGUIUtility.LoadRequired(string.Format("{0}/arrow_up.png", MasterAudioFolderPath)) as Texture;
        public static Texture DownArrowTexture = EditorGUIUtility.LoadRequired(string.Format("{0}/arrow_down.png", MasterAudioFolderPath)) as Texture;
        public static Texture CancelTexture = EditorGUIUtility.LoadRequired(string.Format("{0}/cancel.png", MasterAudioFolderPath)) as Texture;
        public static Texture SaveTexture = EditorGUIUtility.LoadRequired(string.Format("{0}/save.png", MasterAudioFolderPath)) as Texture;
        public static Texture HelpTexture = EditorGUIUtility.LoadRequired(string.Format("{0}/helpIcon.png", MasterAudioFolderPath)) as Texture;


        public static Texture[] LedTextures = {
            EditorGUIUtility.LoadRequired(string.Format("{0}/LED5.png", MasterAudioFolderPath)) as Texture,
            EditorGUIUtility.LoadRequired(string.Format("{0}/LED4.png", MasterAudioFolderPath)) as Texture,
            EditorGUIUtility.LoadRequired(string.Format("{0}/LED3.png", MasterAudioFolderPath)) as Texture,
            EditorGUIUtility.LoadRequired(string.Format("{0}/LED2.png", MasterAudioFolderPath)) as Texture,
            EditorGUIUtility.LoadRequired(string.Format("{0}/LED1.png", MasterAudioFolderPath)) as Texture,
            EditorGUIUtility.LoadRequired(string.Format("{0}/LED0.png", MasterAudioFolderPath)) as Texture
        };
    }
}