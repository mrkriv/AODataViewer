using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Game.Windows.Dialogs;
using ICSharpCode.SharpZipLib.Zip;
using ProjectCommon;

namespace Game.Structures
{
    public class Data
    {
        private readonly BackgroundWorker _backgroundWorker;
        public readonly VDirectory RootDirectory;

        public Data(string path, BackgroundWorker backgroundWorker)
        {
            _backgroundWorker = backgroundWorker;
            var loadBeginDate = DateTime.Now;

            VerInfoDialog.TotalSize = 0;
            VerInfoDialog.TotalFile = 0;
            VerInfoDialog.TotalVFile = 0;

            RootDirectory = new VDirectory("");

            LoadFs(path, "data");

            if (Directory.Exists(path + "\\data_warp"))
            {
                LoadFs(path, "data_warp");
            }

            LoadTexts();

            var offest = DateTime.Now - loadBeginDate;
            EngineConsole.Instance.Print("Файловая система загружена " + offest.TotalSeconds.ToString("0.00") + "c");

            VerInfoDialog.LoadTime = offest.TotalSeconds;
            
            _backgroundWorker.ReportProgress(0, null);
        }

        private void LoadFs(string path, string dataFolder)
        {
            foreach (var packFile in Directory.GetFiles(path + "\\" + dataFolder + "\\Packs", "*.pak"))
            {
                _backgroundWorker.ReportProgress(0, Path.GetFileName(packFile));
                
                using (var archive = new ZipFile(packFile))
                {
                    foreach (ZipEntry entry in archive)
                    {
                        var file = new VFile(entry.Name, packFile, (int) entry.Size);
                        RootDirectory.AddFile(file);

                        VerInfoDialog.TotalSize += entry.Size;
                        VerInfoDialog.TotalFile++;
                    }
                }
            }
        }

        private void LoadTexts()
        {
            var locFiles = RootDirectory.FindFile(".loc").ToList();
            var hashs = new List<string>();

            var sha256 = SHA256.Create();

            foreach (var loc in locFiles)
            {
                var buffer = loc.Data.ToArray();
                
                var hash = string.Concat(sha256.ComputeHash(buffer).Select(x => $"{x:X2}"));

                if (hashs.Contains(hash))
                    continue;

                hashs.Add(hash);
                
                var data = loc.Data.GetRange(16, loc.Data.Count - 16);

                var sizesPos = BitConverter.ToInt32(buffer, 4);
                var itemCount = BitConverter.ToInt32(buffer, 12);

                var pakName = loc.Pak + "/" + loc.Path.Replace('/', '\\');
                _backgroundWorker.ReportProgress(0, loc.Name);
                
                for (var i = 0; i < itemCount; i++)
                {
                    var item = data.GetRange(i * 12, 12).ToArray();
                    var pos = BitConverter.ToInt32(item, 0);
                    var strSize = BitConverter.ToInt32(item, 4);
                    var id = BitConverter.ToInt32(item, 8);

                    var fatEntry = data.GetRange(sizesPos + id * 8, 8).ToArray();
                    var itemFilesize = BitConverter.ToInt32(fatEntry, 0);
                    var itemFilepos = BitConverter.ToInt32(fatEntry, 4);

                    var itemName = Encoding.UTF8.GetString(data.GetRange(pos + 12 * id, strSize - 1).ToArray());
                    var fileData = data.GetRange(8 + sizesPos + itemFilepos + itemCount * 8, itemFilesize * 2);

                    RootDirectory.AddFile(new VFile(itemName, pakName, fileData));

                    VerInfoDialog.TotalVFile++;
                }

                loc.ClearCache();
            }
        }

        public static byte[] SubArray(byte[] data, int index, int length)
        {
            var result = new byte[length];
            Array.Copy(data, index, result, 0, length);
            return result;
        }
    }
}