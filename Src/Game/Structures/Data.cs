using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Game.Windows.Dialogs;
using ICSharpCode.SharpZipLib.Zip;
using ProjectCommon;

namespace Game.Structures
{
    public class Data
    {
        public readonly VDirectory RootDirectory;
        private readonly List<VFile> _locFiles;

        public Data(string path)
        {
            var loadBeginDate = DateTime.Now;

            VerInfoDialog.TotalSize = 0;
            VerInfoDialog.TotalFile = 0;
            VerInfoDialog.TotalVFile = 0;
            VerInfoDialog.Path = path;

            RootDirectory = new VDirectory("");
            _locFiles = new List<VFile>();
            
            LoadFs(path, "data");

            if (Directory.Exists(path + "\\data_warp"))
            {
                LoadFs(path, "data_warp");
            }

            LoadTexts();

            var offest = DateTime.Now - loadBeginDate;
            EngineConsole.Instance.Print("Файловая система загружена " + offest.TotalSeconds.ToString("0.00") + "c");

            VerInfoDialog.LoadTime = offest.TotalSeconds;
        }

        private void LoadFs(string path, string dataFolder)
        {
            foreach (var packFile in Directory.GetFiles(path + "\\" + dataFolder + "\\Packs", "*.pak"))
            {
                using (var archive = new ZipFile(packFile))
                {
                    foreach (ZipEntry entry in archive)
                    {
                        var file = new VFile(entry.Name, packFile, (int) entry.Size);
                        RootDirectory.AddFile(file);
                        
                        VerInfoDialog.TotalSize += entry.Size;
                        VerInfoDialog.TotalFile++;

                        if (entry.Name.EndsWith("*.loc"))
                        {
                            _locFiles.Add(file);
                        }
                    }
                }
            }
        }

        private void LoadTexts()
        {
            foreach (var loc in _locFiles)
            {
                var buffer = loc.Data.GetRange(0, 16).ToArray();
                var data = loc.Data.GetRange(16, loc.Data.Count - 16);

                var sizesPos = BitConverter.ToInt32(buffer, 4);
                var itemCount = BitConverter.ToInt32(buffer, 12);

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

                    RootDirectory.AddFile(new VFile(itemName, "", fileData));

                    VerInfoDialog.TotalVFile++;
                }

                loc.ClearCache();
            }
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

    }
}