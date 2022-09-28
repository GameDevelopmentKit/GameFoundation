#if !BESTHTTP_DISABLE_ALTERNATE_SSL && (!UNITY_WEBGL || UNITY_EDITOR)
#pragma warning disable
namespace BestHTTP.SecureProtocol.Org.BouncyCastle.Bcpg
{
	using System;

	public abstract class OutputStreamPacket
    {
        private readonly BcpgOutputStream bcpgOut;

        internal OutputStreamPacket(
            BcpgOutputStream bcpgOut)
        {
            if (bcpgOut == null)
                throw new ArgumentNullException("bcpgOut");

            this.bcpgOut = bcpgOut;
        }

        public abstract BcpgOutputStream Open();

        public abstract void Close();
    }
}

#pragma warning restore
#endif