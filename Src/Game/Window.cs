using Engine;
using Engine.MathEx;
using Engine.UISystem;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Game
{
    public class Window : Control
    {
        private Vec2 mouseOffset;
        private Vec2 MinSize;
        private bool isMove = false;
        private bool isReSize = false;
        protected Control window;

        public Window(string gui)
        {
            window = ControlDeclarationManager.Instance.CreateControl("Gui\\" + gui + ".gui");
            window.MouseCover = true;

            if (IsValidControl("size"))
            {
                window.Controls["size"].Visible = false;
                MinSize = window.Controls["size"].Size.Value;
            }
            if (IsValidControl("head"))
            {
                window.Controls["head"].MouseDown += Head_MouseDown;
                window.Controls["head"].MouseMove += Head_MouseMove;
                window.Controls["head"].MouseUp += Head_MouseUp;
            }

            if (IsValidControl("resize"))
            {
                if (window.LockEditorResizing)
                    window.Controls["resize"].Visible = false;
                else
                {
                    window.Controls["resize"].MouseDown += ReSize_MouseDown;
                    window.Controls["resize"].MouseMove += ReSize_MouseMove;
                    window.Controls["resize"].MouseUp += ReSize_MouseUp;
                }
            }

            if (IsValidControl("done"))
                ((Button)window.Controls["done"]).Click += Close;

            Controls.Add(window);
            GameEngineApp.Instance.ControlManager.Controls.Add(this);
        }

        protected override bool OnMouseDown(EMouseButtons button)
        {
            Focus();
            return base.OnMouseDown(button);
        }

        public new void Focus()
        {
            if (GameEngineApp.Instance != null && GameEngineApp.Instance.ControlManager != null)
            {
                ControlCollection cl = GameEngineApp.Instance.ControlManager.Controls;
                if (cl.Count != 0 && this != cl[cl.Count - 1])
                    cl.BringToFront(this);
            }
        }

        public bool IsValidControl(string xName)
        {
            try
            {
                return window.Controls[xName] != null;
            }
            catch
            {
                return false;
            }
        }

        public void Close(Button sender = null)
        {
            SetShouldDetach();
        }

        void Head_MouseUp(Control sender, EMouseButtons button)
        {
            if (button == EMouseButtons.Left)
                isMove = false;
        }

        void Head_MouseMove(Control sender)
        {
            if (isMove)
                window.Position = new ScaleValue(ScaleType.Parent, MousePosition - mouseOffset);
        }

        void Head_MouseDown(Control sender, EMouseButtons button)
        {
            if (button == EMouseButtons.Left)
            {
                mouseOffset = MousePosition - window.Position.Value;
                isMove = true;
            }
        }

        void ReSize_MouseUp(Control sender, EMouseButtons button)
        {
            if (button == EMouseButtons.Left)
                isReSize = false;
        }

        void ReSize_MouseMove(Control sender)
        {
            if (isReSize)
            {
                mouseOffset = MousePosition - mouseOffset;
                mouseOffset *= .75f;
                mouseOffset *= new Vec2(1024, 768);
                Vec2 NewSize = window.Size.Value + mouseOffset;

                if (window.Size.Value.X < MinSize.X)
                    NewSize.X = MinSize.X;

                if (window.Size.Value.Y < MinSize.Y)
                    NewSize.Y = MinSize.Y;

                window.Size = new ScaleValue(ScaleType.ScaleByResolution, NewSize);
                mouseOffset = MousePosition;
            }
        }

        void ReSize_MouseDown(Control sender, EMouseButtons button)
        {
            if (button == EMouseButtons.Left)
            {
                mouseOffset = MousePosition;
                isReSize = true;
            }
        }
    }
}