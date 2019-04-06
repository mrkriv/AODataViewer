using System;
using System.Linq;
using Engine;
using Engine.UISystem;
using Game.Structures;
using Game.Windows.Dialogs;
using ProjectCommon.Controls;

namespace Game.Windows
{
    public class PakViewWindow : Window
    {
        private string _path;
        public static Data Data;

        private readonly IconListBox _fileListControl;
        
        public string Path
        {
            get => _path;
            set
            {
                _path = value.Trim('/');
                window.Controls["text_path"].Text = _path;

                _fileListControl.Items.Clear();

                if (_path != "")
                    _fileListControl.Items.Add("<-", "back");

                var mask = ((CheckBox) window.Controls["find"]).Checked ? window.Controls["mask"].Text : null;
                var root = Data.RootDirectory.GetDirectory(_path);

                var directories = root.Directories.Keys.ToList();
                directories.Sort();

                foreach (var name in directories)
                {
                    if (!string.IsNullOrEmpty(mask) && !root.Directories[name].FindFile(mask).Any())
                        continue;

                    _fileListControl.Items.Add(name, "dir");
                }

                var files = root.Files;
                files.Sort();

                foreach (var file in files)
                {
                    if (!string.IsNullOrEmpty(mask) && !file.Name.Contains(mask))
                        continue;

                    _fileListControl.Items.Add(file.Name, GetType(file.Name), file);
                }
            }
        }

        public PakViewWindow(string path) : base("PakView")
        {
            ((Button) window.Controls["edit"]).Click += EditFile;
            ((Button) window.Controls["more"]).Click += MoreSwitch;
            ((Button) window.Controls["specal\\save"]).Click += Specal_save;
            ((CheckBox) window.Controls["find"]).CheckedChange += Find_CheckedChange;
            
            window.Controls["mask"].TextChange += Find;
            window.Controls["Progress"].Visible = false;
            window.Controls["specal"].Visible = false;

            _fileListControl = (IconListBox) window.Controls["list"];
            _fileListControl.SelectedIndexChange += SelectedIndexChange;
            //_fileListControl.MouseDown += ListClick;
            
            Data = new Data(path);
            Path = "";
        }

        private void Specal_save(Control sender)
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

        private VFile GetSelectFile()
        {
            return (VFile) _fileListControl.SelectedItem.Data;
        }

        private void Find_CheckedChange(CheckBox sender)
        {
            Path = Path;
        }

        private void MoreSwitch(Control sender)
        {
            window.Controls["specal"].Visible = !window.Controls["specal"].Visible;
        }

        private void Find(Control sender)
        {
            if (((CheckBox) window.Controls["find"]).Checked)
                Path = Path;
        }

        private void SelectedIndexChange(IconListBox sender)
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

        private bool IsFile()
        {
            return _fileListControl.SelectedItem?.Data != null;
        }

        private void ListClick(object sender, EMouseButtons btn)
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

        private void EditFile(Button sender = null)
        {
            /*try
            {*/
            var file = GetSelectFile();

            switch (GetType(file.Name))
            {
                case "texture":
                case "texture_hi":
                    new TextureViewWindow(file);
                    break;
                case "txt":
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

        private static string GetType(string name)
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
            
            if (name.EndsWith(".xdb"))
                return "xdb";
            
            if (name.EndsWith(".lua") || name.EndsWith(".luac"))
                return "lua";
            
            if (name.EndsWith(".xdb"))
                return "xdb";
            
            if (name.EndsWith(".txt"))
                return "txt";

            return "file";
        }
    }
}