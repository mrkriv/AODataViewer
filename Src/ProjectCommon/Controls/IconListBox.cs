using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using Engine;
using Engine.MathEx;
using Engine.Renderer;
using Engine.UISystem;

namespace ProjectCommon.Controls
{
    public class IconListBox : ListBox
    {
        private Button _btn;
        private ScrollBar _bar;
        private bool _showBar;
        private ScaleValue _clipRectangleBorders = new ScaleValue(ScaleType.ScaleByResolution, Vec2.Zero);
        private bool _hideSelectionWhenDisabled;
        private int _selectIndex = -1;
        private bool _needUpdate;
        private Vec2 _aMx;
        private int _aMy;
        private SelectedIndexChangeDelegate _selectedIndexChangeD;
        private ItemMouseEventHandler _handler;
        public readonly List<Button> Btns = new List<Button>();

        public class Item
        {
            public string Text { get; set; }
            public string Icon { get; set; }
            public object Data { get; set; }
        }

        [Category("List Box")]
        [DefaultValue("GUI\\Icon")]
        [Serialize]
        public string IconDir { get; set; } = "GUI\\Icon";

        public delegate void SelectedIndexChangeDelegate(IconListBox sender);

        public delegate void ItemMouseEventHandler(object sender, ItemMouseEventArgs e);

        [Category("List Box")]
        [DefaultValue(false)]
        [LogicSystemBrowsable(true)]
        [Serialize]
        public bool AlwaysShowScrollBar
        {
            get => _showBar;
            set
            {
                if (_showBar != value)
                {
                    _showBar = value;
                    _needUpdate = true;
                }
            }
        }

        [Category("List Box")]
        [Serialize]
        public ScaleValue ClipRectangleBorders
        {
            get => _clipRectangleBorders;
            set
            {
                _clipRectangleBorders = value;
                _needUpdate = true;
            }
        }

        [Category("List Box")]
        [DefaultValue(false)]
        [LogicSystemBrowsable(true)]
        [Serialize]
        public bool HideSelectionWhenDisabled
        {
            get => _hideSelectionWhenDisabled;
            set
            {
                if (_hideSelectionWhenDisabled != value)
                {
                    _hideSelectionWhenDisabled = value;
                    A2();
                }
            }
        }

        [Browsable(false)]
        [Serialize]
        public Button ItemButton
        {
            get => _btn;
            set
            {
                if (_btn != null)
                {
                    Controls.Remove(_btn);
                }

                _btn = value;
                if (_btn != null)
                {
                    if (_btn.Parent == null)
                    {
                        Controls.Add(_btn);
                    }

                    _btn.Visible = false;
                }

                RemoveBtn();
            }
        }

        [Browsable(false)]
        public List<Button> ItemButtons
        {
            get
            {
                if (_needUpdate)
                {
                    _needUpdate = false;
                    Update();
                }

                return Btns;
            }
        }

        [Browsable(false)]
        [LogicSystemBrowsable(true)]
        public IconListBoxItemCollection Items { get; }

        [Browsable(false)]
        [Serialize]
        public ScrollBar ScrollBar
        {
            get => _bar;
            set
            {
                if (_bar != null)
                {
                    _bar.ValueChange -= OnScroll;
                    Controls.Remove(_bar);
                }

                _bar = value;
                if (_bar != null)
                {
                    if (_bar.Parent == null)
                    {
                        Controls.Add(_bar);
                    }

                    _bar.ValueRange = new Range(0f, 1f);
                    _bar.Value = 0f;
                    _bar.ValueChange += OnScroll;
                }
            }
        }

        [Category("List Box")]
        [DefaultValue(-1)]
        [LogicSystemBrowsable(true)]
        public int SelectedIndex
        {
            get => _selectIndex;
            set
            {
                if (value < -1 || value >= Items.Count)
                {
                    throw new Exception("EComboBox: SelectedIndex: Set invalid value");
                }

                if (_selectIndex != value)
                {
                    if (_selectIndex != -1 && Btns.Count > _selectIndex)
                    {
                        Btns[_selectIndex].Active = false;
                    }

                    _selectIndex = value;
                    OnSelectedIndexChange();
                    if (_selectIndex != -1 && Btns.Count > _selectIndex)
                    {
                        var flag = true;
                        if (_hideSelectionWhenDisabled && !IsEnabledInHierarchy())
                        {
                            flag = false;
                        }

                        if (flag)
                        {
                            Btns[_selectIndex].Active = true;
                        }
                    }
                }

                if (_bar != null && _btn != null && _selectIndex != -1)
                {
                    var single = А1();
                    var screenSize = GetScreenSize();
                    var offsetByTypeFromLocal =
                        GetOffsetByTypeFromLocal(ScaleType.Screen, GetLocalOffsetByValue(_clipRectangleBorders));
                    var y = screenSize.Y - offsetByTypeFromLocal.Y * 2f;
                    var count = single * Items.Count;
                    var single1 = count - y;
                    if (single1 > 0f)
                    {
                        var maximum = _bar.Value * single1;
                        var single2 = single * _selectIndex;
                        var range = new Range(single2, single2 + single);
                        if (range.Minimum >= maximum)
                        {
                            if (range.Maximum > maximum + y)
                            {
                                maximum = range.Maximum + single - y;
                            }
                        }
                        else
                        {
                            maximum = range.Minimum;
                        }

                        _bar.Value = maximum / single1;
                    }
                }
            }
        }

        [Browsable(false)]
        [LogicSystemBrowsable(true)]
        public Item SelectedItem
        {
            get
            {
                if (_selectIndex != -1)
                {
                    return Items[_selectIndex];
                }

                return null;
            }
            set => SelectedIndex = Items.IndexOf(value);
        }

        public IconListBox()
        {
            Items = new IconListBoxItemCollection(this);
        }

        private float А1()
        {
            var screenSize = ItemButton.GetScreenSize();
            var y = screenSize.Y;
            if (_aMy != 0)
            {
                var single = (float) _aMy;
                y = y * single;
                y = (int) y;
                y = y / single;
            }

            return y;
        }

        private void OnScroll(ScrollBar a)
        {
            _needUpdate = true;
        }

        private void RemoveBtn()
        {
            foreach (var button in Btns)
            {
                button.Click -= BunClick;
                button.MouseDoubleClick -= MouseDoubleClick;
                button.UserData = null;
                Controls.Remove(button);
            }

            Btns.Clear();
        }

        private void BunClick(object a)
        {
            var btn = (Button) a;
            if (btn.UserData != null)
            {
                var userData = (int) btn.UserData;
                if (userData < Items.Count)
                {
                    SelectedIndex = userData;
                }
            }
        }

        private void MouseDoubleClick(object sender, EMouseButtons @event)
        {
            if (IsEnabledInHierarchy())
            {
                var button = (Button) sender;
                if (button.UserData != null)
                {
                    var userData = (int) button.UserData;
                    if (userData < Items.Count)
                    {
                        OnItemMouseDoubleClick(userData);
                    }
                }
            }
        }

        private void A2()
        {
            if (_selectIndex != -1 && Btns.Count > _selectIndex)
            {
                var flag = !(_hideSelectionWhenDisabled && !IsEnabledInHierarchy());
                Btns[_selectIndex].Active = flag;
            }
        }

        private void Update()
        {
            if (_btn == null)
                return;

            if (Items.Count < Btns.Count)
            {
                RemoveBtn();
            }

            while (Btns.Count < Items.Count)
            {
                var count = Btns.Count;
                var btn = (Button) _btn.Clone();
                btn.FileNameCreated = null;
                btn.FileNameDeclared = null;
                btn.UserData = count;
                btn.Position = new ScaleValue(ScaleType.Screen, btn.GetScreenPosition() + new Vec2(0f, А1() * count));
                btn.Visible = true;
                btn.Click += BunClick;
                btn.MouseDoubleClick += MouseDoubleClick;
                btn._AllowSave = false;
                btn._AllowClone = false;
                Controls.Add(btn);
                Btns.Add(btn);
            }

            var value = 0f;
            if (_bar != null)
            {
                value = _bar.Value;
            }

            var single1 = А1();
            var count1 = single1 * Items.Count;
            var screenSize = GetScreenSize();
            var offsetByTypeFromLocal =
                GetOffsetByTypeFromLocal(ScaleType.Screen, GetLocalOffsetByValue(_clipRectangleBorders));
            var y = screenSize.Y - offsetByTypeFromLocal.Y * 2f;
            var single2 = count1 - y;
            var single = (single2 <= 0f ? 0f : -value * single2);
            if (_bar != null)
            {
                var scrollBar = _bar;
                var flag = (single2 > 0f ? true : _showBar);
                scrollBar.Visible = flag;
            }

            for (var i = 0; i < Items.Count; i++)
            {
                if (Btns.Count <= i)
                    break;

                var item = Btns[i];
                var it = Items[i];
                item.Text = it.Text;

                if (item.Controls[0].Controls.Count != 0 && !string.IsNullOrEmpty(it.Icon))
                {
                    foreach (var c in item.Controls)
                        c.Controls[0].BackTexture = TextureManager.Instance.Load(IconDir + "\\" + it.Icon + ".png");
                }


                var active = _selectIndex == i;
                if (_hideSelectionWhenDisabled && !IsEnabledInHierarchy())
                {
                    active = false;
                }

                item.Active = active;
                item.Position = new ScaleValue(ScaleType.Screen,
                    _btn.GetScreenPosition() + new Vec2(0f, single + single1 * i));
                var screenRectangle = GetScreenRectangle();
                item.Visible = screenRectangle.IsIntersectsRect(item.GetScreenRectangle());
                var rect = GetScreenRectangle();
                rect.Expand(-GetOffsetByTypeFromLocal(ScaleType.Screen, GetLocalOffsetByValue(_clipRectangleBorders)));
                item.SetScreenClipRectangle(rect);
            }
        }

        protected override void OnControlDetach(Control control)
        {
            base.OnControlDetach(control);
            if (control == ItemButton)
            {
                ItemButton = null;
            }

            if (control == ScrollBar)
            {
                ScrollBar = null;
            }
        }

        protected override StandardChildSlotItem[] OnGetStandardChildSlots()
        {
            var standardChildSlotItem = new[]
            {
                new StandardChildSlotItem("ItemButton", ItemButton), new StandardChildSlotItem("ScrollBar", ScrollBar)
            };
            return standardChildSlotItem;
        }

        protected void OnItemMouseDoubleClick(int itemIndex)
        {
            _handler?.Invoke(this, new ItemMouseEventArgs(itemIndex, Items[itemIndex]));
        }

        protected override bool OnMouseWheel(int delta)
        {
            if (Visible)
            {
                var rect = new Rect(0f, 0f, 1f, 1f);
                if (rect.IsContainsPoint(MousePosition) && IsEnabledInHierarchy() && _bar != null &&
                    _bar.IsEnabledInHierarchy())
                {
                    var value = _bar;
                    value.Value = value.Value - delta / 1500f;
                    return true;
                }
            }

            return base.OnMouseWheel(delta);
        }

        protected override void OnRenderUI(GuiRenderer renderer)
        {
            if (renderer.IsScreen)
            {
                var dimensionsInPixels = renderer.ViewportForScreenGuiRenderer.DimensionsInPixels;
                var size = dimensionsInPixels.Size;
                var y = size.Y;
                if (y != _aMy)
                {
                    _aMy = y;
                    _needUpdate = true;
                }
            }

            if (_needUpdate)
            {
                _needUpdate = false;
                Update();
            }

            base.OnRenderUI(renderer);
        }

        protected override void OnResize()
        {
            base.OnResize();
            _needUpdate = true;
        }

        protected virtual void OnSelectedIndexChange()
        {
            _selectedIndexChangeD?.Invoke(this);
        }

        protected override void OnSetEnable()
        {
            base.OnSetEnable();
            if (_hideSelectionWhenDisabled)
            {
                A2();
            }
        }

        protected override void OnTick(float delta)
        {
            base.OnTick(delta);
            var screenPosition = GetScreenPosition();
            if (screenPosition != _aMx)
            {
                _needUpdate = true;
                _aMx = screenPosition;
            }

            if (_needUpdate)
            {
                _needUpdate = false;
                Update();
            }
        }

        [LogicSystemBrowsable(true)]
        public event ItemMouseEventHandler ItemMouseDoubleClick
        {
            add => _handler += value;
            remove => _handler -= value;
        }

        [LogicSystemBrowsable(true)]
        public event SelectedIndexChangeDelegate SelectedIndexChange
        {
            add => _selectedIndexChangeD += value;
            remove => _selectedIndexChangeD -= value;
        }

        [LogicSystemBrowsable(true)]
        public class IconListBoxItemCollection : IList<Item>, ICollection<Item>, IEnumerable<Item>, IEnumerable
        {
            private List<Item> _items;

            [LogicSystemBrowsable(true)] public int Count => _items.Count;

            public bool IsReadOnly => false;

            [LogicSystemBrowsable(true)]
            public Item this[int index]
            {
                get => _items[index];
                set => throw new Exception("Not implemented.");
            }

            public IconListBox Owner { get; }

            internal IconListBoxItemCollection(IconListBox a)
            {
                _items = new List<Item>();
                Owner = a;
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return _items.GetEnumerator();
            }

            [LogicSystemBrowsable(true)]
            public void Add(string text, string icon, object data = null)
            {
                Add(new Item {Text = text, Icon = icon, Data = data});
            }

            [LogicSystemBrowsable(true)]
            public void Add(Item item)
            {
                _items.Add(item);
                Owner._needUpdate = true;
            }

            [LogicSystemBrowsable(true)]
            public void Clear()
            {
                Owner.SelectedIndex = -1;
                _items.Clear();
                Owner._needUpdate = true;
            }

            [LogicSystemBrowsable(true)]
            public bool Contains(Item item)
            {
                return _items.Contains(item);
            }

            public void CopyTo(Item[] array, int arrayIndex)
            {
                _items.CopyTo(array, arrayIndex);
            }

            public IEnumerator<Item> GetEnumerator()
            {
                return _items.GetEnumerator();
            }

            [LogicSystemBrowsable(true)]
            public int IndexOf(Item item)
            {
                return _items.IndexOf(item);
            }

            [LogicSystemBrowsable(true)]
            public void Insert(int index, Item item)
            {
                _items.Insert(index, item);
                Owner._needUpdate = true;
            }

            [LogicSystemBrowsable(true)]
            public bool Remove(Item item)
            {
                var num = _items.IndexOf(item);
                if (num != -1)
                {
                    RemoveAt(num);
                    return true;
                }

                return false;
            }

            [LogicSystemBrowsable(true)]
            public void RemoveAt(int index)
            {
                if (index < 0 || index >= _items.Count)
                {
                    throw new Exception("EComboBox: Items: Remove at invalid index");
                }

                if (index == Owner.SelectedIndex)
                {
                    Owner.SelectedIndex = index - 1;
                }

                _items.RemoveAt(index);
                Owner._needUpdate = true;
            }
        }
    }
}