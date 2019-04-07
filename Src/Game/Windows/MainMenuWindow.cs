using System;
using System.IO;
using Engine;
using Engine.FileSystem;
using Engine.MathEx;
using Engine.Renderer;
using Engine.UISystem;
using Game.Structures;
using Game.Windows.Dialogs;

namespace Game.Windows
{
    public class MainMenuWindow : Control
    {
        Control window;

        public static MainMenuWindow Instance { get; private set; }

        protected override void OnAttach()
        {
            base.OnAttach();
            Instance = this;
            EngineApp.Instance.Config.RegisterClassParameters(GetType());

            window = ControlDeclarationManager.Instance.CreateControl("Gui\\Main.gui");
            Controls.Add(window);

            ((Button)window.Controls["open"]).Click += open_file;
            ((Button)window.Controls["exit"]).Click += exit;

            var backgrounds = Directory.GetFiles("Data\\" + window.Text);
            var rand = new Random();
            var background = backgrounds[rand.Next(0, backgrounds.Length - 1)];
            
            window.BackTexture = TextureManager.Instance.Load(VirtualFileSystem.GetVirtualPathByReal(background));

            ResetTime();
            
            open_file(null);
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
                        new PakViewWindow(file);
                    }
                }, false);
        }

        protected override void OnDetach()
        {
            base.OnDetach();
            Instance = null;
        }

        protected override void OnRenderUI(GuiRenderer renderer)
        {
            base.OnRenderUI(renderer);
            renderer.AddQuad(new Rect(0, 0, 1, 1), new ColorValue(.2f, .2f, .2f) * window.ColorMultiplier);
        }
    }
}