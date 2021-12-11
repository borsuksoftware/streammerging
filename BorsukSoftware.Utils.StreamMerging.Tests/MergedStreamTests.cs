using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using FluentAssertions;
using Xunit;

namespace BorsukSoftware.Utils.StreamMerging
{
    public class MergedStreamTests
    {
        [Fact]
        public void SimpleRead()
        {
            var byteArrays = Enumerable.Range(0, 10).
                Select(arrayCount =>
                {
                    var array = new byte[256];
                    for (int i = 0; i < array.Length; ++i)
                        array[i] = (byte)i;

                    return array;
                }).
                ToList();

            var sourceStreams = byteArrays.Select(array => new System.IO.MemoryStream(array));

            var mergedStream = new MergedStream(sourceStreams);

            byte[] outputBuffer = new byte[376];
            var bytesRead = mergedStream.Read(outputBuffer, 0, outputBuffer.Length);

            Assert.Equal(outputBuffer.Length, bytesRead);

            outputBuffer.Should().BeEquivalentTo(Enumerable.Range(0, outputBuffer.Length).Select(i => (byte)(i % 256)));
        }

        [Fact]
        public async Task SimpleAsyncRead()
        {
            var byteArrays = Enumerable.Range(0, 10).
                Select(arrayCount =>
                {
                    var array = new byte[256];
                    for (int i = 0; i < array.Length; ++i)
                        array[i] = (byte)i;

                    return array;
                }).
                ToList();

            var sourceStreams = byteArrays.Select(array => new System.IO.MemoryStream(array));

            var mergedStream = new MergedStream(sourceStreams);

            byte[] outputBuffer = new byte[376];
            var bytesRead = await mergedStream.ReadAsync(outputBuffer, 0, outputBuffer.Length);

            Assert.Equal(outputBuffer.Length, bytesRead);

            outputBuffer.Should().BeEquivalentTo(Enumerable.Range(0, outputBuffer.Length).Select(i => (byte)(i % 256)));
        }


        [Fact]
        public void StringRoundTrip()
        {
            int sourceCount = 10;
            int sourceRowCount = 50;
            var expectedLines = Enumerable.Range(0, sourceCount + 2).
                SelectMany(chunkIdx =>
                {
                    if (chunkIdx >= sourceCount)
                        throw new InvalidOperationException();
                    else
                        return Enumerable.Range(0, sourceRowCount).Select(lineNum => $"Chunk #{chunkIdx}, line #{lineNum}");
                });

            byte[][] sourceByteArrays = new byte[sourceCount][];
            for (int i = 0; i < sourceCount; ++i)
            {
                using (var memStream = new System.IO.MemoryStream())
                {
                    using (var streamWriter = new System.IO.StreamWriter(memStream))
                    {
                        for (int j = 0; j < sourceRowCount; ++j)
                        {
                            streamWriter.WriteLine($"Chunk #{i}, line #{j}");
                        }

                        streamWriter.Flush();

                        sourceByteArrays[i] = new byte[memStream.Position];
                        memStream.Position = 0;
                        var bytesRead = memStream.Read(sourceByteArrays[i], 0, sourceByteArrays[i].Length);

                        Assert.Equal(sourceByteArrays[i].Length, bytesRead);
                    }
                }
            }

            var sourceStreams = sourceByteArrays.Select(array => new System.IO.MemoryStream(array));
            var mergedStream = new MergedStream(sourceStreams);

            int readCount = 0;
            using (var streamReader = new System.IO.StreamReader(mergedStream))
            {
                var expectedLineEnumerator = expectedLines.GetEnumerator();
                while(readCount < sourceCount * sourceRowCount)
                {
                    expectedLineEnumerator.MoveNext();
                    var readLine = streamReader.ReadLine();
                    Assert.Equal(expectedLineEnumerator.Current, readLine);
                    ++readCount;
                }

                Assert.Throws<InvalidOperationException>(() => expectedLineEnumerator.MoveNext());
            }

            Assert.Equal(sourceCount * sourceRowCount, readCount);
        }

        [Fact]
        public void StringRoundTripWithCompressedIntermediateStreams()
        {
            int sourceCount = 10;
            int sourceRowCount = 50;
            var expectedLines = Enumerable.Range(0, sourceCount + 2).
                SelectMany(chunkIdx =>
                {
                    if (chunkIdx >= sourceCount)
                        throw new InvalidOperationException();
                    else
                        return Enumerable.Range(0, sourceRowCount).Select(lineNum => $"Chunk #{chunkIdx}, line #{lineNum}");
                });

            byte[][] sourceByteArrays = new byte[sourceCount][];
            for (int i = 0; i < sourceCount; ++i)
            {
                using (var memStream = new System.IO.MemoryStream())
                {
                    using( var compressedStream= new System.IO.Compression.DeflateStream(memStream, System.IO.Compression.CompressionLevel.Optimal))
                    {
                        using (var streamWriter = new System.IO.StreamWriter(compressedStream))
                        {
                            for (int j = 0; j < sourceRowCount; ++j)
                            {
                                streamWriter.WriteLine($"Chunk #{i}, line #{j}");
                            }

                            streamWriter.Flush();
                            compressedStream.Flush();

                            sourceByteArrays[i] = new byte[memStream.Position];
                            memStream.Position = 0;
                            var bytesRead = memStream.Read(sourceByteArrays[i], 0, sourceByteArrays[i].Length);

                            Assert.Equal(sourceByteArrays[i].Length, bytesRead);
                        }
                    }
                }
            }

            var sourceStreams = sourceByteArrays.Select(array => new System.IO.Compression.DeflateStream(new System.IO.MemoryStream(array), System.IO.Compression.CompressionMode.Decompress));
            var mergedStream = new MergedStream(sourceStreams);

            int readCount = 0;
            using (var streamReader = new System.IO.StreamReader(mergedStream))
            {
                var expectedLineEnumerator = expectedLines.GetEnumerator();
                while (readCount < sourceCount * sourceRowCount)
                {
                    expectedLineEnumerator.MoveNext();
                    var readLine = streamReader.ReadLine();
                    Assert.Equal(expectedLineEnumerator.Current, readLine);
                    ++readCount;
                }

                Assert.Throws<InvalidOperationException>(() => expectedLineEnumerator.MoveNext());
            }

            Assert.Equal(sourceCount * sourceRowCount, readCount);
        }
    }
}
