#if !BESTHTTP_DISABLE_ALTERNATE_SSL && (!UNITY_WEBGL || UNITY_EDITOR)
#pragma warning disable
namespace BestHTTP.SecureProtocol.Org.BouncyCastle.Utilities.IO.Pem
{
	using System;
	using System.Collections;
	using System.IO;
	using BestHTTP.SecureProtocol.Org.BouncyCastle.Utilities.Encoders;

	public class PemReader
	{		
		private readonly TextReader   reader;
		private readonly MemoryStream buffer;
		private readonly StreamWriter textBuffer;
		private readonly IList        pushback = Platform.CreateArrayList();
		private          int          c;

		

		public PemReader(TextReader reader)
		{
			if (reader == null)
				throw new ArgumentNullException("reader");


			this.buffer     = new MemoryStream();
			this.textBuffer = new StreamWriter(this.buffer);

			this.reader = reader;
		}

		public TextReader Reader
		{
			get { return reader; }
		}


		/// <returns>
		/// A <see cref="PemObject"/>
		/// </returns>
		/// <exception cref="IOException"></exception>	
		public PemObject ReadPemObject()
		{
			//
			// Look for BEGIN
			//

			for (;;)
			{
				// Seek a leading dash, ignore anything up to that point.
				if (!this.seekDash())
					// There are no pem objects here.
					return null;


				// consume dash [-----]BEGIN ...
				if (!this.consumeDash()) throw new IOException("no data after consuming leading dashes");


				this.skipWhiteSpace();


				if (!this.expect("BEGIN")) continue;

				break;
			}


			this.skipWhiteSpace();

			//
			// Consume type, accepting whitespace
			//

			if (!this.bufferUntilStopChar('-', false)) throw new IOException("ran out of data before consuming type");

			var type = this.bufferedString().Trim();


			// Consume dashes after type.

			if (!this.consumeDash()) throw new IOException("ran out of data consuming header");

			this.skipWhiteSpace();


			//
			// Read ahead looking for headers.
			// Look for a colon for up to 64 characters, as an indication there might be a header.
			//

			IList headers = Platform.CreateArrayList();

			while (this.seekColon(64))
			{
				if (!this.bufferUntilStopChar(':', false)) throw new IOException("ran out of data reading header key value");

				var key = this.bufferedString().Trim();


				this.c = this.Read();
				if (this.c != ':') throw new IOException("expected colon");


				//
				// We are going to look for well formed headers, if they do not end with a "LF" we cannot
				// discern where they end.
				//

				if (!this.bufferUntilStopChar('\n', false)) // Now read to the end of the line.
					throw new IOException("ran out of data before consuming header value");

				this.skipWhiteSpace();

				var value = this.bufferedString().Trim();
				headers.Add(new PemHeader(key, value));
			}


			//
			// Consume payload, ignoring all white space until we encounter a '-'
			//

			this.skipWhiteSpace();

			if (!this.bufferUntilStopChar('-', true)) throw new IOException("ran out of data before consuming payload");

			var payload = this.bufferedString();

			// Seek the start of the end.
			if (!this.seekDash())
			{
				throw new IOException("did not find leading '-'");
			}

			if (!this.consumeDash())
			{
				throw new IOException("no data after consuming trailing dashes");
			}

			if (!this.expect("END " + type)) throw new IOException("END " + type + " was not found.");


			if (!this.seekDash()) throw new IOException("did not find ending '-'");


			// consume trailing dashes.
			this.consumeDash();


			return new PemObject(type, headers, Base64.Decode(payload));
		}


		private string bufferedString()
		{
			this.textBuffer.Flush();
			var value = Strings.FromUtf8ByteArray(this.buffer.ToArray());
			this.buffer.Position = 0;
			this.buffer.SetLength(0);
			return value;
		}


		private bool seekDash()
		{
			this.c = 0;
			while ((this.c = this.Read()) >= 0)
				if (this.c == '-')
					break;

			this.PushBack(this.c);

			return this.c == '-';
		}


		/// <summary>
		///     Seek ':" up to the limit.
		/// </summary>
		/// <param name="upTo"></param>
		/// <returns></returns>
		private bool seekColon(int upTo)
		{
			this.c = 0;
			var colonFound = false;
			var read       = Platform.CreateArrayList();

			for (; upTo >= 0 && this.c >= 0; upTo--)
			{
				this.c = this.Read();
				read.Add(this.c);
				if (this.c == ':')
				{
					colonFound = true;
					break;
				}
			}

			while (read.Count > 0)
			{
				this.PushBack((int)read[read.Count - 1]);
				read.RemoveAt(read.Count - 1);
			}

			return colonFound;
		}


		/// <summary>
		///     Consume the dashes
		/// </summary>
		/// <returns></returns>
		private bool consumeDash()
		{
			this.c = 0;
			while ((this.c = this.Read()) >= 0)
				if (this.c != '-')
					break;

			this.PushBack(this.c);

			return this.c != -1;
		}

		/// <summary>
		///     Skip white space leave char in stream.
		/// </summary>
		private void skipWhiteSpace()
		{
			while ((this.c = this.Read()) >= 0)
				if (this.c > ' ')
					break;
			this.PushBack(this.c);
		}

		/// <summary>
		///     Read forward consuming the expected string.
		/// </summary>
		/// <param name="value">expected string</param>
		/// <returns>false if not consumed</returns>
		private bool expect(string value)
		{
			for (var t = 0; t < value.Length; t++)
			{
				this.c = this.Read();
				if (this.c == value[t])
					continue;
				return false;
			}

			return true;
		}


		/// <summary>
		///     Consume until dash.
		/// </summary>
		/// <returns>true if stream end not met</returns>
		private bool bufferUntilStopChar(char stopChar, bool skipWhiteSpace)
		{
			while ((this.c = this.Read()) >= 0)
			{
				if (skipWhiteSpace && this.c <= ' ') continue;

				if (this.c != stopChar)
				{
					this.textBuffer.Write((char)this.c);
					this.textBuffer.Flush();
				}
				else
				{
					this.PushBack(this.c);
					break;
				}
			}

			return this.c > -1;
		}


		private void PushBack(int value)
		{
			if (this.pushback.Count == 0)
				this.pushback.Add(value);
			else
				this.pushback.Insert(0, value);
		}


		private int Read()
		{
			if (this.pushback.Count > 0)
			{
				var i = (int)this.pushback[0];
				this.pushback.RemoveAt(0);
				return i;
			}

			return this.reader.Read();
		}




	}
}
#pragma warning restore
#endif
