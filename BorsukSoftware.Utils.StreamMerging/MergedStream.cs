using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace BorsukSoftware.Utils.StreamMerging
{
    /// <summary>
    /// Stream class which allows an enumeration of streams to be passed in and opened for reading as and when necessary
    /// </summary>
    /// <remarks>This class can be useful when there are a set of source streams which need to be consumed as a single item, e.g.
    /// When there are a series of chunks for a large document stored independently on disc, and a consumer wishes to be able
    /// to process the entire document without having to pay a large additional memory profile.</remarks>
    public class MergedStream : System.IO.Stream
    {
        #region Member variables

        private IEnumerator<System.IO.Stream> _streamEnumerator;
        private long _currentPosition;
        private bool _streamEnumeratorEndReached = false;

        #endregion

        #region Data Model

        /// <summary>
        /// Gets / sets whether or not to call <see cref="Stream.Dispose"/> on the underlying streams once they've been read
        /// </summary>
        public bool DisposeUnderlyingStreams { get; set; } = true;

        #endregion

        #region Construction Logic

        /// <summary>
        /// Create a new instance of the <see cref="MergedStream"/> class based off the supplied streams.
        /// </summary>
        /// <param name="streams">The set of streams to be merged</param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="streams"/> is null</exception>
        public MergedStream(IEnumerable<System.IO.Stream> streams)
        {
            if (streams == null)
                throw new ArgumentNullException(nameof(streams));

            _streamEnumerator = streams.GetEnumerator();
            _streamEnumeratorEndReached = !_streamEnumerator.MoveNext();
        }

        #endregion

        #region Stream overrides

        public override void Flush()
        {
            throw new NotImplementedException();
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            if (_streamEnumeratorEndReached)
                return 0;

            Span<byte> bufferAsSpan = new Span<byte>(buffer, offset, count);
            return this.Read(bufferAsSpan);
        }

        public override int Read(Span<byte> buffer)
        {
            if (_streamEnumeratorEndReached)
                return 0;

            var count = buffer.Length;
            // Handle simple case
            int totalBytesRead = 0;
            while (true)
            {
                int bytesToReadInCall = count - totalBytesRead;

                var subBuffer = buffer.Slice(totalBytesRead);
                int bytesRead = _streamEnumerator.Current.Read(subBuffer);

                totalBytesRead += bytesRead;
                if (totalBytesRead == count || bytesRead == bytesToReadInCall)
                {
                    _currentPosition += totalBytesRead;
                    return totalBytesRead;
                }

                if (this.DisposeUnderlyingStreams)
                    _streamEnumerator.Current.Dispose();

                if (!_streamEnumerator.MoveNext())
                {
                    _streamEnumeratorEndReached = true;
                    _currentPosition += totalBytesRead;
                    return totalBytesRead;
                }
            }
        }

        public async override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            var response = await ReadAsync(new Memory<byte>(buffer, offset, count), cancellationToken);
            return response;
        }

        public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
        {
            if (_streamEnumeratorEndReached)
                return 0;

            var count = buffer.Length;
            // Handle simple case
            int totalBytesRead = 0;
            while (true)
            {
                int bytesToReadInCall = count - totalBytesRead;

                var subBuffer = buffer.Slice(totalBytesRead);
                int bytesRead = await _streamEnumerator.Current.ReadAsync(subBuffer);

                totalBytesRead += bytesRead;
                if (totalBytesRead == count || bytesRead == bytesToReadInCall)
                {
                    _currentPosition += totalBytesRead;
                    return totalBytesRead;
                }

                if (this.DisposeUnderlyingStreams)
                    _streamEnumerator.Current.Dispose();

                if (!_streamEnumerator.MoveNext())
                {
                    _streamEnumeratorEndReached = true;
                    _currentPosition += totalBytesRead;
                    return totalBytesRead;
                }
            }
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
            throw new NotImplementedException();
        }

        public override bool CanRead => true;

        public override bool CanSeek => false;

        public override bool CanWrite => false;

        public override long Length => throw new NotImplementedException();

        public override long Position { get => _currentPosition; set => throw new NotImplementedException(); }

        #endregion
    }
}