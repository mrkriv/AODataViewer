using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using Engine;
using Engine.FileSystem;
using Engine.UISystem;
using Engine.EntitySystem;
using Engine.MapSystem;
using Engine.MathEx;
using Engine.Renderer;
using Engine.SoundSystem;
using ProjectCommon;
using ProjectEntities;

namespace Game
{
    public class OpenFileDialog : Window
    {
        public delegate void OnOkClick(string file);
        public OnOkClick OkClick;
        public bool SelectFile = true;
        public string File_mask;

        [Config("Environment", "Directory")]
        static string dir = ".";
        [Config("Environment", "File")]
        static string _file;

        public string Dir
        {
            get { return dir; }
            private set
            {
                bool flag = value.EndsWith("..");
                dir = Path.GetFullPath(value);

                if ((flag && dir.Length == 3) || !Directory.Exists(dir))
                    dir = "root";

                IconListBox c = window.Controls["list"] as IconListBox;
                c.Items.Clear();

                if (dir == "root")
                {
                    DriveInfo[] allDrives = DriveInfo.GetDrives();
                    foreach (DriveInfo d in allDrives)
                        c.Items.Add(new string[] { d.Name, "hdd" });
                }
                else
                {
                    c.Items.Add(new string[] { "<--", "back" });
                    foreach (string d in Directory.GetDirectories(dir))
                        c.Items.Add(new string[] { Path.GetFileName(d), "dir" });

                    if (SelectFile)
                        foreach (string f in Directory.GetFiles(dir, File_mask))
                            c.Items.Add(new string[] { Path.GetFileName(f), "file" });
                }
            }
        }

        public OpenFileDialog(OnOkClick d, string filter = "*")
            : base("OpenFileDialog")
        {
            File_mask = filter;
            OkClick += d;
            Init();
        }

        public OpenFileDialog(OnOkClick d, bool selectFile)
            : base("OpenFileDialog")
        {
            SelectFile = selectFile;
            OkClick += d;
            Init();
        }

        void Init()
        {
            EngineApp.Instance.Config.RegisterClassParameters(GetType());

            ((IconListBox)window.Controls["list"]).ItemMouseDoubleClick += ListClick;
            ((IconListBox)window.Controls["list"]).SelectedIndexChange += OpenFileDialog_SelectedIndexChange;
            ((Button)window.Controls["ok"]).Click += Ok;
            window.Controls["file"].Text = _file;

            Dir = dir;
        }

        void OpenFileDialog_SelectedIndexChange(IconListBox sender)
        {
            if (sender.SelectedItem != null)
                window.Controls["file"].Text = ((string[])sender.SelectedItem)[0];
        }

        void ListClick(object sender, IconListBox.ItemMouseEventArgs e)
        {
            try
            {
                string path = Path.Combine(dir, ((string[])(e.Item))[0].Replace("<--", ".."));

                if (File.Exists(path) || File.Exists(path + "\\Profiles\\game.version"))
                {
                    window.Controls["file"].Text = Path.GetFileName(path);
                    Ok(null);
                }
                else
                {
                    Dir = path;
                }
            }
            catch
            {
                window.Controls["file"].Text = "Ошибка.";
            }
        }

        void Ok(Button sender)
        {
            _file = window.Controls["file"].Text;
            OkClick(dir + "\\" + window.Controls["file"].Text);
            SetShouldDetach();
        }
    }
}