using System;
using System.IO;
using Engine;
using Engine.UISystem;
using ProjectCommon;
using ProjectCommon.Controls;

namespace Game.Windows.Dialogs
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
            get => dir;
            private set
            {
                var flag = value.EndsWith("..");
                dir = Path.GetFullPath(value);

                if ((flag && dir.Length == 3) || !Directory.Exists(dir))
                    dir = "root";

                var c = window.Controls["list"] as IconListBox;
                c.Items.Clear();

                if (dir == "root")
                {
                    var allDrives = DriveInfo.GetDrives();
                    foreach (var d in allDrives)
                        c.Items.Add(d.Name, "hdd");
                }
                else
                {
                    c.Items.Add("<--", "back");
                    foreach (var d in Directory.GetDirectories(dir))
                        c.Items.Add(Path.GetFileName(d), "dir");

                    if (SelectFile)
                        foreach (var f in Directory.GetFiles(dir, File_mask))
                            c.Items.Add(Path.GetFileName(f), "file");
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
                window.Controls["file"].Text =sender.SelectedItem.Text;
        }

        void ListClick(object sender, ListBox.ItemMouseEventArgs e)
        {
            try
            {
                var path = Path.Combine(dir, ((IconListBox.Item)(e.Item)).Text.Replace("<--", ".."));

                if (File.Exists(path) || Directory.Exists(path + "\\data\\Packs"))
                {
                    window.Controls["file"].Text = Path.GetFileName(path);
                    Ok(null);
                }
                else
                {
                    Dir = path;
                }
            }
            catch(Exception ex)
            {
                EngineConsole.Instance.Print(ex.StackTrace);

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