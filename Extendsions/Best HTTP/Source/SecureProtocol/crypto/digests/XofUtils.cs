#if !BESTHTTP_DISABLE_ALTERNATE_SSL && (!UNITY_WEBGL || UNITY_EDITOR)
#pragma warning disable
namespace BestHTTP.SecureProtocol.Org.BouncyCastle.Crypto.Digests
{
    using BestHTTP.SecureProtocol.Org.BouncyCastle.Utilities;

    internal class XofUtilities
    {
        internal static byte[] LeftEncode(long strLen)
        {
            byte n = 1;

            var v = strLen;
            while ((v >>= 8) != 0) n++;

            var b = new byte[n + 1];

            b[0] = n;

            for (var i = 1; i <= n; i++) b[i] = (byte)(strLen >> (8 * (n - i)));

            return b;
        }

        internal static byte[] RightEncode(long strLen)
        {
            byte n = 1;

            var v = strLen;
            while ((v >>= 8) != 0) n++;

            var b = new byte[n + 1];

            b[n] = n;

            for (var i = 0; i < n; i++) b[i] = (byte)(strLen >> (8 * (n - i - 1)));

            return b;
        }

        internal static byte[] Encode(byte X) { return Arrays.Concatenate(LeftEncode(8), new[] { X }); }

        internal static byte[] Encode(byte[] inBuf, int inOff, int len)
        {
            if (inBuf.Length == len) return Arrays.Concatenate(LeftEncode(len * 8), inBuf);
            return Arrays.Concatenate(LeftEncode(len * 8), Arrays.CopyOfRange(inBuf, inOff, inOff + len));
        }
    }
}
#pragma warning restore
#endif