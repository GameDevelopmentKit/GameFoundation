/*
	PreviewLabs.PlayerPrefs

	Public Domain
	
	To the extent possible under law, PreviewLabs has waived all copyright and related or neighboring rights to this document. This work is published from: Belgium.
	
	http://www.previewlabs.com
	
*/

using System;
using System.Collections;
using System.IO;
using UnityEngine;

/*! \cond PRIVATE */

#if UNITY_WEBPLAYER || UNITY_WP8 || UNITY_METRO
	// can't compile this class
#else
// ReSharper disable once CheckNamespace
namespace DarkTonic.MasterAudio {
    public static class FilePlayerPrefs {
        private static readonly Hashtable PlayerPrefsHashtable = new Hashtable();

        private static bool _hashTableChanged;
        private static string _serializedOutput = "";
        private static readonly string SerializedInput = "";

        private const string ParametersSeperator = ";";
        private const string KeyValueSeperator = ":";

        private static readonly string FileName = Application.persistentDataPath + "/MAPlayerPrefs.txt";

        static FilePlayerPrefs() {
            //load previous settings
            // ReSharper disable once JoinDeclarationAndInitializer
            StreamReader fileReader;

            if (!File.Exists(FileName))
            {
                return;
            }
            fileReader = new StreamReader(FileName);

            SerializedInput = fileReader.ReadLine();

            Deserialize();

            fileReader.Close();
        }

        public static bool HasKey(string key) {
            return PlayerPrefsHashtable.ContainsKey(key);
        }

        public static void SetString(string key, string value) {
            if (!PlayerPrefsHashtable.ContainsKey(key)) {
                PlayerPrefsHashtable.Add(key, value);
            } else {
                PlayerPrefsHashtable[key] = value;
            }

            _hashTableChanged = true;
        }

        public static void SetInt(string key, int value) {
            if (!PlayerPrefsHashtable.ContainsKey(key)) {
                PlayerPrefsHashtable.Add(key, value);
            } else {
                PlayerPrefsHashtable[key] = value;
            }

            _hashTableChanged = true;
        }

        public static void SetFloat(string key, float value) {
            if (!PlayerPrefsHashtable.ContainsKey(key)) {
                PlayerPrefsHashtable.Add(key, value);
            } else {
                PlayerPrefsHashtable[key] = value;
            }

            _hashTableChanged = true;
        }

        public static void SetBool(string key, bool value) {
            if (!PlayerPrefsHashtable.ContainsKey(key)) {
                PlayerPrefsHashtable.Add(key, value);
            } else {
                PlayerPrefsHashtable[key] = value;
            }

            _hashTableChanged = true;
        }

        public static string GetString(string key) {
            if (PlayerPrefsHashtable.ContainsKey(key)) {
                return PlayerPrefsHashtable[key].ToString();
            }

            return null;
        }

        public static string GetString(string key, string defaultValue) {
            if (PlayerPrefsHashtable.ContainsKey(key)) {
                return PlayerPrefsHashtable[key].ToString();
            } else {
                PlayerPrefsHashtable.Add(key, defaultValue);
                _hashTableChanged = true;
                return defaultValue;
            }
        }

        public static int GetInt(string key) {
            if (!PlayerPrefsHashtable.ContainsKey(key))
            {
                return 0;
            }
            var val = PlayerPrefsHashtable[key];
            if (val is int)
            {
                return (int) val;
            }
            var converted = int.Parse(val.ToString());
            PlayerPrefsHashtable[key] = converted;
            val = converted;
            // ReSharper disable once PossibleInvalidCastException
            return (int)val;
        }

        public static int GetInt(string key, int defaultValue) {
            if (PlayerPrefsHashtable.ContainsKey(key)) {
                return (int)PlayerPrefsHashtable[key];
            } else {
                PlayerPrefsHashtable.Add(key, defaultValue);
                _hashTableChanged = true;
                return defaultValue;
            }
        }

        public static float GetFloat(string key) {
            if (!PlayerPrefsHashtable.ContainsKey(key))
            {
                return 0.0f;
            }
            var val = PlayerPrefsHashtable[key];

            if (val is float)
            {
                return (float) val;
            }
            var converted = float.Parse(val.ToString());
            PlayerPrefsHashtable[key] = converted;
            val = converted;

            // ReSharper disable once PossibleInvalidCastException
            return (float)val;
        }

        public static float GetFloat(string key, float defaultValue) {
            if (PlayerPrefsHashtable.ContainsKey(key)) {
                return (float)PlayerPrefsHashtable[key];
            } else {
                PlayerPrefsHashtable.Add(key, defaultValue);
                _hashTableChanged = true;
                return defaultValue;
            }
        }

        public static bool GetBool(string key) {
            if (PlayerPrefsHashtable.ContainsKey(key)) {
                return (bool)PlayerPrefsHashtable[key];
            }

            return false;
        }

        public static bool GetBool(string key, bool defaultValue) {
            if (PlayerPrefsHashtable.ContainsKey(key)) {
                return (bool)PlayerPrefsHashtable[key];
            } else {
                PlayerPrefsHashtable.Add(key, defaultValue);
                _hashTableChanged = true;
                return defaultValue;
            }
        }

        public static void DeleteKey(string key) {
            PlayerPrefsHashtable.Remove(key);
        }

        public static void DeleteAll() {
            PlayerPrefsHashtable.Clear();
        }

        public static void Flush() {
            if (!_hashTableChanged)
            {
                return;
            }
            Serialize();

            var fileWriter = File.CreateText(FileName);

            // ReSharper disable once ConditionIsAlwaysTrueOrFalse
            // ReSharper disable HeuristicUnreachableCode
            if (fileWriter == null) {
                Debug.LogWarning("PlayerPrefs::Flush() opening file for writing failed: " + FileName);
            }
            // ReSharper restore HeuristicUnreachableCode

            fileWriter.WriteLine(_serializedOutput);

            fileWriter.Close();

            _serializedOutput = "";
        }

        private static void Serialize() {
            var myEnumerator = PlayerPrefsHashtable.GetEnumerator();

            while (myEnumerator.MoveNext()) {
                if (_serializedOutput != "") {
                    _serializedOutput += " " + ParametersSeperator + " ";
                }
                _serializedOutput += EscapeNonSeperators(myEnumerator.Key.ToString()) + " " + KeyValueSeperator + " " + EscapeNonSeperators(myEnumerator.Value.ToString()) + " " + KeyValueSeperator + " " + myEnumerator.Value.GetType();
            }
        }

        private static void Deserialize() {
            var parameters = SerializedInput.Split(new[] { " " + ParametersSeperator + " " }, StringSplitOptions.None);

            foreach (var parameter in parameters) {
                var parameterContent = parameter.Split(new[] { " " + KeyValueSeperator + " " }, StringSplitOptions.None);

                PlayerPrefsHashtable.Add(DeEscapeNonSeperators(parameterContent[0]), GetTypeValue(parameterContent[2], DeEscapeNonSeperators(parameterContent[1])));

                if (parameterContent.Length > 3) {
                    Debug.LogWarning("PlayerPrefs::Deserialize() parameterContent has " + parameterContent.Length + " elements");
                }
            }
        }

        private static string EscapeNonSeperators(string inputToEscape) {
            inputToEscape = inputToEscape.Replace(KeyValueSeperator, "\\" + KeyValueSeperator);
            inputToEscape = inputToEscape.Replace(ParametersSeperator, "\\" + ParametersSeperator);
            return inputToEscape;
        }

        private static string DeEscapeNonSeperators(string inputToDeEscape) {
            inputToDeEscape = inputToDeEscape.Replace("\\" + KeyValueSeperator, KeyValueSeperator);
            inputToDeEscape = inputToDeEscape.Replace("\\" + ParametersSeperator, ParametersSeperator);
            return inputToDeEscape;
        }

        public static object GetTypeValue(string typeName, string value) {
            switch (typeName)
            {
                case "System.String":
                    return value;
                case "System.Int32":
                    return Convert.ToInt32(value);
                case "System.Boolean":
                    return Convert.ToBoolean(value);
                case "System.Single":
                    return Convert.ToSingle(value);
                default:
                    Debug.Log("Unsupported type: " + typeName);
                    break;
            }

            return null;
        }
    }
}
#endif

/*! \endcond */