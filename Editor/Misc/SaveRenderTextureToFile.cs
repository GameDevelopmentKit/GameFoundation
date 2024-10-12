using UnityEngine;

namespace GameFoundation.Scripts.Utilities
{
    using UnityEditor;

    public class SaveRenderTextureToFile
    {
        [MenuItem("Assets/Save RenderTexture to file")]
        public static void SaveRTToFile()
        {
            var rt = Selection.activeObject as RenderTexture;

            RenderTexture.active = rt;
            var tex = new Texture2D(rt.width, rt.height, TextureFormat.RGB24, false);
            tex.ReadPixels(new(0, 0, rt.width, rt.height), 0, 0);
            RenderTexture.active = null;

            byte[] bytes;
            bytes = tex.EncodeToPNG();

            var path = AssetDatabase.GetAssetPath(rt) + ".png";
            System.IO.File.WriteAllBytes(path, bytes);
            AssetDatabase.ImportAsset(path);
            Debug.Log("Saved to " + path);
        }

        [MenuItem("Assets/Save RenderTexture to file", true)]
        public static bool SaveRTToFileValidation()
        {
            return Selection.activeObject is RenderTexture;
        }
    }
}