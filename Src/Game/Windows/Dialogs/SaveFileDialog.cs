using System.IO;
using Engine;
using Engine.UISystem;

namespace Game.Windows.Dialogs
{
    public delegate void FileSelectDelegate(string path);

    public class SaveFileDialog : Window
    {
        [Config("Environment", "SaveDirectory")]
        public static string dir = ".";

        private event FileSelectDelegate FileSelectEvent;

        public event FileSelectDelegate OnFileSelect
        {
            add => FileSelectEvent += value;
            remove => FileSelectEvent -= value;
        }

        public string Dir
        {
            get => dir;
            private set
            {
                dir = Path.GetFullPath(value);

                if (!Directory.Exists(dir))
                    dir = "C:\\";

                var c = window.Controls["list"] as IconListBox;
                c.Items.Clear();

                c.Items.Add("<--", "back");

                foreach (var d in Directory.GetDirectories(dir))
                    c.Items.Add(Path.GetFileName(d), "dir");

                foreach (var f in Directory.GetFiles(dir))
                    c.Items.Add(Path.GetFileName(f), "file");
            }
        }

        public SaveFileDialog() : base("SaveFileDialog")
        {
            FileSelectEvent = path => { };
        }

        public SaveFileDialog(byte[] data) : base("SaveFileDialog")
        {
            FileSelectEvent = path => File.WriteAllBytes(path, data);
        }

        public void Show(string fileName, string[] format)
        {
            EngineApp.Instance.Config.RegisterClassParameters(GetType());

            ((IconListBox) window.Controls["list"]).ItemMouseDoubleClick += ListClick;
            ((IconListBox) window.Controls["list"]).SelectedIndexChange += OpenFileDialog_SelectedIndexChange;
            ((Button) window.Controls["ok"]).Click += Ok;

            foreach (var f in format)
                ((ComboBox) window.Controls["format"]).Items.Add(f);

            ((ComboBox) window.Controls["format"]).SelectedIndex = 0;

            window.Controls["file"].Text = fileName;
            Dir = dir;
        }

        void OpenFileDialog_SelectedIndexChange(IconListBox sender)
        {
            if (sender.SelectedItem != null)
            {
                var item = sender.SelectedItem.Text;
                var path = Path.Combine(dir, item.Replace("<--", ".."));

                if (File.Exists(path))
                    window.Controls["file"].Text = item;
            }
        }

        void ListClick(object sender, ListBox.ItemMouseEventArgs e)
        {
            try
            {
                var path = Path.Combine(dir, ((string[]) (e.Item))[0].Replace("<--", ".."));

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
            var path = dir + "\\" + window.Controls["file"].Text;

            if (((ComboBox) window.Controls["format"]).SelectedIndex != -1)
                path += "." + ((ComboBox) window.Controls["format"]).SelectedItem;

            OnFileSelectEvent(path);

            SetShouldDetach();
        }

        protected virtual void OnFileSelectEvent(string path)
        {
            FileSelectEvent?.Invoke(path);
        }
    }
}