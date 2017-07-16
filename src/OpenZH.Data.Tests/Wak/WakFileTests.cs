﻿using System.IO;
using OpenZH.Data.Wak;
using Xunit;
using Xunit.Abstractions;

namespace OpenZH.Data.Tests.Wak
{
    public class WakFileTests
    {
        private readonly ITestOutputHelper _output;

        public WakFileTests(ITestOutputHelper output)
        {
            _output = output;
        }

        [Fact]
        public void CanRoundtripWakFile()
        {
            InstalledFilesTestData.ReadFiles(".wak", _output, (fileName, openEntryStream) =>
            {
                byte[] originalUncompressedBytes;
                using (var originalUncompressedStream = new MemoryStream())
                using (var entryStream = openEntryStream())
                {
                    entryStream.CopyTo(originalUncompressedStream);
                    originalUncompressedBytes = originalUncompressedStream.ToArray();
                }

                WakFile wakFile;
                using (var entryStream = new MemoryStream(originalUncompressedBytes, false))
                {
                    wakFile = WakFile.Parse(entryStream);
                }

                byte[] serializedBytes;
                using (var serializedStream = new MemoryStream())
                {
                    wakFile.WriteTo(serializedStream);
                    serializedBytes = serializedStream.ToArray();
                }

                AssertUtility.Equal(originalUncompressedBytes, serializedBytes);
            });
        }

        [Fact]
        public void Test()
        {
            WakFile wakFile;
            using (var fileStream = File.OpenRead(@"Wak\Assets\Test.wak"))
            {
                wakFile = WakFile.Parse(fileStream);
            }

            Assert.Equal(5, wakFile.Entries.Length);

            Assert.Equal(0.0f, wakFile.Entries[0].StartX);
            Assert.Equal(1.0f, wakFile.Entries[0].StartY);
            Assert.Equal(2.0f, wakFile.Entries[0].EndX);
            Assert.Equal(3.0f, wakFile.Entries[0].EndY);
            Assert.Equal(WakEntryType.Pond, wakFile.Entries[0].EntryType);

            Assert.Equal(0.1f, wakFile.Entries[1].StartX);
            Assert.Equal(1.1f, wakFile.Entries[1].StartY);
            Assert.Equal(2.1f, wakFile.Entries[1].EndX);
            Assert.Equal(3.1f, wakFile.Entries[1].EndY);
            Assert.Equal(WakEntryType.Ocean, wakFile.Entries[1].EntryType);

            Assert.Equal(0.2f, wakFile.Entries[2].StartX);
            Assert.Equal(1.2f, wakFile.Entries[2].StartY);
            Assert.Equal(2.2f, wakFile.Entries[2].EndX);
            Assert.Equal(3.2f, wakFile.Entries[2].EndY);
            Assert.Equal(WakEntryType.CloseOcean, wakFile.Entries[2].EntryType);

            Assert.Equal(0.3f, wakFile.Entries[3].StartX);
            Assert.Equal(1.3f, wakFile.Entries[3].StartY);
            Assert.Equal(2.3f, wakFile.Entries[3].EndX);
            Assert.Equal(3.3f, wakFile.Entries[3].EndY);
            Assert.Equal(WakEntryType.CloseOceanDouble, wakFile.Entries[3].EntryType);

            Assert.Equal(0.4f, wakFile.Entries[4].StartX);
            Assert.Equal(1.4f, wakFile.Entries[4].StartY);
            Assert.Equal(2.4f, wakFile.Entries[4].EndX);
            Assert.Equal(3.4f, wakFile.Entries[4].EndY);
            Assert.Equal(WakEntryType.Radial, wakFile.Entries[4].EntryType);
        }
    }
}
