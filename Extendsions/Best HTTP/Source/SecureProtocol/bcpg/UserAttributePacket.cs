#if !BESTHTTP_DISABLE_ALTERNATE_SSL && (!UNITY_WEBGL || UNITY_EDITOR)
#pragma warning disable
namespace BestHTTP.SecureProtocol.Org.BouncyCastle.Bcpg
{
    using System.IO;
    using BestHTTP.SecureProtocol.Org.BouncyCastle.Utilities;

    /**
    * Basic type for a user attribute packet.
    */
    public class UserAttributePacket
        : ContainedPacket
    {
        private readonly UserAttributeSubpacket[] subpackets;

        public UserAttributePacket(
            BcpgInputStream bcpgIn)
        {
            var                    sIn = new UserAttributeSubpacketsParser(bcpgIn);
            UserAttributeSubpacket sub;

            var v = Platform.CreateArrayList();
            while ((sub = sIn.ReadPacket()) != null) v.Add(sub);

            this.subpackets = new UserAttributeSubpacket[v.Count];

            for (var i = 0; i != this.subpackets.Length; i++) this.subpackets[i] = (UserAttributeSubpacket)v[i];
        }

        public UserAttributePacket(
            UserAttributeSubpacket[] subpackets)
        {
            this.subpackets = subpackets;
        }

        public UserAttributeSubpacket[] GetSubpackets() { return this.subpackets; }

        public override void Encode(
            BcpgOutputStream bcpgOut)
        {
            var bOut = new MemoryStream();

            for (var i = 0; i != this.subpackets.Length; i++) this.subpackets[i].Encode(bOut);

            bcpgOut.WritePacket(PacketTag.UserAttribute, bOut.ToArray(), false);
        }
    }
}
#pragma warning restore
#endif