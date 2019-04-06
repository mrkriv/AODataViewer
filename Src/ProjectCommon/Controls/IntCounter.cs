using System.ComponentModel;
using System.Drawing;
using Engine.UISystem;

namespace ProjectCommon.Controls
{
    public class IntCounter : Control
    {
        private EditBox _editLine;
        private Button _plus;
        private Button _minus;
        private int _min;
        private int _max;
        private Button.ClickDelegate _plusClick;
        private Button.ClickDelegate _minusClick;
        private DefaultEventDelegate _editBoxText;
        public delegate void ValueChangeDelegate(IntCounter control, int value);
        public event ValueChangeDelegate ValueChange;
        
        [Browsable(false)]
        [Serialize]
        public EditBox EditLine
        {
            get => _editLine;
            set
            {
                if (_editLine != null)
                {
                    _editLine.TextChange -= _editBoxText;
                    _editLine.MouseWheel -= OnMouseWheel;
                    Controls.Remove(_editLine);
                }

                _editLine = value;
                if (_editLine == null)
                    return;

                if (_editLine.Parent == null)
                    Controls.Add(_editLine);

                _editBoxText = new DefaultEventDelegate(OnTextChange);
                _editLine.Text = "0";
                _editLine.TextChange += _editBoxText;
                _editLine.MouseWheel += OnMouseWheel;
                Update();
            }
        }

        [Browsable(false)]
        [Serialize]
        public Button Plus
        {
            get => _plus;
            set
            {
                if (_plus != null)
                {
                    _plus.Click -= _plusClick;
                    Controls.Remove(_plus);
                }

                _plus = value;
                if (_plus == null)
                    return;

                if (_plus.Parent == null)
                    Controls.Add(_plus);

                _plusClick = new Button.ClickDelegate(OnPlus);
                _plus.Click += _plusClick;
                Update();
            }
        }

        [Browsable(false)]
        [Serialize]
        public Button Minus
        {
            get => _minus;
            set
            {
                if (_minus != null)
                {
                    _minus.Click -= _minusClick;
                    Controls.Remove(_minus);
                }

                _minus = value;
                if (_minus == null)
                    return;

                if (_minus.Parent == null)
                    Controls.Add(_minus);

                _minusClick = new Button.ClickDelegate(OnMinus);
                _minus.Click += _minusClick;
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
                    return int.Parse(_editLine.Text);
                }
                catch
                {
                    return 0;
                }
            }
            set
            {
                if (_editLine != null)
                {
                    Plus.Enable = true;
                    Minus.Enable = true;
                    if (_max != 0 && value > _max)
                    {
                        value = _max;
                        Plus.Enable = false;
                    }
                    else if (_min != 0 && value < _min)
                    {
                        value = _min;
                        Minus.Enable = false;
                    }

                    if (value == 0)
                        _editLine.Text = "0";
                    else
                        _editLine.Text = value.ToString();
                }
            }
        }

        [Category("Counter")]
        [DefaultValue(1)]
        [Serialize]
        public int Step { get; set; } = 1;

        [Category("Counter")]
        [DefaultValue(0)]
        [Serialize]
        public int Min
        {
            get => _min;
            set
            {
                _min = value;
                if (_min > _max)
                    _min = _max - 1;
            }
        }

        [Category("Counter")]
        [DefaultValue(0)]
        [Serialize]
        public int Max
        {
            get => _max;
            set
            {
                _max = value;
                if (_max < _min)
                    _max = _min + 1;
            }
        }

        protected override StandardChildSlotItem[] OnGetStandardChildSlots()
        {
            return new StandardChildSlotItem[3]
              {
                new StandardChildSlotItem("EditLine", EditLine),
                new StandardChildSlotItem("Plus", Plus),
                new StandardChildSlotItem("Minus", Minus)
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
            var numbers = "-0123456789";
            var str = "";

            foreach (var c in _editLine.Text)
            {
                foreach (var n in numbers)
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

            _editLine.Text = str;
            OnValueChange();
        }

        public void OnValueChange()
        {
            ValueChange?.Invoke(this, Value);
        }

        public void OnMinus(Button sender = null)
        {
            Plus.Enable = true;
            Value -= Step;

            if (Value - Step < _min)
                Minus.Enable = false;
        }

        void OnPlus(Button sender = null)
        {
            Minus.Enable = true;
            Value += Step;

            if (_max != 0 && Value + Step > _max)
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