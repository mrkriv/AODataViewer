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
    public class SaveFileDialog : Window
    {
        [Config("Environment", "SaveDirectory")]
        public static string dir = ".";
        byte[] Data;

        public string Dir
        {
            get { return dir; }
            private set
            {
                dir = Path.GetFullPath(value);

                if (!Directory.Exists(dir))
                    dir = "C:\\";

                IconListBox c = window.Controls["list"] as IconListBox;
                c.Items.Clear();

                c.Items.Add(new string[] { "<--", "back" });
                foreach (string d in Directory.GetDirectories(dir))
                    c.Items.Add(new string[] { Path.GetFileName(d), "dir" });

                foreach (string f in Directory.GetFiles(dir))
                    c.Items.Add(new string[] { Path.GetFileName(f), "file" });
            }
        }

        public SaveFileDialog(string file, byte[] data)
            : base("SaveFileDialog")
        {
            Data = data;
            Init(file, new string[] { });
        }

        public SaveFileDialog(string file, byte[] data, string format)
            : base("SaveFileDialog")
        {
            Data = data;
            Init(file, new string[] { format });
        }

        public SaveFileDialog(string file, byte[] data, string[] format)
            : base("SaveFileDialog")
        {
            Data = data;
            Init(file, format);
        }

        void Init(string file, string[] format)
        {
            EngineApp.Instance.Config.RegisterClassParameters(GetType());
                        
            ((IconListBox)window.Controls["list"]).ItemMouseDoubleClick += ListClick;
            ((IconListBox)window.Controls["list"]).SelectedIndexChange += OpenFileDialog_SelectedIndexChange;
            ((Button)window.Controls["ok"]).Click += Ok;

            window.Controls["file"].Text = file;

            foreach (string f in format)
                ((ComboBox)window.Controls["format"]).Items.Add(f);

            Dir = dir;
        }

        void OpenFileDialog_SelectedIndexChange(IconListBox sender)
        {
            if (sender.SelectedItem != null)
            {
                string item = ((string[])sender.SelectedItem)[0];
                string path = Path.Combine(dir, item.Replace("<--", ".."));

                if (File.Exists(path))
                    window.Controls["file"].Text = item;
            }
        }

        void ListClick(object sender, IconListBox.ItemMouseEventArgs e)
        {
            try
            {
                string path = Path.Combine(dir, ((string[])(e.Item))[0].Replace("<--", ".."));

                if (File.Exists(path))
                    window.Controls["file"].Text = Path.GetFileName(path);
                else
                    Dir = path;
            }
            catch
            {
                window.Controls["file"].Text = "Ошибка.";
            }
        }

        void Ok(Button sender)
        {
            string path = dir + "\\" + window.Controls["file"].Text;

            if (((ComboBox)window.Controls["format"]).SelectedIndex != -1)
                path += "." + ((ComboBox)window.Controls["format"]).SelectedItem as string;

            File.WriteAllBytes(path, Data);

            SetShouldDetach();
        }
    }
}