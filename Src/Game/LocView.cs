using System.Text;
using Engine;
using Engine.UISystem;

namespace Game
{
    class LocView : Window
    {
        static LocView instance;

        public LocView(Data.File file)
            : base("LocView")
        {
            if (instance == null)
                instance = this;
            else
                Close();

            ((Button)window.Controls["find"]).Click += Find_Click;
            instance.window.Text = file.GetOnlyName();
            try
            {
                instance.window.Controls["text"].Text = Encoding.Unicode.GetString(file.Data.ToArray());
                instance.window.Controls["text"].ColorMultiplier = new Engine.MathEx.ColorValue(1, .98f, .62f);
            }
            catch
            {
                instance.window.Controls["text"].Text = "Не удалось прочитать файл, возможно данные повреждены.";
                instance.window.Controls["text"].ColorMultiplier = new Engine.MathEx.ColorValue(1, 0, 0);
            }
        }

        void Find_Click(Button sender)
        {
            new TextFind();
        }

        protected override void OnDetach()
        {
            base.OnDetach();

            if (this == instance)
                instance = null;
        }
    }
}