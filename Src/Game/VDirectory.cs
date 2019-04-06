using System;
using System.Collections.Generic;

namespace Game
{
    public class VDirectory
    {
        public static char[] PathSeparators = { '\\', '/'};

        public string Name { get; }
        public Dictionary<string, VDirectory> Directories { get; }
        public List<VFile> Files { get; }

        public VDirectory(string name)
        {
            Directories = new Dictionary<string, VDirectory>();
            Files = new List<VFile>();
            Name = name;
        }

        public void AddFile(VFile file)
        {
            AddFile(file, file.Path);
        }

        private void AddFile(VFile file, string relativePath)
        {
            var pp = relativePath.Split(PathSeparators, 2, StringSplitOptions.RemoveEmptyEntries);

            if (pp.Length > 1)
            {
                var dirName = pp[0];

                if (!Directories.TryGetValue(dirName, out var dir))
                {
                    dir = new VDirectory(dirName);
                    Directories.Add(dirName, dir);
                }

                dir.AddFile(file, pp[1]);
            }
            else
            {
                Files.Add(file);
            }
        }

        public VDirectory GetDirectory(string relativePath)
        {
            if (string.IsNullOrEmpty(relativePath))
                return this;

            var pp = relativePath.Split(PathSeparators, 2);

            if (Directories.TryGetValue(pp[0], out var dir))
            {
                return dir.GetDirectory(pp.Length > 1 ? pp[1] : null);
            }

            return null;
        }
    }
}