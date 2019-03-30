using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using Engine;
using Engine.EntitySystem;
using Engine.Renderer;
using Engine.MapSystem;
using Engine.MathEx;
using Engine.Utils;
using Engine.UISystem;

namespace Game
{
    class MapingWinow : Window
    {
        public MapingWinow()
            : base("MapingWindows")
        {
            ScaleValue p = V3dWindow.Instance.Position;
            Position = new ScaleValue(p.Type, p.Value - Size.Value);
        }
    }
}