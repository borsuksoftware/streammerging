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
        private Xunit.Abstractions.ITestOutputHelper _testOutputHelper;

        public MergedStreamTests(Xunit.Abstractions.ITestOutputHelper testOutputHelper)
        {
            _testOutputHelper = testOutputHelper;
        }

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

        [Fact]
        public void VeryLargeStringRoundTrip()
        {
            int expectedSize = 2_731_902;
            var sb = new StringBuilder();
            for (int i = 0; i < expectedSize; ++i)
            {
                sb.Append((char)('a' + (i % 26)));
            }

            var sourceString = sb.ToString();
            Assert.Equal(expectedSize, sourceString.Length);

            byte[] sourceByteArray;
            using (var memStream = new System.IO.MemoryStream())
            {
                using (var streamWriter = new System.IO.StreamWriter(memStream))
                {
                    streamWriter.Write(sourceString);

                    streamWriter.Flush();

                    sourceByteArray = new byte[memStream.Position];
                    memStream.Position = 0;
                    var bytesRead = memStream.Read(sourceByteArray, 0, sourceByteArray.Length);

                    Assert.Equal(sourceByteArray.Length, bytesRead);
                }
            }

            var sourceStreams = new[] { new System.IO.MemoryStream(sourceByteArray) };
            var mergedStream = new MergedStream(sourceStreams);

            using (var streamReader = new System.IO.StreamReader(mergedStream))
            {
                var readLine = streamReader.ReadToEnd();

                Assert.Equal(expectedSize, readLine.Length);
                Assert.Equal(sourceString, readLine);
            }
        }

        [Fact]
        public async Task StringRoundTripAsync()
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
                while (readCount < sourceCount * sourceRowCount)
                {
                    expectedLineEnumerator.MoveNext();
                    var readLine = await streamReader.ReadLineAsync();
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
                    using (var compressedStream = new System.IO.Compression.DeflateStream(memStream, System.IO.Compression.CompressionLevel.Optimal))
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


        [Fact]
        public void StringRoundTripWithCompressedIntermediateStreamsAsync_Sync()
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
                    using (var compressedStream = new System.IO.Compression.DeflateStream(memStream, System.IO.Compression.CompressionLevel.Optimal, leaveOpen: true))
                    using (var streamWriter = new System.IO.StreamWriter(compressedStream, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false), leaveOpen: true))
                    {
                        for (int j = 0; j < sourceRowCount; ++j)
                            streamWriter.WriteLine($"Chunk #{i}, line #{j}");
                    }
                    sourceByteArrays[i] = memStream.ToArray();
                }
            }

            var sourceStreams = sourceByteArrays.Select(array =>
                (System.IO.Stream)new System.IO.Compression.DeflateStream(
                    new System.IO.MemoryStream(array),
                    System.IO.Compression.CompressionMode.Decompress));

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

        [Fact]
        public void VeryLargeStringRoundTrip_SingleCompressedChunk()
        {
            int expectedSize = 1_500_000;
            var sb = new StringBuilder();
            for (int i = 0; i < expectedSize; ++i)
                sb.Append((char)('a' + (i % 26)));

            var sourceString = sb.ToString();
            Assert.Equal(expectedSize, sourceString.Length);

            byte[] compressedBytes;
            using (var memStream = new System.IO.MemoryStream())
            {
                using (var compressedStream = new System.IO.Compression.DeflateStream(memStream, System.IO.Compression.CompressionLevel.Optimal, leaveOpen: true))
                using (var streamWriter = new System.IO.StreamWriter(compressedStream, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false), leaveOpen: true))
                {
                    streamWriter.Write(sourceString);
                }
                compressedBytes = memStream.ToArray();
            }

            var decompressionStream = new System.IO.Compression.DeflateStream(
                new System.IO.MemoryStream(compressedBytes),
                System.IO.Compression.CompressionMode.Decompress);

            var mergedStream = new MergedStream(new[] { decompressionStream });

            string result;
            using (var streamReader = new System.IO.StreamReader(mergedStream))
            {
                result = streamReader.ReadToEnd();
            }

            Assert.Equal(expectedSize, result.Length);
            Assert.Equal(sourceString, result);
        }

        /// <summary>
        /// Regression test: async reads from a single compressed chunk should return all data.
        /// Previously, <see cref="MergedStream.ReadAsync"/> would treat a partial read from a
        /// <see cref="System.IO.Compression.DeflateStream"/> as stream exhaustion and silently
        /// drop the remaining bytes.
        /// </summary>
        [Fact]
        public async Task VeryLargeStringRoundTrip_Async_SingleCompressedChunk()
        {
            int expectedSize = 1_500_000;
            var sb = new StringBuilder();
            for (int i = 0; i < expectedSize; ++i)
                sb.Append((char)('a' + (i % 26)));

            var sourceString = sb.ToString();
            Assert.Equal(expectedSize, sourceString.Length);

            byte[] compressedBytes;
            using (var memStream = new System.IO.MemoryStream())
            {
                using (var compressedStream = new System.IO.Compression.DeflateStream(memStream, System.IO.Compression.CompressionLevel.Optimal, leaveOpen: true))
                using (var streamWriter = new System.IO.StreamWriter(compressedStream, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false), leaveOpen: true))
                {
                    streamWriter.Write(sourceString);
                }
                compressedBytes = memStream.ToArray();
            }

            var decompressionStream = new System.IO.Compression.DeflateStream(
                new System.IO.MemoryStream(compressedBytes),
                System.IO.Compression.CompressionMode.Decompress);

            var mergedStream = new MergedStream(new[] { decompressionStream });

            string result;
            using (var outputStream = new System.IO.MemoryStream())
            {
                await mergedStream.CopyToAsync(outputStream);
                result = Encoding.UTF8.GetString(outputStream.ToArray());
            }

            Assert.Equal(expectedSize, result.Length);
            Assert.Equal(sourceString, result);
        }

        /// <summary>
        /// Regression test: async reads across multiple compressed chunks should return all data
        /// and correctly advance to the next stream when one is exhausted.
        /// </summary>
        [Fact]
        public async Task StringRoundTrip_Async_MultipleCompressedChunks()
        {
            int sourceCount = 10;
            int sourceRowCount = 50;

            var expectedLines = Enumerable.Range(0, sourceCount)
                .SelectMany(chunkIdx => Enumerable.Range(0, sourceRowCount)
                    .Select(lineNum => $"Chunk #{chunkIdx}, line #{lineNum}"))
                .ToList();

            byte[][] sourceByteArrays = new byte[sourceCount][];
            for (int i = 0; i < sourceCount; ++i)
            {
                using (var memStream = new System.IO.MemoryStream())
                {
                    using (var compressedStream = new System.IO.Compression.DeflateStream(memStream, System.IO.Compression.CompressionLevel.Optimal, leaveOpen: true))
                    using (var streamWriter = new System.IO.StreamWriter(compressedStream, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false), leaveOpen: true))
                    {
                        for (int j = 0; j < sourceRowCount; ++j)
                            streamWriter.WriteLine($"Chunk #{i}, line #{j}");
                    }
                    sourceByteArrays[i] = memStream.ToArray();
                }
            }

            var sourceStreams = sourceByteArrays.Select(array =>
                (System.IO.Stream)new System.IO.Compression.DeflateStream(
                    new System.IO.MemoryStream(array),
                    System.IO.Compression.CompressionMode.Decompress));

            var mergedStream = new MergedStream(sourceStreams);

            string result;
            using (var outputStream = new System.IO.MemoryStream())
            {
                await mergedStream.CopyToAsync(outputStream);
                result = Encoding.UTF8.GetString(outputStream.ToArray());
            }

            var actualLines = result.Split('\n')
                .Select(l => l.TrimEnd('\r'))
                .Where(l => l.Length > 0)
                .ToList();

            Assert.Equal(expectedLines.Count, actualLines.Count);
            actualLines.Should().BeEquivalentTo(expectedLines, options => options.WithStrictOrdering());
        }

        /// <summary>
        /// Regression test: async reads with a very large single-line uncompressed payload
        /// should return all data (mirrors <see cref="VeryLargeStringRoundTrip"/> for the async path).
        /// </summary>
        [Fact]
        public async Task VeryLargeStringRoundTrip_Async_SingleUncompressedChunk()
        {
            int expectedSize = 2_731_902;
            var sb = new StringBuilder();
            for (int i = 0; i < expectedSize; ++i)
                sb.Append((char)('a' + (i % 26)));

            var sourceString = sb.ToString();
            Assert.Equal(expectedSize, sourceString.Length);

            var sourceBytes = Encoding.UTF8.GetBytes(sourceString);
            var mergedStream = new MergedStream(new[] { new System.IO.MemoryStream(sourceBytes) });

            string result;
            using (var outputStream = new System.IO.MemoryStream())
            {
                await mergedStream.CopyToAsync(outputStream);
                result = Encoding.UTF8.GetString(outputStream.ToArray());
            }

            Assert.Equal(expectedSize, result.Length);
            Assert.Equal(sourceString, result);
        }

        /// <summary>
        /// Test to reflect a use case which failed during a migration from .net5.0 to .net6.0
        /// </summary>
        /// <remarks>The code in question was reading in a set of serialized streams from disc but some characters were missing after the migration to .net6.0</remarks>
        [Fact]
        public void DeserializingCompressedStreams()
        {
            var resourceStreams = typeof(MergedStreamTests).Assembly.GetManifestResourceNames().
                Where(n => n.Contains("Resources.CompressedFiles.chunk-")).
                OrderBy(n => n.Split("-").Last()).
                Select(n => typeof(MergedStreamTests).Assembly.GetManifestResourceStream(n));

            var decompressionStreams = resourceStreams.Select(s => new System.IO.Compression.DeflateStream(s, System.IO.Compression.CompressionMode.Decompress));

            var readLines = new List<string>(500);

            using (var mergedStream = new MergedStream(decompressionStreams))
            {
                using (var streamReader = new System.IO.StreamReader(mergedStream))
                {
                    while (!streamReader.EndOfStream)
                    {
                        var line = streamReader.ReadLine();
                        readLines.Add(line);

                        _testOutputHelper.WriteLine(line);
                    }
                }
            }

            readLines.Should().BeEquivalentTo(Enumerable.Range(1, 500).Select(i => String.Join("\t", Enumerable.Range(1, 32).Select(j => $"Cell {i}-{j}"))));
        }

        [Fact]
        public async Task DeserializingCompressedStreamsAsync()
        {
            var resourceStreams = typeof(MergedStreamTests).Assembly.GetManifestResourceNames().
                Where(n => n.Contains("Resources.CompressedFiles.chunk-")).
                OrderBy(n => n.Split("-").Last()).
                Select(n => typeof(MergedStreamTests).Assembly.GetManifestResourceStream(n));

            var decompressionStreams = resourceStreams.Select(s => new System.IO.Compression.DeflateStream(s, System.IO.Compression.CompressionMode.Decompress));

            var readLines = new List<string>(500);

            using (var mergedStream = new MergedStream(decompressionStreams))
            {
                using (var streamReader = new System.IO.StreamReader(mergedStream))
                {
                    while (!streamReader.EndOfStream)
                    {
                        var line = await streamReader.ReadLineAsync();
                        readLines.Add(line);

                        _testOutputHelper.WriteLine(line);
                    }
                }
            }

            readLines.Should().BeEquivalentTo(Enumerable.Range(1, 500).Select(i => String.Join("\t", Enumerable.Range(1, 32).Select(j => $"Cell {i}-{j}"))));
        }
    }
}
