using Engine.MathEx;
using Engine.Renderer;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;

namespace Engine.UISystem
{
    public class GraphLine2D : Control
    {
        List<int> Data = new List<int>();
        List<float> Buffer = new List<float>();
        private ColorValue lineColor = new ColorValue(1, 0, 0);
        private ColorValue zone0Color = new ColorValue(.35f, .92f, .92f, .35f);
        private ColorValue zone1Color = new ColorValue(.92f, .35f, .35f, .35f);
        private float zone0;
        private float zone1;

        [Category("Graph")]
        [DefaultValue(typeof(ColorValue), "255 0 0")]
        [Serialize]
        public ColorValue LineColor
        {
            get { return lineColor; }
            set { lineColor = value; }
        }

        [Category("Graph")]
        [DefaultValue(typeof(ColorValue), "90 235 235 90")]
        [Serialize]
        public ColorValue Zone0Color
        {
            get { return zone0Color; }
            set { zone0Color = value; }
        }

        [Category("Graph")]
        [DefaultValue(typeof(ColorValue), "235 90 90 90")]
        [Serialize]
        public ColorValue Zone1Color
        {
            get { return zone1Color; }
            set { zone1Color = value; }
        }

        [Category("Graph")]
        [DefaultValue(0.0f)]
        [Serialize]
        public float Zone0
        {
            get { return zone0; }
            set
            {
                zone0 = value;

                if (zone0 < 0)
                    zone0 = 0;
                else if (zone0 > 1)
                    zone0 = 1;
            }
        }

        [Category("Graph")]
        [DefaultValue(0.0f)]
        [Serialize]
        public float Zone1
        {
            get { return zone1; }
            set
            {
                zone1 = value;

                if (zone1 < 0)
                    zone0 = 0;
                else if (zone1 + zone0 > 1)
                    zone1 = 1 - zone0;
            }
        }

        protected override void OnRenderUI(GuiRenderer renderer)
        {
            base.OnRenderUI(renderer);

            Vec2 offest = GetScreenPosition();
            Vec2 scale = GetScreenSize();

            for (int i = 0; i < Buffer.Count - 1; i++)
            {
                Vec2 In = offest + new Vec2((float)i / (float)Buffer.Count, Buffer[i]) * scale;
                Vec2 To = offest + new Vec2((float)(i + 1) / (float)Buffer.Count, Buffer[i + 1]) * scale;

                renderer.AddLine(In, To, LineColor);
            }

            if (zone0 != 0)
                renderer.AddQuad(offest + new Rect(0, 0, zone0, 1) * scale, zone0Color);
            if (zone1 != 0)
                renderer.AddQuad(offest + new Rect(0, 0, zone0 + zone1, 1) * scale, zone1Color);
        }

        void UpdateBuffer()
        {
            Buffer = new List<float>();
            int max = 0;

            int step = (int)Math.Ceiling(Data.Count / ((int)EngineApp.Instance.VideoMode.X * (double)GetScreenSize().X));

            for (int i = 0; i < Data.Count; i += step)
            {
                if (Data[i] > max)
                    max = Data[i];

                Buffer.Add(Data[i]);
            }

            for (int i = 0; i < Buffer.Count; i++)
                Buffer[i] = 1 - Buffer[i] / (float)max;
        }

        public void SetData(List<int> buffer)
        {
            Data = buffer;
            UpdateBuffer();
        }

        public void AddData(int i)
        {
            Data.Add(i);
            UpdateBuffer();
        }

        public void RemoveData(int index)
        {
            Data.RemoveAt(index);
            UpdateBuffer();
        }

        public void ClearData()
        {
            Data.Clear();
            Buffer.Clear();
        }
    }
}
