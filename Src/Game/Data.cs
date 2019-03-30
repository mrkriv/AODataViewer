using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using ICSharpCode.SharpZipLib.Checksums;
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
            DateTime d = DateTime.Now;

            string pak = "";
            long TotalSize = 0;
            int TotalFile = 0;
            File Loc = null;
            Files = new List<File>();

            ZipFile z = new ZipFile(path + "\\data\\Packs\\Version.pak");
            ZipEntry e = z.GetEntry("Version/DataPacksInfo.bin");
            Stream s = z.GetInputStream(e);

            byte[] buffer = new byte[e.Size];
            s.Read(buffer, 0, (int)e.Size);

            string[] str = Encoding.UTF8.GetString(buffer).Split('\n');

            foreach (string item in str)
            {
                if (item != "")
                {
                    string[] sub = item.Split('\t');
                    if (pak == "")
                    {
                        pak = sub[0];
                        TotalSize += int.Parse(sub[2]);
                        TotalFile += int.Parse(sub[1]);
                    }
                    else
                    {
                        File f = new File(sub[0], pak, int.Parse(sub[2]));
                        if (sub[0] == "Bin/pack.loc")
                            Loc = f;
                        else
                            Files.Add(f);
                    }
                }
                else
                    pak = "";
            }

            s.Close();
            z.Close();
            VerInfo.Path = path;
            VerInfo.TotalSize = TotalSize;
            VerInfo.TotalFile = TotalFile;

            Loc.ReadData(true);
            buffer = Loc.Data.GetRange(0, 16).ToArray();
            List<byte> loc = Loc.Data.GetRange(16, Loc.Data.Count - 16);

            int sizes_pos = BitConverter.ToInt32(buffer, 4);
            int item_count = BitConverter.ToInt32(buffer, 12);

            int i = 0;
            for (; i < item_count; i++)
            {
                byte[] item = loc.GetRange(i * 12, 12).ToArray();
                int pos = BitConverter.ToInt32(item, 0);
                int str_size = BitConverter.ToInt32(item, 4);
                int id = BitConverter.ToInt32(item, 8);

                byte[] fat_entry = loc.GetRange(sizes_pos + id * 8, 8).ToArray();
                int item_filesize = BitConverter.ToInt32(fat_entry, 0);
                int item_filepos = BitConverter.ToInt32(fat_entry, 4);

                string item_name = Encoding.UTF8.GetString(loc.GetRange(pos + 12 * id, str_size - 1).ToArray());
                
                File f = new File(item_name, "", 0);
                f.Data = loc.GetRange(8 + sizes_pos + item_filepos + item_count * 8, item_filesize * 2);
                Files.Add(f);
            }

            Loc.ClearCache();

            TimeSpan offest = DateTime.Now - d;
            EngineConsole.Instance.Print("Файловая система загружена " + offest.TotalSeconds.ToString("0.00") + "c");

            VerInfo.TotalVFile = i;
            VerInfo.LoadTime = offest.TotalSeconds;
        }

        public class File
        {
            string name;
            public int Size;
            public string Pak;
            public List<byte> Data;

            public string Name
            {
                get { return name; }
            }

            public File(string file, string pak, int size)
            {
                name = file;
                Pak = pak;
                Size = size;
            }

            public string GetOnlyName()
            {
                string[] str = name.Split('/');
                return str[str.Length - 1].Split('.')[0];
            }

            public void ReadData(bool unzip = false)
            {
                if (Data != null)
                    return;

                ZipFile z = new ZipFile(VerInfo.Path + "\\data\\Packs\\" + Pak);
                ZipEntry e = z.GetEntry(name);
                Stream s = z.GetInputStream(e);

                if (unzip)
                    UnGZip(s);
                else
                {
                    Data = new List<byte>();
                    byte[] buffer = new byte[e.Size];
                    s.Read(buffer, 0, (int)e.Size);

                    foreach (byte by in buffer)
                        Data.Add(by);
                }
            }

            public void UnGZip(Stream Stream)
            {
                Data = new List<byte>();
                Stream.ReadByte();
                Stream.ReadByte();
                Stream stream2 = (Stream)new MemoryStream(67108882);
                using (DeflateStream deflateStream = new DeflateStream(Stream, CompressionMode.Decompress))
                {
                    byte[] buffer = new byte[32768];
                    int count;
                    while ((count = deflateStream.Read(buffer, 0, buffer.Length)) != 0)
                        stream2.Write(buffer, 0, count);
                }
                byte[] b = new byte[stream2.Length];
                stream2.Position = 0L;
                stream2.Read(b, 0, (int)stream2.Length);

                foreach (byte by in b)
                    Data.Add(by);
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