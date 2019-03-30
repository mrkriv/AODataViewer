﻿using System.Text;
using Engine.UISystem;

namespace Game
{
    class LocView : Window
    {
        private static LocView _instance;

        public LocView(Data.File file)
            : base("LocView")
        {
            if (_instance == null)
                _instance = this;
            else
                Close();

            ((Button)window.Controls["find"]).Click += Find_Click;
            _instance.window.Text = file.GetOnlyName();
            try
            {
                _instance.window.Controls["text"].Text = Encoding.Unicode.GetString(file.Data.ToArray());
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