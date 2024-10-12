namespace GameFoundation.Scripts.Utilities.Extension
{
    using System;
    using System.IO;
    using System.Security.Cryptography;
    using System.Text;
    using UnityEngine;

    public class MD5Utils
    {
        public static string GetMD5HashFromFile(string fileName)
        {
            try
            {
                using var md5    = MD5.Create();
                using var stream = File.OpenRead(fileName);
                var       hash   = md5.ComputeHash(stream);

                var sb = new StringBuilder();
                foreach (var t in hash) sb.Append(t.ToString("X2"));
                return sb.ToString();
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
            }

            return string.Empty;
        }
    }
}