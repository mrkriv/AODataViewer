using Engine.MathEx;
using Engine.Renderer;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;

namespace Engine.UISystem
{
    public class IconListBox : ListBox
    {
        private Button btn;
        private ScrollBar bar;
        private bool showBar;
        private Control.ScaleValue clipRectangleBorders = new Control.ScaleValue(Control.ScaleType.ScaleByResolution, Vec2.Zero);
        private bool hideSelectionWhenDisabled;
        private IconListBoxItemCollection items;
        private int selectIndex = -1;
        private List<Button> btns = new List<Button>();
        private bool needUpdate;
        private Vec2 aMx;
        private int aMY;
        private SelectedIndexChangeDelegate SelectedIndexChangeD;
        private ItemMouseEventHandler Handler;

        string iconDir = "GUI\\Icon";

        [Category("List Box")]
        [DefaultValue("GUI\\Icon")]
        [Serialize]
        public string IconDir
        {
            get { return iconDir; }
            set { iconDir = value; }
        }

        public delegate void SelectedIndexChangeDelegate(IconListBox sender);
        public delegate void ItemMouseEventHandler(object sender, ItemMouseEventArgs e);

        [Category("List Box")]
        [DefaultValue(false)]
        [LogicSystemBrowsable(true)]
        [Serialize]
        public bool AlwaysShowScrollBar
        {
            get
            {
                return showBar;
            }
            set
            {
                if (showBar != value)
                {
                    showBar = value;
                    needUpdate = true;
                    return;
                }
                else
                {
                    return;
                }
            }
        }

        [Category("List Box")]
        [Serialize]
        public Control.ScaleValue ClipRectangleBorders
        {
            get
            {
                return clipRectangleBorders;
            }
            set
            {
                clipRectangleBorders = value;
                needUpdate = true;
            }
        }

        [Category("List Box")]
        [DefaultValue(false)]
        [LogicSystemBrowsable(true)]
        [Serialize]
        public bool HideSelectionWhenDisabled
        {
            get
            {
                return hideSelectionWhenDisabled;
            }
            set
            {
                if (hideSelectionWhenDisabled != value)
                {
                    hideSelectionWhenDisabled = value;
                    b();
                    return;
                }
                else
                {
                    return;
                }
            }
        }

        [Browsable(false)]
        [Serialize]
        public Button ItemButton
        {
            get
            {
                return btn;
            }
            set
            {
                if (btn != null)
                {
                    base.Controls.Remove(btn);
                }
                btn = value;
                if (btn != null)
                {
                    if (btn.Parent == null)
                    {
                        base.Controls.Add(btn);
                    }
                    btn.Visible = false;
                }
                RemoveBtn();
            }
        }

        [Browsable(false)]
        public List<Button> ItemButtons
        {
            get
            {
                if (needUpdate)
                {
                    needUpdate = false;
                    Update();
                }
                return btns;
            }
        }

        [Browsable(false)]
        [LogicSystemBrowsable(true)]
        public IconListBoxItemCollection Items
        {
            get
            {
                return items;
            }
        }

        [Browsable(false)]
        [Serialize]
        public ScrollBar ScrollBar
        {
            get
            {
                return bar;
            }
            set
            {
                if (bar != null)
                {
                    bar.ValueChange -= new ScrollBar.ValueChangeDelegate(OnScroll);
                    base.Controls.Remove(bar);
                }
                bar = value;
                if (bar != null)
                {
                    if (bar.Parent == null)
                    {
                        base.Controls.Add(bar);
                    }
                    bar.ValueRange = new Range(0f, 1f);
                    bar.Value = 0f;
                    bar.ValueChange += new ScrollBar.ValueChangeDelegate(OnScroll);
                }
            }
        }

        [Category("List Box")]
        [DefaultValue(-1)]
        [LogicSystemBrowsable(true)]
        public int SelectedIndex
        {
            get
            {
                return selectIndex;
            }
            set
            {
                if (value < -1 || value >= items.Count)
                {
                    throw new Exception("EComboBox: SelectedIndex: Set invalid value");
                }
                else
                {
                    if (selectIndex != value)
                    {
                        if (selectIndex != -1 && btns.Count > selectIndex)
                        {
                            btns[selectIndex].Active = false;
                        }
                        selectIndex = value;
                        OnSelectedIndexChange();
                        if (selectIndex != -1 && btns.Count > selectIndex)
                        {
                            bool flag = true;
                            if (hideSelectionWhenDisabled && !base.IsEnabledInHierarchy())
                            {
                                flag = false;
                            }
                            if (flag)
                            {
                                btns[selectIndex].Active = true;
                            }
                        }
                    }
                    if (bar != null && btn != null && selectIndex != -1)
                    {
                        float single = a();
                        Vec2 screenSize = base.GetScreenSize();
                        Vec2 offsetByTypeFromLocal = base.GetOffsetByTypeFromLocal(Control.ScaleType.Screen, base.GetLocalOffsetByValue(clipRectangleBorders));
                        float y = screenSize.Y - offsetByTypeFromLocal.Y * 2f;
                        float count = single * (float)items.Count;
                        float single1 = count - y;
                        if (single1 > 0f)
                        {
                            float maximum = bar.Value * single1;
                            float single2 = single * (float)selectIndex;
                            Range range = new Range(single2, single2 + single);
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
                            bar.Value = maximum / single1;
                        }
                    }
                    return;
                }
            }
        }

        [Browsable(false)]
        [LogicSystemBrowsable(true)]
        public object SelectedItem
        {
            get
            {
                if (selectIndex != -1)
                {
                    return items[selectIndex];
                }
                else
                {
                    return null;
                }
            }
            set
            {
                SelectedIndex = items.IndexOf(value);
            }
        }

        public IconListBox()
        {
            items = new IconListBoxItemCollection(this);
        }

        private float a()
        {
            Vec2 screenSize = ItemButton.GetScreenSize();
            float y = screenSize.Y;
            if (aMY != 0)
            {
                float single = (float)aMY;
                y = y * single;
                y = (float)((int)y);
                y = y / single;
            }
            return y;
        }

        private void OnScroll(ScrollBar A)
        {
            needUpdate = true;
        }

        private void RemoveBtn()
        {
            foreach (Button button in btns)
            {
                button.Click -= new Button.ClickDelegate(BunClick);
                button.MouseDoubleClick -= new Control.MouseButtonDelegate(MouseDoubleClick);
                button.UserData = null;
                base.Controls.Remove(button);
            }
            btns.Clear();
        }

        private void BunClick(object A)
        {
            Button btn = (Button)A;
            if (btn.UserData != null)
            {
                int userData = (int)btn.UserData;
                if (userData < items.Count)
                {
                    SelectedIndex = userData;
                    return;
                }
                else
                {
                    return;
                }
            }
            else
            {
                return;
            }
        }

        private void MouseDoubleClick(object A, EMouseButtons a)
        {
            if (base.IsEnabledInHierarchy())
            {
                Button button = (Button)A;
                if (button.UserData != null)
                {
                    int userData = (int)button.UserData;
                    if (userData < items.Count)
                    {
                        OnItemMouseDoubleClick(userData);
                        return;
                    }
                    else
                    {
                        return;
                    }
                }
                else
                {
                    return;
                }
            }
            else
            {
                return;
            }
        }

        private void b()
        {
            if (selectIndex != -1 && btns.Count > selectIndex)
            {
                bool flag = true;
                if (hideSelectionWhenDisabled && !base.IsEnabledInHierarchy())
                {
                    flag = false;
                }
                btns[selectIndex].Active = flag;
            }
        }

        private void Update()
        {
            float single;
            bool flag;
            if (btn != null)
            {
                if (items.Count < btns.Count)
                {
                    RemoveBtn();
                }
                while (btns.Count < items.Count)
                {
                    int count = btns.Count;
                    Button _btn = (Button)btn.Clone();
                    _btn.FileNameCreated = null;
                    _btn.FileNameDeclared = null;
                    _btn.UserData = count;
                    _btn.Position = new Control.ScaleValue(Control.ScaleType.Screen, _btn.GetScreenPosition() + new Vec2(0f, a() * (float)count));
                    _btn.Visible = true;
                    _btn.Click += new Button.ClickDelegate(BunClick);
                    _btn.MouseDoubleClick += new Control.MouseButtonDelegate(MouseDoubleClick);
                    _btn._AllowSave = false;
                    _btn._AllowClone = false;
                    base.Controls.Add(_btn);
                    btns.Add(_btn);
                }
                float value = 0f;
                if (bar != null)
                {
                    value = bar.Value;
                }
                float single1 = a();
                float count1 = single1 * (float)items.Count;
                Vec2 screenSize = base.GetScreenSize();
                Vec2 offsetByTypeFromLocal = base.GetOffsetByTypeFromLocal(Control.ScaleType.Screen, base.GetLocalOffsetByValue(clipRectangleBorders));
                float y = screenSize.Y - offsetByTypeFromLocal.Y * 2f;
                float single2 = count1 - y;
                single = (single2 <= 0f ? 0f : -value * single2);
                if (bar != null)
                {
                    ScrollBar scrollBar = bar;
                    flag = (single2 > 0f ? true : showBar);
                    scrollBar.Visible = flag;
                }
                for (int i = 0; i < items.Count; i++)
                {
                    if (btns.Count <= i)
                        break;
                    Button item = btns[i];
                    if (items[i] is string[])
                    {
                        string[] it = items[i] as string[];
                        item.Text = it[0];
                        if (item.Controls[0].Controls.Count != 0 && it.Length > 1 && it[1] != "")
                        {
                            foreach (Control c in item.Controls)
                                c.Controls[0].BackTexture = TextureManager.Instance.Load(iconDir + "\\" + it[1] + ".png");
                        }
                    }
                    else
                        item.Text = items[i].ToString();

                    bool active = selectIndex == i;
                    if (hideSelectionWhenDisabled && !base.IsEnabledInHierarchy())
                    {
                        active = false;
                    }
                    item.Active = active;
                    item.Position = new Control.ScaleValue(Control.ScaleType.Screen, btn.GetScreenPosition() + new Vec2(0f, single + single1 * (float)i));
                    Rect screenRectangle = base.GetScreenRectangle();
                    item.Visible = screenRectangle.IsIntersectsRect(item.GetScreenRectangle());
                    Rect rect = base.GetScreenRectangle();
                    rect.Expand(-base.GetOffsetByTypeFromLocal(Control.ScaleType.Screen, base.GetLocalOffsetByValue(clipRectangleBorders)));
                    item.SetScreenClipRectangle(rect);
                }
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

        protected override Control.StandardChildSlotItem[] OnGetStandardChildSlots()
        {
            Control.StandardChildSlotItem[] standardChildSlotItem = new Control.StandardChildSlotItem[] { new Control.StandardChildSlotItem("ItemButton", ItemButton), new Control.StandardChildSlotItem("ScrollBar", ScrollBar) };
            return standardChildSlotItem;
        }

        protected void OnItemMouseDoubleClick(int itemIndex)
        {
            if (Handler != null)
            {
                Handler(this, new ItemMouseEventArgs(itemIndex, items[itemIndex]));
            }
        }

        protected override bool OnMouseWheel(int delta)
        {
            if (base.Visible)
            {
                Rect rect = new Rect(0f, 0f, 1f, 1f);
                if (rect.IsContainsPoint(base.MousePosition) && base.IsEnabledInHierarchy() && bar != null && bar.IsEnabledInHierarchy())
                {
                    ScrollBar value = bar;
                    value.Value = value.Value - (float)delta / 1500f;
                    return true;
                }
            }
            return base.OnMouseWheel(delta);
        }

        protected override void OnRenderUI(GuiRenderer renderer)
        {
            if (renderer.IsScreen)
            {
                RectI dimensionsInPixels = renderer.ViewportForScreenGuiRenderer.DimensionsInPixels;
                Vec2I size = dimensionsInPixels.Size;
                int y = size.Y;
                if (y != aMY)
                {
                    aMY = y;
                    needUpdate = true;
                }
            }
            if (needUpdate)
            {
                needUpdate = false;
                Update();
            }
            base.OnRenderUI(renderer);
        }

        protected override void OnResize()
        {
            base.OnResize();
            needUpdate = true;
        }

        protected virtual void OnSelectedIndexChange()
        {
            if (SelectedIndexChangeD != null)
            {
                SelectedIndexChangeD(this);
            }
        }

        protected override void OnSetEnable()
        {
            base.OnSetEnable();
            if (hideSelectionWhenDisabled)
            {
                b();
            }
        }

        protected override void OnTick(float delta)
        {
            base.OnTick(delta);
            Vec2 screenPosition = base.GetScreenPosition();
            if (screenPosition != aMx)
            {
                needUpdate = true;
                aMx = screenPosition;
            }
            if (needUpdate)
            {
                needUpdate = false;
                Update();
            }
        }

        [LogicSystemBrowsable(true)]
        public event ItemMouseEventHandler ItemMouseDoubleClick
        {
            add
            {
                Handler += value;
            }
            remove
            {
                Handler -= value;
            }
        }

        [LogicSystemBrowsable(true)]
        public event SelectedIndexChangeDelegate SelectedIndexChange
        {
            add
            {
                SelectedIndexChangeD += value;
            }
            remove
            {
                SelectedIndexChangeD -= value;
            }
        }

        [LogicSystemBrowsable(true)]
        public class IconListBoxItemCollection : IList<object>, ICollection<object>, IEnumerable<object>, IEnumerable
        {
            private IconListBox owner;
            private List<object> items;

            [LogicSystemBrowsable(true)]
            public int Count
            {
                get
                {
                    return items.Count;
                }
            }

            public bool IsReadOnly
            {
                get
                {
                    return false;
                }
            }

            [LogicSystemBrowsable(true)]
            public object this [int index]
            {
                get
                {
                    return items[index];
                }
                set
                {
                    throw new Exception("Not implemented.");
                }
            }

            public IconListBox Owner
            {
                get
                {
                    return owner;
                }
            }

            internal IconListBoxItemCollection(IconListBox A)
            {
                items = new List<object>();
                owner = A;
            }

            IEnumerator System.Collections.IEnumerable.GetEnumerator()
            {
                return items.GetEnumerator();
            }

            [LogicSystemBrowsable(true)]
            public void Add(object item)
            {
                items.Add(item);
                owner.needUpdate = true;
            }

            [LogicSystemBrowsable(true)]
            public void Clear()
            {
                owner.SelectedIndex = -1;
                items.Clear();
                owner.needUpdate = true;
            }

            [LogicSystemBrowsable(true)]
            public bool Contains(object item)
            {
                return items.Contains(item);
            }

            public void CopyTo(object[] array, int arrayIndex)
            {
                items.CopyTo(array, arrayIndex);
            }

            public IEnumerator<object> GetEnumerator()
            {
                return items.GetEnumerator();
            }

            [LogicSystemBrowsable(true)]
            public int IndexOf(object item)
            {
                return items.IndexOf(item);
            }

            [LogicSystemBrowsable(true)]
            public void Insert(int index, object item)
            {
                items.Insert(index, item);
                owner.needUpdate = true;
            }

            [LogicSystemBrowsable(true)]
            public bool Remove(object item)
            {
                int num = items.IndexOf(item);
                if (num != -1)
                {
                    RemoveAt(num);
                    return true;
                }
                else
                {
                    return false;
                }
            }

            [LogicSystemBrowsable(true)]
            public void RemoveAt(int index)
            {
                if (index < 0 || index >= items.Count)
                {
                    throw new Exception("EComboBox: Items: Remove at invalid index");
                }
                else
                {
                    if (index == owner.SelectedIndex)
                    {
                        owner.SelectedIndex = index - 1;
                    }
                    items.RemoveAt(index);
                    owner.needUpdate = true;
                    return;
                }
            }
        }
    }
}