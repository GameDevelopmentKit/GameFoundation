namespace GameFoundation.Scripts.Utilities.Extension
{
    using System;
    using System.IO;
    using System.Security.Cryptography;
    using System.Text;

    public class MD5Utils
    {
        public static string GetMD5HashFromFile(string fileName)
        {
            try
            {
                using (var md5 = MD5.Create())
                {
                    using (var stream = File.OpenRead(fileName))
                    {
                        var hash = md5.ComputeHash(stream);

                        StringBuilder sb = new StringBuilder();
                        for (int i = 0; i < hash.Length; i++)
                        {
                            sb.Append(hash[i].ToString("X2"));
                        }
                        return sb.ToString();
                    }
                }
            }
            catch (Exception ex)
            {

            }

            return string.Empty;
        }
    }
}