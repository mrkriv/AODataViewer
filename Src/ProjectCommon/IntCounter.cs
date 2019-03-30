using Engine;
using Engine.MathEx;
using Engine.Renderer;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Reflection;

namespace Engine.UISystem
{
    public class IntCounter : Control
    {
        private EditBox editLine;
        private Button plus;
        private Button minus;
        private int step = 1;
        private int min = 0;
        private int max = 0;
        private Button.ClickDelegate plusClick;
        private Button.ClickDelegate minusClick;
        private Control.DefaultEventDelegate editBoxText;
        public delegate void ValueChangeDelegate(IntCounter control, int value);
        public event ValueChangeDelegate ValueChange;
        
        [Browsable(false)]
        [Serialize]
        public EditBox EditLine
        {
            get { return editLine; }
            set
            {
                if (editLine != null)
                {
                    editLine.TextChange -= editBoxText;
                    editLine.MouseWheel -= OnMouseWheel;
                    Controls.Remove(editLine);
                }

                editLine = value;
                if (editLine == null)
                    return;

                if (editLine.Parent == null)
                    Controls.Add(editLine);

                editBoxText = new DefaultEventDelegate(OnTextChange);
                editLine.Text = "0";
                editLine.TextChange += editBoxText;
                editLine.MouseWheel += OnMouseWheel;
                Update();
            }
        }

        [Browsable(false)]
        [Serialize]
        public Button Plus
        {
            get { return plus; }
            set
            {
                if (plus != null)
                {
                    plus.Click -= plusClick;
                    Controls.Remove(plus);
                }

                plus = value;
                if (plus == null)
                    return;

                if (plus.Parent == null)
                    Controls.Add(plus);

                plusClick = new Button.ClickDelegate(OnPlus);
                plus.Click += plusClick;
                Update();
            }
        }

        [Browsable(false)]
        [Serialize]
        public Button Minus
        {
            get { return minus; }
            set
            {
                if (minus != null)
                {
                    minus.Click -= minusClick;
                    Controls.Remove(minus);
                }

                minus = value;
                if (minus == null)
                    return;

                if (minus.Parent == null)
                    Controls.Add(minus);

                minusClick = new Button.ClickDelegate(OnMinus);
                minus.Click += minusClick;
                Update();
            }
        }

        [Category("Counter")]
        [DefaultValue(0)]
        [Serialize]
        public int Value
        {
            get
            {
                try
                {
                    return int.Parse(editLine.Text);
                }
                catch
                {
                    return 0;
                }
            }
            set
            {
                if (editLine != null)
                {
                    Plus.Enable = true;
                    Minus.Enable = true;
                    if (max != 0 && value > max)
                    {
                        value = max;
                        Plus.Enable = false;
                    }
                    else if (min != 0 && value < min)
                    {
                        value = min;
                        Minus.Enable = false;
                    }

                    if (value == 0)
                        editLine.Text = "0";
                    else
                        editLine.Text = value.ToString();
                }
            }
        }

        [Category("Counter")]
        [DefaultValue(1)]
        [Serialize]
        public int Step
        {
            get { return step; }
            set { step = value; }
        }

        [Category("Counter")]
        [DefaultValue(0)]
        [Serialize]
        public int Min
        {
            get { return min; }
            set
            {
                min = value;
                if (min > max)
                    min = max - 1;
            }
        }

        [Category("Counter")]
        [DefaultValue(0)]
        [Serialize]
        public int Max
        {
            get { return max; }
            set
            {
                max = value;
                if (max < min)
                    max = min + 1;
            }
        }

        protected override Control.StandardChildSlotItem[] OnGetStandardChildSlots()
        {
            return new Control.StandardChildSlotItem[3]
              {
                new Control.StandardChildSlotItem("EditLine", (Control)EditLine),
                new Control.StandardChildSlotItem("Plus", (Control)Plus),
                new Control.StandardChildSlotItem("Minus", (Control)Minus)
              };
        }

        protected override void OnAttach()
        {
            base.OnAttach();
            Update();
        }

        public void Update() { }


        void OnMouseWheel(Control sender, int delta)
        {
            if (!((EditBox)sender).Focused)
                return;

            if (delta > 0)
                OnPlus();
            else
                OnMinus();
        }

        public void OnTextChange(Control sender)
        {
            string numbers = "-0123456789";
            string str = "";

            foreach (char c in editLine.Text)
            {
                foreach (char n in numbers)
                {
                    if (c == n)
                    {
                        str += c.ToString();
                        break;
                    }
                }
            }

            if (str == "")
                str = "0";

            editLine.Text = str;
            OnValueChange();
        }

        public void OnValueChange()
        {
            if (ValueChange != null)
                ValueChange(this, Value);
        }

        public void OnMinus(Button sender = null)
        {
            Plus.Enable = true;
            Value -= step;

            if (Value - step < min)
                Minus.Enable = false;
        }

        void OnPlus(Button sender = null)
        {
            Minus.Enable = true;
            Value += step;

            if (max != 0 && Value + step > max)
                Plus.Enable = false;
        }

        protected override void OnControlDetach(Control control)
        {
            base.OnControlDetach(control);

            if (control == EditLine)
                EditLine = null;
            if (control == Plus)
                Plus = null;
            if (control == Minus)
                Minus = null;
        }

        protected override Image OnGetEditorIcon()
        {
            return new ScrollBar().GetEditorIcon();
        }
    }
}