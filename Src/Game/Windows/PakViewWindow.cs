using System;
using Engine;
using Engine.UISystem;
using Game.Structures;
using Game.Windows.Dialogs;

namespace Game.Windows
{
    public class PakViewWindow : Window
    {
        private string _path;
        public static Data Data;

        public string Path
        {
            get => _path;
            set
            {
                _path = value.Trim('/');
                window.Controls["text_path"].Text = _path;

                var c = window.Controls["list"] as IconListBox;
                c.Items.Clear();

                if (_path != "")
                    c.Items.Add("<-", "back");

                var mask = window.Controls["mask"].Text;
                var useMask =
                    ((CheckBox) window.Controls["find"]).Checked && !string.IsNullOrEmpty(mask); //todo:: add scearch

                var directory = Data.RootDirectory.GetDirectory(_path);
                directory.Files.Sort((a, b) => string.Compare(a.Path, b.Path, StringComparison.Ordinal));

                foreach (var name in directory.Directories.Keys)
                {
                    c.Items.Add(name, "dir");
                }

                foreach (var file in directory.Files)
                {
                    c.Items.Add(file.Name, GetType(file.Name), file);
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

        public PakViewWindow(string path)
            : base("PakView")
        {
            Data = new Data(path);
            //((IconListBox)window.Controls["list"]).MouseDown += ListClick;
            ((IconListBox) window.Controls["list"]).SelectedIndexChange += SelectedIndexChange;
            ((Button) window.Controls["edit"]).Click += EditFile;
            ((Button) window.Controls["more"]).Click += MoreSwitch;
            ((Button) window.Controls["specal\\save"]).Click += Specal_save;
            window.Controls["mask"].TextChange += Find;
            window.Controls["Progress"].Visible = false;
            window.Controls["specal"].Visible = false;
            ((CheckBox) window.Controls["find"]).CheckedChange += Find_CheckedChange;

            Path = "";
        }

        void Specal_save(Control sender)
        {
            try
            {
                var file = GetSelectFile();

                var saveWindow = new SaveFileDialog(file.Data.ToArray());
                saveWindow.Show(file.Path, new[] {"dds"});

                file.ClearCache();
            }
            catch
            {
                window.Controls["info"].Text = "Ошибка.";
            }
            window.Controls["specal"].Visible = false;
        }

        VFile GetSelectFile()
        {
            var lb = window.Controls["list"] as IconListBox;
            return (VFile) lb.SelectedItem.Data;
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
                ((Button) window.Controls["edit"]).Enable = true;
                ((Button) window.Controls["more"]).Enable = true;
                window.Controls["info"].Text = GetSelectFile().Pak;
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
            return lb?.SelectedItem?.Data != null;
        }

        void ListClick(object sender, EMouseButtons btn)
        {
            var selectItem = ((IconListBox) sender).SelectedItem;

            if (selectItem == null)
                return;

            var item = selectItem.Text;

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
            var file = GetSelectFile();

            switch (GetType(file.Name))
            {
                case "texture":
                    new TextureViewWindow(file);
                    break;
                case "texture_hi":
                    new TextureViewWindow(file);
                    break;
                case "loc":
                    new TextViewWindow(file);
                    break;
                case "model":
                    new ModelViewWindow(file);
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