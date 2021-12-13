#if UNITY_EDITOR
using System.Text;
using UnityEditor;

namespace DA_Assets
{
    public class HandleDefine
    {
        /// <summary>
        /// Adding define symbol to /Project Settings/Scripting Define Symbols.
        /// </summary>
        public static void AddDefine(string define)
        {
            bool exists = IsDefineExists(define);
            if (exists == false)
            {
                InstallDefine(define);
            }
        }
        /// <summary>
        /// Remove define symbol from /Project Settings/Scripting Define Symbols.
        /// </summary>
        public static void RemoveDefine(string define)
        {
            bool exists = IsDefineExists(define);

            if (exists)
            {
                BuildTargetGroup targetGroup = EditorUserBuildSettings.selectedBuildTargetGroup;
                BuildTargetGroup selectedBuildTargetGroup = EditorUserBuildSettings.selectedBuildTargetGroup;
                string scriptingDefineSymbols = PlayerSettings.GetScriptingDefineSymbolsForGroup(selectedBuildTargetGroup);
                scriptingDefineSymbols = scriptingDefineSymbols.Replace(define, "");
                PlayerSettings.SetScriptingDefineSymbolsForGroup(targetGroup, scriptingDefineSymbols);
            }
        }
        /// <summary>
        /// Check is define symbol exists in /Project Settings/Scripting Define Symbols.
        /// </summary>
        public static bool IsDefineExists(string define)
        {
            string defines = PlayerSettings.GetScriptingDefineSymbolsForGroup(EditorUserBuildSettings.selectedBuildTargetGroup);
            bool exists = defines.Contains(define);
            return exists;
        }
        private static void InstallDefine(string def)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append(PlayerSettings.GetScriptingDefineSymbolsForGroup(EditorUserBuildSettings.selectedBuildTargetGroup));

            if (sb.Length == 0)
            {
                sb.Append(def + ";");
            }
            else
            {
                sb.Append($"{(sb[sb.Length - 1] == ';' ? "" : ";")}{def}");
            }

            PlayerSettings.SetScriptingDefineSymbolsForGroup(EditorUserBuildSettings.selectedBuildTargetGroup, sb.ToString());
        }
    }
}
#endif