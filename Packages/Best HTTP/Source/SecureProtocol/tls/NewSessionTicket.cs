#if !BESTHTTP_DISABLE_ALTERNATE_SSL && (!UNITY_WEBGL || UNITY_EDITOR)
#pragma warning disable
namespace BestHTTP.SecureProtocol.Org.BouncyCastle.Tls
{
    using System.IO;

    public sealed class NewSessionTicket
    {
        public NewSessionTicket(long ticketLifetimeHint, byte[] ticket)
        {
            this.TicketLifetimeHint = ticketLifetimeHint;
            this.Ticket             = ticket;
        }

        public long TicketLifetimeHint { get; }

        public byte[] Ticket { get; }

        /// <summary>Encode this <see cref="NewSessionTicket" /> to a <see cref="Stream" />.</summary>
        /// <param name="output">the <see cref="Stream" /> to encode to.</param>
        /// <exception cref="IOException" />
        public void Encode(Stream output)
        {
            TlsUtilities.WriteUint32(this.TicketLifetimeHint, output);
            TlsUtilities.WriteOpaque16(this.Ticket, output);
        }

        /// <summary>Parse a <see cref="NewSessionTicket" /> from a <see cref="Stream" />.</summary>
        /// <param name="input">the <see cref="Stream" /> to parse from.</param>
        /// <returns>a <see cref="NewSessionTicket" /> object.</returns>
        /// <exception cref="IOException" />
        public static NewSessionTicket Parse(Stream input)
        {
            var ticketLifetimeHint = TlsUtilities.ReadUint32(input);
            var ticket             = TlsUtilities.ReadOpaque16(input);
            return new NewSessionTicket(ticketLifetimeHint, ticket);
        }
    }
}
#pragma warning restore
#endif