using System;
using System.Collections.Generic;
using Engine.MathEx;
using Engine.Renderer;
using Engine.UISystem;
using Game.Structures;
using Game.Windows.Dialogs;

namespace Game.Windows
{
    class TextureViewWindow : Window
    {
        List<byte> buffer;
        bool isInit;
        static TextureViewWindow instance;
        bool first = true;
        VFile _vFile;
        int Width = 4096;
        int Height = 4096;
        int type = 1;

        public TextureViewWindow(VFile vFile)
            : base("TextureView")
        {
            if (instance == null)
                instance = this;
            else
                Close();

            instance.Init(vFile);
        }

        public void Init(VFile vFile)
        {
            isInit = true;

            instance.window.Text = vFile.Name;

            if (instance.first)
            {
                ((ComboBox)instance.window.Controls["w"]).Items.Clear();
                ((ComboBox)instance.window.Controls["h"]).Items.Clear();
                ((ComboBox)instance.window.Controls["type"]).Items.Clear();

                for (var i = 4; i <= 12; i++)
                {
                    ((ComboBox)instance.window.Controls["w"]).Items.Add((int)Math.Pow(2, i));
                    ((ComboBox)instance.window.Controls["h"]).Items.Add((int)Math.Pow(2, i));
                }

                ((ComboBox)instance.window.Controls["type"]).Items.Add("DXT1");
                ((ComboBox)instance.window.Controls["type"]).Items.Add("DXT2");
                ((ComboBox)instance.window.Controls["type"]).Items.Add("DXT3");
                ((ComboBox)instance.window.Controls["type"]).Items.Add("DXT4");
                ((ComboBox)instance.window.Controls["type"]).Items.Add("DXT5");
                ((ComboBox)instance.window.Controls["type"]).SelectedIndexChange += TypeSelected;
                ((ComboBox)instance.window.Controls["h"]).SelectedIndexChange += SizeSelected;
                ((ComboBox)instance.window.Controls["w"]).SelectedIndexChange += SizeSelected;

                ((Button)window.Controls["export"]).Click += Export;
                ((Button)window.Controls["export_express"]).Click += Export_express;

                instance.first = false;
            }

            _vFile?.ClearCache();
            _vFile = vFile;

            Width = 4096;
            Height = 4096;

            while (true)
            {
                var size = Width * Height;
                if (type == 1)
                    size /= 2;

                if (size > _vFile.Data.Count)
                {
                    Width /= 2;
                    Height /= 2;
                }
                else
                {
                    ((ComboBox)window.Controls["w"]).SelectedIndex = GetPowerOfTwo(Width) - 4;
                    ((ComboBox)window.Controls["h"]).SelectedIndex = GetPowerOfTwo(Height) - 4;
                    break;
                }
            }

            ((ComboBox)window.Controls["type"]).SelectedIndex = 0;

            isInit = false;
            Render();
        }

        public void Export_express(Button sender)
        {
            System.IO.File.WriteAllBytes(SaveFileDialog.dir + "\\" + _vFile.Name + ".dds", buffer.ToArray());
        }

        public void Export(Button sender)
        {
            var saveWindow = new SaveFileDialog(buffer.ToArray());
            saveWindow.Show(_vFile.Name, new[] {"dds"});
        }

        int GetPowerOfTwo(int x)
        {
            var y = 0;

            while (x % 2 == 0)
            {
                if (x == 0)
                    break;

                x /= 2;
                y++;
            }

            return y;
        }

        void SizeSelected(ComboBox sender)
        {
            if (isInit)
                return;

            Width = (int)((ComboBox)window.Controls["w"]).SelectedItem;
            Height = (int)((ComboBox)window.Controls["h"]).SelectedItem;

            if (((CheckBox)window.Controls["cube"]).Checked)
            {
                isInit = true;
                ((ComboBox)window.Controls["w"]).SelectedIndex = sender.SelectedIndex;
                ((ComboBox)window.Controls["h"]).SelectedIndex = sender.SelectedIndex;
                isInit = false;
            }

            Render();
        }

        void TypeSelected(ComboBox sender)
        {
            if (isInit)
                return;

            type = int.Parse((((ComboBox)window.Controls["type"]).SelectedItem as string).Substring(3));
            Render();
        }

        void Render()
        {
            Decoder("Data\\GUI\\Textures\\dec.dds", _vFile.Data, type, Width, Height);

            TextureManager.Instance.Unload("GUI\\Textures\\dec.dds");
            window.Controls["img"].BackTexture = TextureManager.Instance.Load("GUI\\Textures\\dec.dds");
        }

        protected override void OnDetach()
        {
            base.OnDetach();

            _vFile?.ClearCache();

            if (this == instance)
                instance = null;
        }

        public void Decoder(string path, List<byte> Data, int type, int Width, int Height)
        {
            buffer = new List<byte>();

            window.Controls["img\\info"].Text = $"{Width}x{Height} DTX{type}";

            var size = Width * Height;
            if (type == 1)
                size /= 2;

            if (size > Data.Count)
            {
                ((TextBox)window.Controls["img\\info"]).TextColor = new ColorValue(1, 0, 0);
                return;
            }

            buffer.AddRange(new byte[] { 0x44, 0x44, 0x53, 0x20, 0x7c, 0x00, 0x00, 0x00, 0x07, 0x10, 0x08, 0x00 });
            buffer.AddRange(BitConverter.GetBytes(Width));
            buffer.AddRange(BitConverter.GetBytes(Height));
            buffer.AddRange(BitConverter.GetBytes(size));

            for (var i = 0; i < 52; i++)
                buffer.Add(0x00);

            buffer.AddRange(new byte[] { 0x20, 0x00, 0x00, 0x00 });
            buffer.AddRange(new byte[] { 0x04, 0x00, 0x00, 0x00, 0x44, 0x58, 0x54 });
            buffer.Add((byte)(0x30 + type));

            for (var i = 0; i < 20; i++)
                buffer.Add(0x00);

            buffer.AddRange(new byte[] { 0x00, 0x10, 0x00, 0x00 });

            for (var i = 0; i < 16; i++)
                buffer.Add(0x00);

            buffer.AddRange(Data.GetRange(Data.Count - size, size));

            System.IO.File.WriteAllBytes(path, buffer.ToArray());

            ((TextBox)window.Controls["img\\info"]).TextColor = new ColorValue(1, 1, 1);
        }
    }
}