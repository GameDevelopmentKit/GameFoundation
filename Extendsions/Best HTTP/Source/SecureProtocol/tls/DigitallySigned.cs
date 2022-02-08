#if !BESTHTTP_DISABLE_ALTERNATE_SSL && (!UNITY_WEBGL || UNITY_EDITOR)
#pragma warning disable
namespace BestHTTP.SecureProtocol.Org.BouncyCastle.Tls
{
    using System;
    using System.IO;

    public sealed class DigitallySigned
    {
        public DigitallySigned(SignatureAndHashAlgorithm algorithm, byte[] signature)
        {
            if (signature == null)
                throw new ArgumentNullException("signature");

            this.Algorithm = algorithm;
            this.Signature = signature;
        }

        /// <returns>a <see cref="SignatureAndHashAlgorithm" /> (or null before TLS 1.2).</returns>
        public SignatureAndHashAlgorithm Algorithm { get; }

        public byte[] Signature { get; }

        /// <summary>Encode this <see cref="DigitallySigned" /> to a <see cref="Stream" />.</summary>
        /// <param name="output">the <see cref="Stream" /> to encode to.</param>
        /// <exception cref="IOException" />
        public void Encode(Stream output)
        {
            if (this.Algorithm != null) this.Algorithm.Encode(output);
            TlsUtilities.WriteOpaque16(this.Signature, output);
        }

        /// <summary>Parse a <see cref="DigitallySigned" /> from a <see cref="Stream" />.</summary>
        /// <param name="context">the <see cref="TlsContext" /> of the current connection.</param>
        /// <param name="input">the <see cref="Stream" /> to parse from.</param>
        /// <returns>a <see cref="DigitallySigned" /> object.</returns>
        /// <exception cref="IOException" />
        public static DigitallySigned Parse(TlsContext context, Stream input)
        {
            SignatureAndHashAlgorithm algorithm = null;
            if (TlsUtilities.IsTlsV12(context))
            {
                algorithm = SignatureAndHashAlgorithm.Parse(input);

                if (SignatureAlgorithm.anonymous == algorithm.Signature)
                    throw new TlsFatalAlert(AlertDescription.illegal_parameter);
            }

            var signature = TlsUtilities.ReadOpaque16(input);
            return new DigitallySigned(algorithm, signature);
        }
    }
}
#pragma warning restore
#endif