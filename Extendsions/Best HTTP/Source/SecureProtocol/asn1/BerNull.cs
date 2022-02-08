#if !BESTHTTP_DISABLE_ALTERNATE_SSL && (!UNITY_WEBGL || UNITY_EDITOR)
#pragma warning disable
namespace BestHTTP.SecureProtocol.Org.BouncyCastle.Asn1
{
	/**
	 * A BER Null object.
	 */

	public class BerNull
		: DerNull
	{
		public new static readonly BerNull Instance = new();

		private BerNull()
		{
		}
	}
}
#pragma warning restore
#endif
