#if !BESTHTTP_DISABLE_ALTERNATE_SSL && (!UNITY_WEBGL || UNITY_EDITOR)
#pragma warning disable
namespace BestHTTP.SecureProtocol.Org.BouncyCastle.Crypto.Parameters
{
    using BestHTTP.SecureProtocol.Org.BouncyCastle.Utilities;

    public sealed class FpeParameters
        : ICipherParameters
    {
        private readonly byte[] tweak;

        public FpeParameters(KeyParameter key, int radix, byte[] tweak) : this(key, radix, tweak, false) { }

        public FpeParameters(KeyParameter key, int radix, byte[] tweak, bool useInverse)
        {
            this.Key                = key;
            this.Radix              = radix;
            this.tweak              = Arrays.Clone(tweak);
            this.UseInverseFunction = useInverse;
        }

        public KeyParameter Key { get; }

        public int Radix { get; }

        public bool UseInverseFunction { get; }

        public byte[] GetTweak() { return Arrays.Clone(this.tweak); }
    }
}
#pragma warning restore
#endif