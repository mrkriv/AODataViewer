using System;
using System.Collections.Generic;
using System.Text;
using ICSharpCode.SharpZipLib.Zip;
using System.IO;
using System.IO.Compression;
using ProjectCommon;

namespace Game
{
    public class Data
    {
        public List<File> Files;

        public Data(string path)
        {
            var loadBeginDate = DateTime.Now;

            VerInfo.TotalSize = 0;
            VerInfo.TotalFile = 0;
            VerInfo.TotalVFile = 0;
            VerInfo.Path = path;

            Files = new List<File>();

            LoadDataDirectory(path, "data");
            LoadTextsPack("data");

            if (Directory.Exists(path + "\\data_warp"))
            {
                LoadTextsPack("data_warp");
            }

            //Files.Sort((a, b) => a.Name.CompareTo(b.Name));

            var offest = DateTime.Now - loadBeginDate;
            EngineConsole.Instance.Print("Файловая система загружена " + offest.TotalSeconds.ToString("0.00") + "c");

            VerInfo.LoadTime = offest.TotalSeconds;
        }

        private void LoadDataDirectory(string path, string dataFolder)
        {
            var buffer = ReadFromZip(path + "\\" + dataFolder + "\\Packs\\Version.pak", "Version/DataPacksInfo.bin");

            var pak = "";
            var str = Encoding.UTF8.GetString(buffer).Split('\n');
            var folder = "\\" + dataFolder + "\\Packs\\";

            foreach (var item in str)
            {
                if (item == "")
                {
                    pak = "";
                    continue;
                }

                var sub = item.Split('\t');
                if (pak == "")
                {
                    pak = sub[0];
                    VerInfo.TotalSize += int.Parse(sub[2]);
                    VerInfo.TotalFile += int.Parse(sub[1]);
                }
                else
                {
                    var localPathToPak = folder + pak;
                    var f = new File(sub[0], localPathToPak, int.Parse(sub[2]));
                    Files.Add(f);
                }
            }
        }

        private void LoadTextsPack(string dataFolder)
        {
            var Loc = new File("Bin/pack.loc", dataFolder + "\\Packs\\Texts.pak", 0);

            Loc.ReadData(true);
            var buffer = Loc.Data.GetRange(0, 16).ToArray();
            var loc = Loc.Data.GetRange(16, Loc.Data.Count - 16);

            var sizes_pos = BitConverter.ToInt32(buffer, 4);
            var item_count = BitConverter.ToInt32(buffer, 12);

            for (var i = 0; i < item_count; i++)
            {
                var item = loc.GetRange(i * 12, 12).ToArray();
                var pos = BitConverter.ToInt32(item, 0);
                var str_size = BitConverter.ToInt32(item, 4);
                var id = BitConverter.ToInt32(item, 8);

                var fat_entry = loc.GetRange(sizes_pos + id * 8, 8).ToArray();
                var item_filesize = BitConverter.ToInt32(fat_entry, 0);
                var item_filepos = BitConverter.ToInt32(fat_entry, 4);

                var item_name = Encoding.UTF8.GetString(loc.GetRange(pos + 12 * id, str_size - 1).ToArray());

                var f = new File(item_name, "", 0);
                f.Data = loc.GetRange(8 + sizes_pos + item_filepos + item_count * 8, item_filesize * 2);
                Files.Add(f);

                VerInfo.TotalVFile++;
            }

            Loc.ClearCache();
        }

        private byte[] ReadFromZip(string pathToArchive, string pathToFile)
        {
            using (var archive = new ZipFile(pathToArchive))
            {
                var file = archive.GetEntry(pathToFile);
                using (var s = archive.GetInputStream(file))
                {
                    var buffer = new byte[file.Size];
                    s.Read(buffer, 0, (int)file.Size);

                    return buffer;
                }
            }
        }

        public class File
        {
            public List<byte> Data;

            public int Size { get; }
            public string Pak { get; }
            public string Name { get; }

            public File(string file, string pak, int size)
            {
                Name = file;
                Pak = pak;
                Size = size;
            }

            public string GetOnlyName()
            {
                var str = Name.Split('/');
                return str[str.Length - 1].Split('.')[0];
            }

            public void ReadData(bool unzip = false)
            {
                if (Data != null)
                    return;

                var z = new ZipFile(VerInfo.Path + "\\" + Pak);
                var e = z.GetEntry(Name);
                var s = z.GetInputStream(e);

                if (unzip)
                    UnGZip(s);
                else
                {
                    Data = new List<byte>();
                    var buffer = new byte[e.Size];
                    s.Read(buffer, 0, (int)e.Size);

                    foreach (var by in buffer)
                        Data.Add(by);
                }
            }

            public void UnGZip(Stream stream)
            {
                Data = new List<byte>();

                stream.ReadByte();
                stream.ReadByte();

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

                    foreach (var by in b)
                        Data.Add(by);
                }
            }

            public void ClearCache()
            {
                if (Data != null)
                {
                    Data.Clear();
                    Data = null;
                }
            }
        }
    }
}