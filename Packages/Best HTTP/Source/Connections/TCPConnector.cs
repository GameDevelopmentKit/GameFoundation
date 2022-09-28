#if !UNITY_WEBGL || UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;

using BestHTTP.Extensions;

#if !NETFX_CORE || UNITY_EDITOR
using System.Net.Security;
#endif

#if !BESTHTTP_DISABLE_ALTERNATE_SSL && (!UNITY_WEBGL || UNITY_EDITOR)
using BestHTTP.SecureProtocol.Org.BouncyCastle.Crypto.Tls;
using BestHTTP.PlatformSupport.Memory;
using System.Runtime.InteropServices;
using System.Net.Sockets;
#endif

#if NETFX_CORE
    using System.Threading.Tasks;
    using Windows.Networking.Sockets;

    using TcpClient = BestHTTP.PlatformSupport.TcpClient.WinRT.TcpClient;

    //Disable CD4014: Because this call is not awaited, execution of the current method continues before the call is completed. Consider applying the 'await' operator to the result of the call.
#pragma warning disable 4014
#else
    using TcpClient = BestHTTP.PlatformSupport.TcpClient.General.TcpClient;
#endif

using BestHTTP.Timings;

namespace BestHTTP.Connections
{
    // Non-used experimental stream. Reading from the inner stream is done parallel and Read is blocked if no data is buffered.
    // Additionally BC reads 5 bytes for the TLS header, than the payload. Buffering data from the network could save at least one context switch per TLS message.
    // In theory it, could help as reading from the network could be done parallel with TLS decryption.
    // However, if decrypting data is done faster than data is coming on the network, waiting for data longer and letting SpinWait to go deep-sleep it's going to
    // resume the thread milliseconds after new data is available. Those little afters are adding up and actually slowing down the download.
    // Not using locking just calling TryDequeue until there's data would solve the slow-down, but with the price of using 100% CPU of a core.
    // The whole struggle might worth it if Unity would implement SocketAsyncEventArgs properly.
    //sealed class BufferedReadNetworkStream : Stream
    //{
    //    public override bool CanRead => throw new NotImplementedException();
    //
    //    public override bool CanSeek => throw new NotImplementedException();
    //
    //    public override bool CanWrite => throw new NotImplementedException();
    //
    //    public override long Length => throw new NotImplementedException();
    //
    //    public override long Position { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
    //
    //    byte[] buf;
    //    int available = 0;
    //    int pos = 0;
    //
    //    private System.Net.Sockets.Socket client;
    //    int readBufferSize;
    //    int bufferSize;
    //    private System.Threading.SpinWait spinWait = new System.Threading.SpinWait();
    //
    //    System.Collections.Concurrent.ConcurrentQueue<BufferSegment> downloadedData = new System.Collections.Concurrent.ConcurrentQueue<BufferSegment>();
    //    private int downloadedBytes;
    //    private System.Threading.SpinWait downWait = new System.Threading.SpinWait();
    //    private int closed = 0;
    //
    //    //System.Net.Sockets.SocketAsyncEventArgs socketAsyncEventArgs = new System.Net.Sockets.SocketAsyncEventArgs();
    //
    //    //DateTime started;
    //
    //    public BufferedReadNetworkStream(System.Net.Sockets.Socket socket, int readBufferSize, int bufferSize)
    //    {
    //        this.client = socket;
    //        this.readBufferSize = readBufferSize;
    //        this.bufferSize = bufferSize;
    //
    //        //this.socketAsyncEventArgs.AcceptSocket = this.client;
    //        //
    //        //var buffer = BufferPool.Get(this.readBufferSize, true);
    //        //this.socketAsyncEventArgs.SetBuffer(buffer, 0, buffer.Length);
    //        //
    //        ////var bufferList = new List<ArraySegment<byte>>();
    //        ////for (int i = 0; i < 1; i++)
    //        ////{
    //        ////    var buffer = BufferPool.Get(this.readBufferSize, true);
    //        ////    bufferList.Add(new ArraySegment<byte>(buffer));
    //        ////}
    //        ////this.socketAsyncEventArgs.BufferList = bufferList;
    //        //
    //        //this.socketAsyncEventArgs.Completed += SocketAsyncEventArgs_Completed;
    //        //
    //        //this.started = DateTime.Now;
    //        //if (!this.client.ReceiveAsync(this.socketAsyncEventArgs))
    //        //    SocketAsyncEventArgs_Completed(null, this.socketAsyncEventArgs);
    //
    //        BestHTTP.PlatformSupport.Threading.ThreadedRunner.RunShortLiving(() =>
    //        {
    //            DateTime started = DateTime.Now;
    //            try
    //            {
    //                while (closed == 0)
    //                {
    //                    var buffer = BufferPool.Get(this.readBufferSize, true);
    //
    //                    int count = this.client.Receive(buffer, 0, buffer.Length, System.Net.Sockets.SocketFlags.None);
    //                    //int count = 0;
    //                    //unsafe {
    //                    //    fixed (byte* pBuffer = buffer)
    //                    //    {
    //                    //        int zero = 0;
    //                    //        count = recvfrom(this.client.Handle, pBuffer, buffer.Length, SocketFlags.None, null, ref zero);
    //                    //    }
    //                    //}
    //
    //                    this.downloadedData.Enqueue(new BufferSegment(buffer, 0, count));
    //                    System.Threading.Interlocked.Add(ref downloadedBytes, count);
    //
    //                    if (HTTPManager.Logger.Level <= Logger.Loglevels.Warning)
    //                        HTTPManager.Logger.Warning(nameof(BufferedReadNetworkStream), $"read count: {count:N0} downloadedBytes: {downloadedBytes:N0} / {this.bufferSize:N0}");
    //
    //                    if (count <= 0)
    //                    {
    //                        System.Threading.Interlocked.Exchange(ref closed, 1);
    //                        return;
    //                    }
    //
    //                    while (downloadedBytes >= this.bufferSize)
    //                    {
    //                        downWait.SpinOnce();
    //                    }
    //                }
    //            }
    //            catch (Exception ex)
    //            {
    //                UnityEngine.Debug.LogException(ex);
    //            }
    //            finally
    //            {
    //                UnityEngine.Debug.Log($"Reading finished in {(DateTime.Now - started)}");
    //            }
    //        });
    //    }
    //
    //    //private void SocketAsyncEventArgs_Completed(object sender, System.Net.Sockets.SocketAsyncEventArgs e)
    //    //{
    //    //    this.downloadedData.Enqueue(new BufferSegment(e.Buffer, 0, e.BytesTransferred));
    //    //
    //    //    if (e.BytesTransferred == 0)
    //    //    {
    //    //        UnityEngine.Debug.Log($"Reading finished in {(DateTime.Now - started)}");
    //    //        return;
    //    //    }
    //    //
    //    //    int down = System.Threading.Interlocked.Add(ref downloadedBytes, e.BytesTransferred);
    //    //
    //    //    if (HTTPManager.Logger.Level <= Logger.Loglevels.Warning)
    //    //        HTTPManager.Logger.Warning(nameof(BufferedReadNetworkStream), $"SocketAsyncEventArgs_Completed - read count: {e.BytesTransferred:N0} downloadedBytes: {down:N0} / {this.bufferSize:N0}");
    //    //
    //    //    var buffer = BufferPool.Get(this.readBufferSize, true);
    //    //    this.socketAsyncEventArgs.SetBuffer(buffer, 0, buffer.Length);
    //    //
    //    //    if (!this.client.ReceiveAsync(this.socketAsyncEventArgs))
    //    //        SocketAsyncEventArgs_Completed(null, this.socketAsyncEventArgs);
    //    //}
    //
    //    private void SwitchBuffers(bool waitForData)
    //    {
    //        //HTTPManager.Logger.Error("Read", $"{this.downloadedData.Count}");
    //        BufferSegment segment;
    //        while (!this.downloadedData.TryDequeue(out segment))
    //        {
    //            if (waitForData && closed == 0)
    //            {
    //                if (HTTPManager.Logger.Level <= Logger.Loglevels.Error)
    //                    HTTPManager.Logger.Error(nameof(BufferedReadNetworkStream), $"SpinOnce");
    //                this.spinWait.SpinOnce();
    //            }
    //            else
    //                return;
    //        }
    //
    //        //if (segment.Count <= 0)
    //        //    throw new Exception("Connection closed!");
    //
    //        if (buf != null)
    //            BufferPool.Release(buf);
    //
    //        System.Threading.Interlocked.Add(ref downloadedBytes, -segment.Count);
    //
    //        buf = segment.Data;
    //        available = segment.Count;
    //        pos = 0;
    //    }
    //
    //    public override int Read(byte[] buffer, int offset, int size)
    //    {
    //        if (this.buf == null)
    //        {
    //            SwitchBuffers(true);
    //        }
    //
    //        if (size <= available)
    //        {
    //            Array.Copy(buf, pos, buffer, offset, size);
    //            available -= size;
    //            pos += size;
    //
    //            if (available == 0)
    //            {
    //                SwitchBuffers(false);
    //            }
    //
    //            return size;
    //        }
    //        else
    //        {
    //            int readcount = 0;
    //            if (available > 0)
    //            {
    //                Array.Copy(buf, pos, buffer, offset, available);
    //                offset += available;
    //                readcount += available;
    //                available = 0;
    //                pos = 0;
    //            }
    //
    //            while (true)
    //            {
    //                try
    //                {
    //                    SwitchBuffers(true);
    //                }
    //                catch (Exception ex)
    //                {
    //                    if (readcount > 0)
    //                    {
    //                        return readcount;
    //                    }
    //
    //                    throw (ex);
    //                }
    //
    //                if (available < 1)
    //                {
    //                    if (readcount > 0)
    //                    {
    //                        return readcount;
    //                    }
    //
    //                    return available;
    //                }
    //                else
    //                {
    //                    int toread = size - readcount;
    //                    if (toread <= available)
    //                    {
    //                        Array.Copy(buf, pos, buffer, offset, toread);
    //                        available -= toread;
    //                        pos += toread;
    //                        readcount += toread;
    //                        return readcount;
    //                    }
    //                    else
    //                    {
    //                        Array.Copy(buf, pos, buffer, offset, available);
    //                        offset += available;
    //                        readcount += available;
    //                        pos = 0;
    //                        available = 0;
    //                    }
    //                }
    //            }
    //        }
    //    }
    //
    //    public override long Seek(long offset, SeekOrigin origin)
    //    {
    //        throw new NotImplementedException();
    //    }
    //
    //    public override void SetLength(long value)
    //    {
    //        throw new NotImplementedException();
    //    }
    //
    //    public override void Write(byte[] buffer, int offset, int count)
    //    {
    //        this.client.Send(buffer, offset, count, System.Net.Sockets.SocketFlags.None);
    //
    //        HTTPManager.Logger.Warning(nameof(BufferedReadNetworkStream), $"Wrote: {count}");
    //    }
    //
    //    public override void Close()
    //    {
    //        base.Close();
    //
    //        //socketAsyncEventArgs.Dispose();
    //        //socketAsyncEventArgs = null;
    //    }
    //
    //    public override void Flush()
    //    {
    //    }
    //}

    sealed class BufferedReadNetworkStream : Stream
    {
        public override bool CanRead { get { throw new NotImplementedException(); } }

        public override bool CanSeek { get { throw new NotImplementedException(); } }

        public override bool CanWrite { get { throw new NotImplementedException(); } }

        public override long Length { get { throw new NotImplementedException(); } }

        public override long Position { get { throw new NotImplementedException(); } set { throw new NotImplementedException(); } }

        private ReadOnlyBufferedStream readStream;
        private Stream innerStream;

        public BufferedReadNetworkStream(Stream stream, int bufferSize)
        {
            this.innerStream = stream;
            this.readStream = new ReadOnlyBufferedStream(stream, bufferSize);
        }

        public override void Flush()
        {
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            return this.readStream.Read(buffer, offset, count);
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotImplementedException();
        }

        public override void SetLength(long value)
        {
            throw new NotImplementedException();
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            this.innerStream.Write(buffer, offset, count);
        }

        public override void Close()
        {
            base.Close();

            this.innerStream.Close();
        }
    }

    public sealed class TCPConnector : IDisposable
    {
        public bool IsConnected { get { return this.Client != null && this.Client.Connected; } }

        public string NegotiatedProtocol { get; private set; }

        public TcpClient Client { get; private set; }

        public Stream TopmostStream { get; private set; }

        public Stream Stream { get; private set; }

        public bool LeaveOpen { get; set; }

        public void Connect(HTTPRequest request)
        {
            string negotiatedProtocol = HTTPProtocolFactory.W3C_HTTP1;

            Uri uri =
#if !BESTHTTP_DISABLE_PROXY
                request.HasProxy ? request.Proxy.Address :
#endif
                request.CurrentUri;

            #region TCP Connection

            if (Client == null)
                Client = new TcpClient();

            if (!Client.Connected)
            {
                Client.ConnectTimeout = request.ConnectTimeout;

#if NETFX_CORE
                Client.UseHTTPSProtocol =
#if !BESTHTTP_DISABLE_ALTERNATE_SSL && (!UNITY_WEBGL || UNITY_EDITOR)
                    !Request.UseAlternateSSL &&
#endif
                    HTTPProtocolFactory.IsSecureProtocol(uri);
#endif

                if (HTTPManager.Logger.Level == Logger.Loglevels.All)
                    HTTPManager.Logger.Verbose("TCPConnector", string.Format("'{0}' - Connecting to {1}:{2}", request.CurrentUri.ToString(), uri.Host, uri.Port.ToString()), request.Context);

#if !NETFX_CORE && (!UNITY_WEBGL || UNITY_EDITOR)
                bool changed = false;
                int? sendBufferSize = null, receiveBufferSize = null;

                if (HTTPManager.SendBufferSize.HasValue)
                {
                    sendBufferSize = Client.SendBufferSize;
                    Client.SendBufferSize = HTTPManager.SendBufferSize.Value;
                    changed = true;
                }

                if (HTTPManager.ReceiveBufferSize.HasValue)
                {
                    receiveBufferSize = Client.ReceiveBufferSize;
                    Client.ReceiveBufferSize = HTTPManager.ReceiveBufferSize.Value;
                    changed = true;
                }

                if (HTTPManager.Logger.Level == Logger.Loglevels.All)
                {
                    if (changed)
                        HTTPManager.Logger.Verbose("TCPConnector", string.Format("'{0}' - Buffer sizes changed - Send from: {1} to: {2}, Receive from: {3} to: {4}, Blocking: {5}",
                                request.CurrentUri.ToString(),
                                sendBufferSize,
                                Client.SendBufferSize,
                                receiveBufferSize,
                                Client.ReceiveBufferSize,
                                Client.Client.Blocking),
                            request.Context);
                    else
                        HTTPManager.Logger.Verbose("TCPConnector", string.Format("'{0}' - Buffer sizes - Send: {1} Receive: {2} Blocking: {3}", request.CurrentUri.ToString(), Client.SendBufferSize, Client.ReceiveBufferSize, Client.Client.Blocking), request.Context);
                }
#endif

#if NETFX_CORE && !UNITY_EDITOR && !ENABLE_IL2CPP
                try
                {
                    Client.Connect(uri.Host, uri.Port);
                }
                finally
                {
                    request.Timing.Add(TimingEventNames.TCP_Connection);
                }
#else
                System.Net.IPAddress[] addresses = null;
                try
                {
                    if (Client.ConnectTimeout > TimeSpan.Zero)
                    {
                        // https://forum.unity3d.com/threads/best-http-released.200006/page-37#post-3150972
                        using (System.Threading.ManualResetEvent mre = new System.Threading.ManualResetEvent(false))
                        {
                            IAsyncResult result = System.Net.Dns.BeginGetHostAddresses(uri.Host, (res) => { try { mre.Set(); } catch { } }, null);
                            bool success = mre.WaitOne(Client.ConnectTimeout);
                            if (success)
                            {
                                addresses = System.Net.Dns.EndGetHostAddresses(result);
                            }
                            else
                            {
                                throw new TimeoutException("DNS resolve timed out!");
                            }
                        }
                    }
                    else
                    {
                        addresses = System.Net.Dns.GetHostAddresses(uri.Host);
                    }
                }
                finally
                {
                    request.Timing.Add(TimingEventNames.DNS_Lookup);
                }

                if (HTTPManager.Logger.Level == Logger.Loglevels.All)
                    HTTPManager.Logger.Verbose("TCPConnector", string.Format("'{0}' - DNS Query returned with addresses: {1}", request.CurrentUri.ToString(), addresses != null ? addresses.Length : -1), request.Context);

                if (request.IsCancellationRequested)
                    throw new Exception("IsCancellationRequested");

                try
                {
                    Client.Connect(addresses, uri.Port, request);
                }
                finally
                {
                    request.Timing.Add(TimingEventNames.TCP_Connection);
                }

                if (request.IsCancellationRequested)
                    throw new Exception("IsCancellationRequested");
#endif

                if (HTTPManager.Logger.Level <= Logger.Loglevels.Information)
                    HTTPManager.Logger.Information("TCPConnector", "Connected to " + uri.Host + ":" + uri.Port.ToString(), request.Context);
            }
            else if (HTTPManager.Logger.Level <= Logger.Loglevels.Information)
                HTTPManager.Logger.Information("TCPConnector", "Already connected to " + uri.Host + ":" + uri.Port.ToString(), request.Context);

#endregion

            if (Stream == null)
            {
                bool isSecure = HTTPProtocolFactory.IsSecureProtocol(request.CurrentUri);

                // set the stream to Client.GetStream() so the proxy, if there's any can use it directly.
                this.Stream = this.TopmostStream = Client.GetStream();

                /*if (Stream.CanTimeout)
                    Stream.ReadTimeout = Stream.WriteTimeout = (int)Request.Timeout.TotalMilliseconds;*/

#if !BESTHTTP_DISABLE_PROXY
                if (request.HasProxy)
                {
                    try
                    {
                        request.Proxy.Connect(this.Stream, request);
                    }
                    finally
                    {
                        request.Timing.Add(TimingEventNames.Proxy_Negotiation);
                    }
                }

                if (request.IsCancellationRequested)
                    throw new Exception("IsCancellationRequested");
#endif

                // proxy connect is done, we can set the stream to a buffered one. HTTPProxy requires the raw NetworkStream for HTTPResponse's ReadUnknownSize!
                this.Stream = this.TopmostStream = new BufferedReadNetworkStream(Client.GetStream(), Math.Max(8 * 1024, HTTPManager.ReceiveBufferSize ?? Client.ReceiveBufferSize));

                // We have to use Request.CurrentUri here, because uri can be a proxy uri with a different protocol
                if (isSecure)
                {
                    DateTime tlsNegotiationStartedAt = DateTime.Now;
#region SSL Upgrade

#if !BESTHTTP_DISABLE_ALTERNATE_SSL && (!UNITY_WEBGL || UNITY_EDITOR)
                    if (request.UseAlternateSSL)
                    {
                        var handler = new TlsClientProtocol(this.Stream, new BestHTTP.SecureProtocol.Org.BouncyCastle.Security.SecureRandom());

                        List<string> protocols = new List<string>();
#if !BESTHTTP_DISABLE_HTTP2
                        SupportedProtocols protocol = request.ProtocolHandler == SupportedProtocols.Unknown ? HTTPProtocolFactory.GetProtocolFromUri(request.CurrentUri) : request.ProtocolHandler;
                        if (protocol == SupportedProtocols.HTTP)
                        {
                            // http/2 over tls (https://www.iana.org/assignments/tls-extensiontype-values/tls-extensiontype-values.xhtml#alpn-protocol-ids)
                            protocols.Add(HTTPProtocolFactory.W3C_HTTP2);
                        }
#endif

                        protocols.Add(HTTPProtocolFactory.W3C_HTTP1);

                        AbstractTlsClient tlsClient = null;
                        if (HTTPManager.TlsClientFactory == null)
                        {
                            tlsClient = HTTPManager.DefaultTlsClientFactory(request, protocols);
                        }
                        else
                        {
                            try
                            {
                                tlsClient = HTTPManager.TlsClientFactory(request, protocols);
                            }
                            catch
                            { }

                            if (tlsClient == null)
                                tlsClient = HTTPManager.DefaultTlsClientFactory(request, protocols);
                        }

                        tlsClient.LoggingContext = request.Context;
                        handler.Connect(tlsClient);

                        if (!string.IsNullOrEmpty(tlsClient.ServerSupportedProtocol))
                            negotiatedProtocol = tlsClient.ServerSupportedProtocol;

                        Stream = handler.Stream;
                    }
                    else
#endif
                    {
#if !NETFX_CORE
                        SslStream sslStream = new SslStream(Client.GetStream(), false, (sender, cert, chain, errors) =>
                        {
                            return request.CallCustomCertificationValidator(cert, chain);
                        });

                        if (!sslStream.IsAuthenticated)
                            sslStream.AuthenticateAsClient(request.CurrentUri.Host);
                        Stream = sslStream;
#else
                        Stream = Client.GetStream();
#endif
                    }
#endregion

                    request.Timing.Add(TimingEventNames.TLS_Negotiation, DateTime.Now - tlsNegotiationStartedAt);
                }
            }

            this.NegotiatedProtocol = negotiatedProtocol;
        }

        public void Close()
        {
            if (Client != null && !this.LeaveOpen)
            {
                try
                {
                    if (Stream != null)
                        Stream.Close();
                }
                catch { }
                finally
                {
                    Stream = null;
                }

                try
                {
                    Client.Close();
                }
                catch { }
                finally
                {                  
                    Client = null;
                }
            }
        }

        public void Dispose()
        {
            Close();
        }
    }
}
#endif
