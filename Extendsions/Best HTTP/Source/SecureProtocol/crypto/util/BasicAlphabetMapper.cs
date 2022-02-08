#if !BESTHTTP_DISABLE_ALTERNATE_SSL && (!UNITY_WEBGL || UNITY_EDITOR)
#pragma warning disable
namespace BestHTTP.SecureProtocol.Org.BouncyCastle.Crypto.Utilities
{
    using System;
    using System.Collections;
    using BestHTTP.SecureProtocol.Org.BouncyCastle.Utilities;

    /**
     * A basic alphabet mapper that just creates a mapper based on the
     * passed in array of characters.
     */
    public class BasicAlphabetMapper
        : IAlphabetMapper
    {
        private readonly IDictionary indexMap = Platform.CreateHashtable();
        private readonly IDictionary charMap  = Platform.CreateHashtable();

        /**
         * Base constructor.
         * 
         * @param alphabet a string of characters making up the alphabet.
         */
        public BasicAlphabetMapper(string alphabet) :
            this(alphabet.ToCharArray())
        {
        }

        /**
         * Base constructor.
         * 
         * @param alphabet an array of characters making up the alphabet.
         */
        public BasicAlphabetMapper(char[] alphabet)
        {
            for (var i = 0; i != alphabet.Length; i++)
            {
                if (this.indexMap.Contains(alphabet[i])) throw new ArgumentException("duplicate key detected in alphabet: " + alphabet[i]);
                this.indexMap.Add(alphabet[i], i);
                this.charMap.Add(i, alphabet[i]);
            }
        }

        public int Radix => this.indexMap.Count;

        public byte[] ConvertToIndexes(char[] input)
        {
            byte[] outBuf;

            if (this.indexMap.Count <= 256)
            {
                outBuf = new byte[input.Length];
                for (var i = 0; i != input.Length; i++) outBuf[i] = (byte)(int)this.indexMap[input[i]];
            }
            else
            {
                outBuf = new byte[input.Length * 2];
                for (var i = 0; i != input.Length; i++)
                {
                    var idx = (int)this.indexMap[input[i]];
                    outBuf[i * 2]     = (byte)((idx >> 8) & 0xff);
                    outBuf[i * 2 + 1] = (byte)(idx & 0xff);
                }
            }

            return outBuf;
        }

        public char[] ConvertToChars(byte[] input)
        {
            char[] outBuf;

            if (this.charMap.Count <= 256)
            {
                outBuf = new char[input.Length];
                for (var i = 0; i != input.Length; i++) outBuf[i] = (char)this.charMap[input[i] & 0xff];
            }
            else
            {
                if ((input.Length & 0x1) != 0) throw new ArgumentException("two byte radix and input string odd.Length");

                outBuf = new char[input.Length / 2];
                for (var i = 0; i != input.Length; i += 2) outBuf[i / 2] = (char)this.charMap[((input[i] << 8) & 0xff00) | (input[i + 1] & 0xff)];
            }

            return outBuf;
        }
    }
}
#pragma warning restore
#endif