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
using Engine.Renderer.ModelImporting;
using Engine.SoundSystem;
using ProjectCommon;
using ProjectEntities;
using ICSharpCode.SharpZipLib.Checksums;
using ICSharpCode.SharpZipLib.Zip;

namespace Game
{
    public class PakView : Window
    {
        string path;
        public List<Data.File> Files;
        public static Data data;

        public string Path
        {
            get { return path; }
            private set
            {
                path = value.Trim('/');
                window.Controls["text_path"].Text = path;
                Files = new List<Data.File>();
                List<string> items = new List<string>();
                IconListBox c = window.Controls["list"] as IconListBox;
                c.Items.Clear();

                if (path != "")
                    c.Items.Add(new string[] { "<-", "back" });

                foreach (Data.File f in data.Files)
                {
                    string name = f.Name.Trim('/');

                    if (((CheckBox)window.Controls["find"]).Checked && window.Controls["mask"].Text != "" && name.IndexOf(window.Controls["mask"].Text) == -1)
                        continue;

                    if (!name.StartsWith(path))
                        continue;

                    name = name.Substring(path.Length);
                    name = name.TrimStart('/');
                    name = name.Split('/')[0];

                    if (!items.Contains(name))
                    {

                        string[] s = new string[] { name, "dir" };

                        if (name.IndexOf(".") != -1)
                            s[1] = GetType(name);

                        items.Add(name);
                        c.Items.Add(s);
                        Files.Add(f);
                    }
                }
            }
        }

        string GetType(string name)
        {
            if (name.EndsWith(".(Geometry).bin"))
                return "model";
            if (name.EndsWith(".(Texture).bin"))
                return "texture";
            if (name.EndsWith(".(Texture).hi.bin"))
                return "texture_hi";
            if (name.EndsWith(".(CollisionMesh).hi.bin"))
                return "model_cl";
            if (name.EndsWith(".(SkeletalAnimation).bin"))
                return "anim";
            if (name.EndsWith(".txt"))
                return "loc";

            return "file";
        }

        public PakView(string path)
            : base("PakView")
        {
            data = new Data(path);
            ((IconListBox)window.Controls["list"]).ItemMouseDoubleClick += ListClick;
            ((IconListBox)window.Controls["list"]).SelectedIndexChange += SelectedIndexChange;
            ((Button)window.Controls["edit"]).Click += EditFile;
            ((Button)window.Controls["more"]).Click += MoreSwitch;
            ((Button)window.Controls["specal\\save"]).Click += Specal_save;
            ((Button)window.Controls["specal\\ungzip"]).Click += Specal_ungzip;
            window.Controls["mask"].TextChange += Find;
            window.Controls["Progress"].Visible = false;
            window.Controls["specal"].Visible = false;
            ((CheckBox)window.Controls["find"]).CheckedChange += Find_CheckedChange;

            Path = "";
        }

        void Specal_save(Control sender)
        {
            Save(false);
            window.Controls["specal"].Visible = false;
        }

        void Specal_ungzip(Control sender)
        {
            Save(true);
            window.Controls["specal"].Visible = false;
        }

        void Save(bool ungzip)
        {
            try
            {
                IconListBox lb = window.Controls["list"] as IconListBox;
                string file = ((string[])lb.SelectedItem)[0];
                Data.File f = Files[lb.SelectedIndex - 1];
                f.ReadData(ungzip);
                new SaveFileDialog(file, f.Data.ToArray());
                f.ClearCache();
            }
            catch
            {
                window.Controls["info"].Text = "Ошибка.";
            }
        }

        void Find_CheckedChange(CheckBox sender)
        {
            Path = Path;
        }

        void MoreSwitch(Control sender)
        {
            window.Controls["specal"].Visible = !window.Controls["specal"].Visible;
        }

        void Find(Control sender)
        {
            if (((CheckBox)window.Controls["find"]).Checked)
                Path = Path;
        }

        void SelectedIndexChange(IconListBox sender)
        {
            window.Controls["specal"].Visible = false;
            if (IsFile())
            {
                IconListBox lb = window.Controls["list"] as IconListBox;
                ((Button)window.Controls["edit"]).Enable = true;
                ((Button)window.Controls["more"]).Enable = true;
                window.Controls["info"].Text = Files[lb.SelectedIndex - 1].Pak;
            }
            else
            {
                ((Button)window.Controls["edit"]).Enable = false;
                ((Button)window.Controls["more"]).Enable = false;
                window.Controls["info"].Text = "Виртуальный файл";
            }
        }

        bool IsFile()
        {
            IconListBox lb = window.Controls["list"] as IconListBox;
            if (lb.SelectedItem != null)
            {
                string item = ((string[])lb.SelectedItem)[0];
                return item.IndexOf(".") != -1;
            }
            return false;
        }

        void ListClick(object sender, IconListBox.ItemMouseEventArgs e)
        {
            string item = ((string[])((IconListBox)sender).SelectedItem)[0];

            if (item == null)
                return;

            if (item != "<-")
            {
                if (!IsFile())
                    Path += "/" + item;
                else
                    EditFile();
            }
            else
            {
                if (path.IndexOf('/') != -1)
                    Path = path.Remove(path.LastIndexOf('/'));
                else
                    Path = "";
            }
        }

        void EditFile(Button sender = null)
        {
            /*try
            {*/
            IconListBox lb = window.Controls["list"] as IconListBox;
            string file = ((string[])lb.SelectedItem)[0];
            switch (GetType(file))
            {
                case "texture":
                    new TextureView(Files[lb.SelectedIndex - 1]);
                    break;
                case "texture_hi":
                    new TextureView(Files[lb.SelectedIndex - 1]);
                    break;
                case "loc":
                    new LocView(Files[lb.SelectedIndex - 1]);
                    break;
                case "model":
                    new V3dWindow(Files[lb.SelectedIndex - 1]);
                    break;
            }
            /*}
            catch
            {
                window.Controls["info"].Text = "Ошибка.";
            }*/
        }
    }
}