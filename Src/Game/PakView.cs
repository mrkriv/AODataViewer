﻿using System.Collections.Generic;
using Engine;
using Engine.UISystem;

namespace Game
{
    public class PakView : Window
    {
        private string _path;
        private List<Data.File> _files;
        public static Data Data;

        public string Path
        {
            get => _path;
            set
            {
                _path = value.Trim('/');
                window.Controls["text_path"].Text = _path;
                _files = new List<Data.File>();
                var items = new List<string>();
                var c = window.Controls["list"] as IconListBox;
                c.Items.Clear();

                if (_path != "")
                    c.Items.Add(new[] {"<-", "back"});

                var mask = window.Controls["mask"].Text;
                var useMask = ((CheckBox) window.Controls["find"]).Checked && !string.IsNullOrEmpty(mask);

                foreach (var f in Data.Files)
                {
                    var name = f.Name;

                    if (!name.StartsWith(_path))
                        continue;

                    if (useMask && !name.Contains(mask))
                        continue;

                    _files.Add(f);
                }

                _files.Sort((a, b) => a.Name.CompareTo(b.Name));

                foreach (var f in _files)
                {
                    var name = f.Name.Trim('/');

                    name = name.Substring(_path.Length);
                    name = name.TrimStart('/');
                    name = name.Split('/')[0];

                    if (items.Contains(name))
                        continue;

                    var s = new[] {name, "dir"};

                    if (name.Contains("."))
                        s[1] = GetType(name);

                    items.Add(name);
                    c.Items.Add(s);
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
            Data = new Data(path);
            //((IconListBox)window.Controls["list"]).MouseDown += ListClick;
            ((IconListBox) window.Controls["list"]).SelectedIndexChange += SelectedIndexChange;
            ((Button) window.Controls["edit"]).Click += EditFile;
            ((Button) window.Controls["more"]).Click += MoreSwitch;
            ((Button) window.Controls["specal\\save"]).Click += Specal_save;
            ((Button) window.Controls["specal\\ungzip"]).Click += Specal_ungzip;
            window.Controls["mask"].TextChange += Find;
            window.Controls["Progress"].Visible = false;
            window.Controls["specal"].Visible = false;
            ((CheckBox) window.Controls["find"]).CheckedChange += Find_CheckedChange;

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
                var lb = window.Controls["list"] as IconListBox;
                var file = ((string[]) lb.SelectedItem)[0];
                var f = _files[lb.SelectedIndex - 1];
                f.ReadData(ungzip);
                
                var saveWindow = new SaveFileDialog(f.Data.ToArray());
                saveWindow.Show(file, new[] {"dds"});
                
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
            if (((CheckBox) window.Controls["find"]).Checked)
                Path = Path;
        }

        void SelectedIndexChange(IconListBox sender)
        {
            window.Controls["specal"].Visible = false;
            if (IsFile())
            {
                var lb = window.Controls["list"] as IconListBox;
                ((Button) window.Controls["edit"]).Enable = true;
                ((Button) window.Controls["more"]).Enable = true;
                window.Controls["info"].Text = _files[lb.SelectedIndex - 1].Pak;
            }
            else
            {
                ((Button) window.Controls["edit"]).Enable = false;
                ((Button) window.Controls["more"]).Enable = false;
                window.Controls["info"].Text = "Виртуальный файл";
            }

            ListClick(sender, EMouseButtons.Left);
        }

        bool IsFile()
        {
            var lb = window.Controls["list"] as IconListBox;
            if (lb.SelectedItem != null)
            {
                var item = ((string[]) lb.SelectedItem)[0];
                return item.IndexOf(".") != -1;
            }

            return false;
        }

        void ListClick(object sender, EMouseButtons btn)
        {
            var selectItem = ((IconListBox) sender).SelectedItem;

            var item = ((string[]) selectItem)?[0];

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
                if (_path.IndexOf('/') != -1)
                    Path = _path.Remove(_path.LastIndexOf('/'));
                else
                    Path = "";
            }
        }

        void EditFile(Button sender = null)
        {
            /*try
            {*/
            var lb = window.Controls["list"] as IconListBox;
            var file = ((string[]) lb.SelectedItem)[0];
            switch (GetType(file))
            {
                case "texture":
                    new TextureView(_files[lb.SelectedIndex - 1]);
                    break;
                case "texture_hi":
                    new TextureView(_files[lb.SelectedIndex - 1]);
                    break;
                case "loc":
                    new LocView(_files[lb.SelectedIndex - 1]);
                    break;
                case "model":
                    new ModelViewWindow(_files[lb.SelectedIndex - 1]);
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