#if !BESTHTTP_DISABLE_ALTERNATE_SSL && (!UNITY_WEBGL || UNITY_EDITOR)
#pragma warning disable
namespace BestHTTP.SecureProtocol.Org.BouncyCastle.Tls
{
    using System.IO;
    using BestHTTP.SecureProtocol.Org.BouncyCastle.Math;
    using BestHTTP.SecureProtocol.Org.BouncyCastle.Utilities;

    public sealed class ServerSrpParams
    {
        public ServerSrpParams(BigInteger N, BigInteger g, byte[] s, BigInteger B)
        {
            this.N = N;
            this.G = g;
            this.S = Arrays.Clone(s);
            this.B = B;
        }

        public BigInteger B { get; }

        public BigInteger G { get; }

        public BigInteger N { get; }

        public byte[] S { get; }

        /// <summary>Encode this <see cref="ServerSrpParams" /> to a <see cref="Stream" />.</summary>
        /// <param name="output">the <see cref="Stream" /> to encode to.</param>
        /// <exception cref="IOException" />
        public void Encode(Stream output)
        {
            TlsSrpUtilities.WriteSrpParameter(this.N, output);
            TlsSrpUtilities.WriteSrpParameter(this.G, output);
            TlsUtilities.WriteOpaque8(this.S, output);
            TlsSrpUtilities.WriteSrpParameter(this.B, output);
        }

        /// <summary>Parse a <see cref="ServerSrpParams" /> from a <see cref="Stream" />.</summary>
        /// <param name="input">the <see cref="Stream" /> to parse from.</param>
        /// <returns>a <see cref="ServerSrpParams" /> object.</returns>
        /// <exception cref="IOException" />
        public static ServerSrpParams Parse(Stream input)
        {
            var N = TlsSrpUtilities.ReadSrpParameter(input);
            var g = TlsSrpUtilities.ReadSrpParameter(input);
            var s = TlsUtilities.ReadOpaque8(input, 1);
            var B = TlsSrpUtilities.ReadSrpParameter(input);

            return new ServerSrpParams(N, g, s, B);
        }
    }
}
#pragma warning restore
#endif