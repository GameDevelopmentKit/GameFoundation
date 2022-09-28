#if !BESTHTTP_DISABLE_ALTERNATE_SSL && (!UNITY_WEBGL || UNITY_EDITOR)
#pragma warning disable
namespace BestHTTP.SecureProtocol.Org.BouncyCastle.Bcpg.Attr
{
    using System;
    using System.IO;

    /// <remarks>Basic type for a image attribute packet.</remarks>
    public class ImageAttrib
        : UserAttributeSubpacket
    {
        public enum Format : byte
        {
            Jpeg = 1
        }

        private static readonly byte[] Zeroes = new byte[12];

        private readonly int    hdrLength;
        private readonly byte[] imageData;

        public ImageAttrib(byte[] data)
            : this(false, data)
        {
        }

        public ImageAttrib(bool forceLongLength, byte[] data)
            : base(UserAttributeSubpacketTag.ImageAttribute, forceLongLength, data)
        {
            this.hdrLength = ((data[1] & 0xff) << 8) | (data[0] & 0xff);
            this.Version   = data[2] & 0xff;
            this.Encoding  = data[3] & 0xff;

            this.imageData = new byte[data.Length - this.hdrLength];
            Array.Copy(data, this.hdrLength, this.imageData, 0, this.imageData.Length);
        }

        public ImageAttrib(
            Format imageType,
            byte[] imageData)
            : this(ToByteArray(imageType, imageData))
        {
        }

        private static byte[] ToByteArray(
            Format imageType,
            byte[] imageData)
        {
            var bOut = new MemoryStream();
            bOut.WriteByte(0x10);
            bOut.WriteByte(0x00);
            bOut.WriteByte(0x01);
            bOut.WriteByte((byte)imageType);
            bOut.Write(Zeroes, 0, Zeroes.Length);
            bOut.Write(imageData, 0, imageData.Length);
            return bOut.ToArray();
        }

        public virtual int Version { get; }

        public virtual int Encoding { get; }

        public virtual byte[] GetImageData() { return this.imageData; }
    }
}
#pragma warning restore
#endif