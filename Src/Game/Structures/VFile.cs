using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using ICSharpCode.SharpZipLib.Zip;

namespace Game.Structures
{
    public class VFile : IComparable<VFile>
    {
        private readonly List<byte> _constData;
        private List<byte> _cache;

        public int Size { get; }
        public string Pak { get; }
        public string Path { get; }
        public string Name => Path.Split(VDirectory.PathSeparators).Last();

        public List<byte> Data
        {
            get
            {
                if (_constData != null)
                    return _constData;

                if (_cache == null)
                    _cache = ReadData().ToList();

                return _cache;
            }
        }

        public VFile(string file, string pak, List<byte> data)
        {
            Path = file;
            Pak = pak;
            _constData = data;
            Size = _constData.Count;
        }

        public VFile(string file, string pak, int size)
        {
            Path = file;
            Pak = pak;
            Size = size;
        }

        public byte[] ReadData()
        {
            var zipFile = new ZipFile(Pak);
            var entry = zipFile.GetEntry(Path);
            var stream = zipFile.GetInputStream(entry);

            var b1 = (byte) stream.ReadByte();
            var b2 = (byte) stream.ReadByte();

            if (b1 == 0x78 && b2 == 0x9C) // zlib default compression
                return UnGZip(stream);

            var buffer = new byte[entry.Size];
            buffer[0] = b1;
            buffer[1] = b2;

            stream.Read(buffer, 2, (int) entry.Size - 2);
            return buffer;
        }

        public byte[] UnGZip(Stream stream)
        {
            using (var stream2 = (Stream) new MemoryStream(67108882))
            {
                using (var deflateStream = new DeflateStream(stream, CompressionMode.Decompress))
                {
                    var buffer = new byte[32768];
                    int count;
                    while ((count = deflateStream.Read(buffer, 0, buffer.Length)) != 0)
                        stream2.Write(buffer, 0, count);
                }

                var b = new byte[stream2.Length];
                stream2.Position = 0L;
                stream2.Read(b, 0, (int) stream2.Length);

                return b;
            }
        }

        public void ClearCache()
        {
            if (_cache != null)
            {
                _cache.Clear();
                _cache = null;
            }
        }

        public int CompareTo(VFile other)
        {
            if (ReferenceEquals(this, other))
                return 0;
            if (ReferenceEquals(null, other))
                return 1;

            return string.Compare(Path, other.Path, StringComparison.Ordinal);
        }
    }
}