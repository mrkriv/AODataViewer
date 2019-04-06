using System.Text;
using Engine.UISystem;
using Game.Structures;

namespace Game.Windows
{
    class TextViewWindow : Window
    {
        private static TextViewWindow _instance;

        public TextViewWindow(VFile vFile)
            : base("LocView")
        {
            if (_instance == null)
                _instance = this;
            else
                Close();

            ((Button) window.Controls["find"]).Click += Find_Click;
            _instance.window.Text = vFile.Name;
            try
            {
                _instance.window.Controls["text"].Text = Encoding.Unicode.GetString(vFile.Data.ToArray());
                _instance.window.Controls["text"].ColorMultiplier = new Engine.MathEx.ColorValue(1, .98f, .62f);
            }
            catch
            {
                _instance.window.Controls["text"].Text = "Не удалось прочитать файл, возможно данные повреждены.";
                _instance.window.Controls["text"].ColorMultiplier = new Engine.MathEx.ColorValue(1, 0, 0);
            }
        }

        void Find_Click(Button sender)
        {
            var textFind = new TextFind();
        }

        protected override void OnDetach()
        {
            base.OnDetach();

            if (this == _instance)
                _instance = null;
        }
    }
}