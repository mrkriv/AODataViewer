using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.IO.Compression;
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
    public class MainMenuWindow : Control
    {
        Control window;
        static MainMenuWindow instance;

        public static MainMenuWindow Instance
        {
            get { return instance; }
        }

        protected override void OnAttach()
        {
            base.OnAttach();
            instance = this;
            EngineApp.Instance.Config.RegisterClassParameters(GetType());

            window = ControlDeclarationManager.Instance.CreateControl("Gui\\Main.gui");
            Controls.Add(window);

            ((Button)window.Controls["open"]).Click += open_file;
            ((Button)window.Controls["exit"]).Click += exit;

            string[] bk = Directory.GetFiles("Data\\" + window.Text);
            Random rand = new Random();
            window.BackTexture = TextureManager.Instance.Load(VirtualFileSystem.GetVirtualPathByReal(bk[rand.Next(0, bk.Length - 1)]));

            ResetTime();
        }

        void exit(Button sender)
        {
            GameEngineApp.Instance.SetFadeOutScreenAndExit();
        }

        void open_file(Button sender)
        {
            new OpenFileDialog(delegate(string file)
                {
                    if (Directory.Exists(file))
                    {
                        new PakView(file);
                        new VerInfo(file);
                    }
                }, false);
        }

        protected override void OnDetach()
        {
            base.OnDetach();
            instance = null;
        }

        protected override void OnRenderUI(GuiRenderer renderer)
        {
            base.OnRenderUI(renderer);
            renderer.AddQuad(new Rect(0, 0, 1, 1), new ColorValue(.2f, .2f, .2f) * window.ColorMultiplier);
        }
    }
}