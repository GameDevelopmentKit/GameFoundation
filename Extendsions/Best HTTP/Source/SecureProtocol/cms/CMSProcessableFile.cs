#if !BESTHTTP_DISABLE_ALTERNATE_SSL && (!UNITY_WEBGL || UNITY_EDITOR)
#pragma warning disable
#if !PORTABLE || NETFX_CORE || DOTNET
namespace BestHTTP.SecureProtocol.Org.BouncyCastle.Cms
{
	using System;
	using System.IO;
	using BestHTTP.SecureProtocol.Org.BouncyCastle.Utilities;
	using BestHTTP.SecureProtocol.Org.BouncyCastle.Utilities.IO;

	/**
	* a holding class for a file of data to be processed.
	*/
	public class CmsProcessableFile
		: CmsProcessable, CmsReadable
	{
		private const int DefaultBufSize = 32 * 1024;

        private readonly FileInfo	_file;
		private readonly int		_bufSize;

        public CmsProcessableFile(FileInfo file)
			: this(file, DefaultBufSize)
		{
		}

        public CmsProcessableFile(FileInfo file, int bufSize)
		{
			_file = file;
			_bufSize = bufSize;
		}

        public virtual Stream GetInputStream()
		{
			return new FileStream(_file.FullName, FileMode.Open, FileAccess.Read, FileShare.Read, _bufSize);
		}

        public virtual void Write(Stream zOut)
		{
			Stream inStr = this._file.OpenRead();
			Streams.PipeAll(inStr, zOut, this._bufSize);
            Platform.Dispose(inStr);
		}

        /// <returns>The file handle</returns>
		[Obsolete]
		public virtual object GetContent()
		{
			return _file;
		}
	}
}
#endif
#pragma warning restore
#endif
