#if !BESTHTTP_DISABLE_ALTERNATE_SSL && (!UNITY_WEBGL || UNITY_EDITOR)
#pragma warning disable
namespace BestHTTP.SecureProtocol.Org.BouncyCastle.Cms
{
	using System.IO;
	using BestHTTP.SecureProtocol.Org.BouncyCastle.Asn1;
	using BestHTTP.SecureProtocol.Org.BouncyCastle.Asn1.Cms;
	using BestHTTP.SecureProtocol.Org.BouncyCastle.Asn1.X509;
	using BestHTTP.SecureProtocol.Org.BouncyCastle.Crypto;
	using BestHTTP.SecureProtocol.Org.BouncyCastle.Crypto.IO;
	using BestHTTP.SecureProtocol.Org.BouncyCastle.Security;
	using BestHTTP.SecureProtocol.Org.BouncyCastle.Utilities;
	using BestHTTP.SecureProtocol.Org.BouncyCastle.Utilities.IO;

	/**
	 * General class for generating a CMS authenticated-data message stream.
	 * <p>
	 *     A simple example of usage.
	 *     <pre>
	 *         CMSAuthenticatedDataStreamGenerator edGen = new CMSAuthenticatedDataStreamGenerator();
	 *         edGen.addKeyTransRecipient(cert);
	 *         ByteArrayOutputStream  bOut = new ByteArrayOutputStream();
	 *         OutputStream out = edGen.open(
	 *         bOut, CMSAuthenticatedDataGenerator.AES128_CBC, "BC");*
	 *         out.write(data);
	 *         out.close();
	 *     </pre>
	 * </p>
	 */
	public class CmsAuthenticatedDataStreamGenerator
		: CmsAuthenticatedGenerator
	{
		// TODO Add support
//		private object              _originatorInfo = null;
//		private object              _unprotectedAttributes = null;
		private int  _bufferSize;
		private bool _berEncodeRecipientSet;

		/**
		* base constructor
		*/
		public CmsAuthenticatedDataStreamGenerator() { }

		/**
		 * constructor allowing specific source of randomness
		 * @param rand instance of SecureRandom to use
		 */
		public CmsAuthenticatedDataStreamGenerator(
			SecureRandom rand)
			: base(rand)
		{
		}

		/**
		 * Set the underlying string size for encapsulated data
		 * 
		 * @param bufferSize length of octet strings to buffer the data.
		 */
		public void SetBufferSize(
			int bufferSize)
		{
			this._bufferSize = bufferSize;
		}

		/**
		* Use a BER Set to store the recipient information
		*/
		public void SetBerEncodeRecipients(
			bool berEncodeRecipientSet)
		{
			this._berEncodeRecipientSet = berEncodeRecipientSet;
		}

		/**
		 * generate an enveloped object that contains an CMS Enveloped Data
		 * object using the given provider and the passed in key generator.
		 * @throws java.io.IOException
		 */
		private Stream Open(
			Stream outStr,
			string macOid,
			CipherKeyGenerator keyGen)
		{
			// FIXME Will this work for macs?
			var encKeyBytes = keyGen.GenerateKey();
			var encKey      = ParameterUtilities.CreateKeyParameter(macOid, encKeyBytes);

			var asn1Params = this.GenerateAsn1Parameters(macOid, encKeyBytes);

			ICipherParameters cipherParameters;
			var macAlgId = this.GetAlgorithmIdentifier(
				macOid, encKey, asn1Params, out cipherParameters);

			var recipientInfos = new Asn1EncodableVector();

			foreach (RecipientInfoGenerator rig in this.recipientInfoGenerators)
				try
				{
					recipientInfos.Add(rig.Generate(encKey, this.rand));
				}
				catch (InvalidKeyException e)
				{
					throw new CmsException("key inappropriate for algorithm.", e);
				}
				catch (GeneralSecurityException e)
				{
					throw new CmsException("error making encrypted content.", e);
				}

			// FIXME Only passing key at the moment
//			return Open(outStr, macAlgId, cipherParameters, recipientInfos);
			return this.Open(outStr, macAlgId, encKey, recipientInfos);
		}

		protected Stream Open(
			Stream outStr,
			AlgorithmIdentifier macAlgId,
			ICipherParameters cipherParameters,
			Asn1EncodableVector recipientInfos)
		{
			try
			{
				//
				// ContentInfo
				//
				var cGen = new BerSequenceGenerator(outStr);

				cGen.AddObject(CmsObjectIdentifiers.AuthenticatedData);

				//
				// Authenticated Data
				//
				var authGen = new BerSequenceGenerator(
					cGen.GetRawOutputStream(), 0, true);

				authGen.AddObject(new DerInteger(AuthenticatedData.CalculateVersion(null)));

				var authRaw = authGen.GetRawOutputStream();
				Asn1Generator recipGen = this._berEncodeRecipientSet
					? new BerSetGenerator(authRaw)
					: new DerSetGenerator(authRaw);

				foreach (Asn1Encodable ae in recipientInfos) recipGen.AddObject(ae);

				recipGen.Close();

				authGen.AddObject(macAlgId);

				var eiGen = new BerSequenceGenerator(authRaw);
				eiGen.AddObject(CmsObjectIdentifiers.Data);

				var octetOutputStream = CmsUtilities.CreateBerOctetOutputStream(
					eiGen.GetRawOutputStream(), 0, true, this._bufferSize);

				var mac = MacUtilities.GetMac(macAlgId.Algorithm);
				// TODO Confirm no ParametersWithRandom needed
				mac.Init(cipherParameters);
				Stream mOut = new TeeOutputStream(octetOutputStream, new MacSink(mac));

				return new CmsAuthenticatedDataOutputStream(mOut, mac, cGen, authGen, eiGen);
			}
			catch (SecurityUtilityException e)
			{
				throw new CmsException("couldn't create cipher.", e);
			}
			catch (InvalidKeyException e)
			{
				throw new CmsException("key invalid in message.", e);
			}
			catch (IOException e)
			{
				throw new CmsException("exception decoding algorithm parameters.", e);
			}
		}

		/**
		* generate an enveloped object that contains an CMS Enveloped Data object
		*/
		public Stream Open(
			Stream outStr,
			string encryptionOid)
		{
			var keyGen = GeneratorUtilities.GetKeyGenerator(encryptionOid);

			keyGen.Init(new KeyGenerationParameters(this.rand, keyGen.DefaultStrength));

			return this.Open(outStr, encryptionOid, keyGen);
		}

		/**
		* generate an enveloped object that contains an CMS Enveloped Data object
		*/
		public Stream Open(
			Stream outStr,
			string encryptionOid,
			int keySize)
		{
			var keyGen = GeneratorUtilities.GetKeyGenerator(encryptionOid);

			keyGen.Init(new KeyGenerationParameters(this.rand, keySize));

			return this.Open(outStr, encryptionOid, keyGen);
		}

		private class CmsAuthenticatedDataOutputStream
			: BaseOutputStream
		{
			private readonly Stream               macStream;
			private readonly IMac                 mac;
			private readonly BerSequenceGenerator cGen;
			private readonly BerSequenceGenerator authGen;
			private readonly BerSequenceGenerator eiGen;

			public CmsAuthenticatedDataOutputStream(
				Stream macStream,
				IMac mac,
				BerSequenceGenerator cGen,
				BerSequenceGenerator authGen,
				BerSequenceGenerator eiGen)
			{
				this.macStream = macStream;
				this.mac       = mac;
				this.cGen      = cGen;
				this.authGen   = authGen;
				this.eiGen     = eiGen;
			}

			public override void WriteByte(
				byte b)
			{
				this.macStream.WriteByte(b);
			}

			public override void Write(
				byte[] bytes,
				int off,
				int len)
			{
				this.macStream.Write(bytes, off, len);
			}

#if PORTABLE || NETFX_CORE
            protected override void Dispose(bool disposing)
            {
                if (disposing)
                {
                    BestHTTP.SecureProtocol.Org.BouncyCastle.Utilities.Platform.Dispose(macStream);

                    // TODO Parent context(s) should really be be closed explicitly

				    eiGen.Close();

				    // [TODO] auth attributes go here 
				    byte[] macOctets = MacUtilities.DoFinal(mac);
				    authGen.AddObject(new DerOctetString(macOctets));
				    // [TODO] unauth attributes go here

				    authGen.Close();
				    cGen.Close();
                }
                base.Dispose(disposing);
            }
#else
			public override void Close()
			{
				Platform.Dispose(this.macStream);

				// TODO Parent context(s) should really be be closed explicitly

				this.eiGen.Close();

				// [TODO] auth attributes go here 
				var macOctets = MacUtilities.DoFinal(this.mac);
				this.authGen.AddObject(new DerOctetString(macOctets));
				// [TODO] unauth attributes go here

				this.authGen.Close();
				this.cGen.Close();
				base.Close();
			}
#endif
		}
	}
}
#pragma warning restore
#endif
