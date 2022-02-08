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
	using BestHTTP.SecureProtocol.Org.BouncyCastle.Crypto.Parameters;
	using BestHTTP.SecureProtocol.Org.BouncyCastle.Security;
	using BestHTTP.SecureProtocol.Org.BouncyCastle.Utilities;
	using BestHTTP.SecureProtocol.Org.BouncyCastle.Utilities.IO;

	/**
	 * General class for generating a CMS authenticated-data message.
	 * 
	 * A simple example of usage.
	 * <pre>
	 *     CMSAuthenticatedDataGenerator  fact = new CMSAuthenticatedDataGenerator();
	 *     fact.addKeyTransRecipient(cert);
	 *     CMSAuthenticatedData         data = fact.generate(content, algorithm, "BC");
	 * </pre>
	 */
	public class CmsAuthenticatedDataGenerator
		: CmsAuthenticatedGenerator
	{
		/**
	     * base constructor
	     */
		public CmsAuthenticatedDataGenerator() { }

		/**
		 * constructor allowing specific source of randomness
		 * @param rand instance of SecureRandom to use
		 */
		public CmsAuthenticatedDataGenerator(
			SecureRandom rand)
			: base(rand)
		{
		}

		/**
		 * generate an enveloped object that contains an CMS Enveloped Data
		 * object using the given provider and the passed in key generator.
		 */
		private CmsAuthenticatedData Generate(
			CmsProcessable content,
			string macOid,
			CipherKeyGenerator keyGen)
		{
			AlgorithmIdentifier macAlgId;
			KeyParameter        encKey;
			Asn1OctetString     encContent;
			Asn1OctetString     macResult;

			try
			{
				// FIXME Will this work for macs?
				var encKeyBytes = keyGen.GenerateKey();
				encKey = ParameterUtilities.CreateKeyParameter(macOid, encKeyBytes);

				var asn1Params = this.GenerateAsn1Parameters(macOid, encKeyBytes);

				ICipherParameters cipherParameters;
				macAlgId = this.GetAlgorithmIdentifier(
					macOid, encKey, asn1Params, out cipherParameters);

				var mac = MacUtilities.GetMac(macOid);
				// TODO Confirm no ParametersWithRandom needed
				// FIXME Only passing key at the moment
//	            mac.Init(cipherParameters);
				mac.Init(encKey);

				var    bOut = new MemoryStream();
				Stream mOut = new TeeOutputStream(bOut, new MacSink(mac));

				content.Write(mOut);

				Platform.Dispose(mOut);

				encContent = new BerOctetString(bOut.ToArray());

				var macOctets = MacUtilities.DoFinal(mac);
				macResult = new DerOctetString(macOctets);
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

			var eci = new ContentInfo(CmsObjectIdentifiers.Data, encContent);

			var contentInfo = new ContentInfo(
				CmsObjectIdentifiers.AuthenticatedData,
				new AuthenticatedData(null, new DerSet(recipientInfos), macAlgId, null, eci, null, macResult, null));

			return new CmsAuthenticatedData(contentInfo);
		}

		/**
	     * generate an authenticated object that contains an CMS Authenticated Data object
	     */
		public CmsAuthenticatedData Generate(
			CmsProcessable content,
			string encryptionOid)
		{
			try
			{
				// FIXME Will this work for macs?
				var keyGen = GeneratorUtilities.GetKeyGenerator(encryptionOid);

				keyGen.Init(new KeyGenerationParameters(this.rand, keyGen.DefaultStrength));

				return this.Generate(content, encryptionOid, keyGen);
			}
			catch (SecurityUtilityException e)
			{
				throw new CmsException("can't find key generation algorithm.", e);
			}
		}
	}
}
#pragma warning restore
#endif
