using System;
using System.Collections.Generic;
using System.ComponentModel;
using Engine;
using Engine.MathEx;
using Engine.Renderer;
using Engine.UISystem;

namespace ProjectCommon.Controls
{
    public class GraphLine2D : Control
    {
        private List<float> _buffer = new List<float>();
        private List<int> _data = new List<int>();
        private float _zone0;
        private float _zone1;

        [Category("Graph")]
        [DefaultValue(typeof(ColorValue), "255 0 0")]
        [Serialize]
        public ColorValue LineColor { get; set; } = new ColorValue(1, 0, 0);

        [Category("Graph")]
        [DefaultValue(typeof(ColorValue), "90 235 235 90")]
        [Serialize]
        public ColorValue Zone0Color { get; set; } = new ColorValue(.35f, .92f, .92f, .35f);

        [Category("Graph")]
        [DefaultValue(typeof(ColorValue), "235 90 90 90")]
        [Serialize]
        public ColorValue Zone1Color { get; set; } = new ColorValue(.92f, .35f, .35f, .35f);

        [Category("Graph")]
        [DefaultValue(0.0f)]
        [Serialize]
        public float Zone0
        {
            get => _zone0;
            set
            {
                _zone0 = value;

                if (_zone0 < 0)
                    _zone0 = 0;
                else if (_zone0 > 1)
                    _zone0 = 1;
            }
        }

        [Category("Graph")]
        [DefaultValue(0.0f)]
        [Serialize]
        public float Zone1
        {
            get => _zone1;
            set
            {
                _zone1 = value;

                if (_zone1 < 0)
                    _zone0 = 0;
                else if (_zone1 + _zone0 > 1)
                    _zone1 = 1 - _zone0;
            }
        }

        protected override void OnRenderUI(GuiRenderer renderer)
        {
            base.OnRenderUI(renderer);

            var offest = GetScreenPosition();
            var scale = GetScreenSize();

            for (var i = 0; i < _buffer.Count - 1; i++)
            {
                var In = offest + new Vec2(i / (float) _buffer.Count, _buffer[i]) * scale;
                var to = offest + new Vec2((i + 1) / (float) _buffer.Count, _buffer[i + 1]) * scale;

                renderer.AddLine(In, to, LineColor);
            }

            if (_zone0 > 0)
                renderer.AddQuad(offest + new Rect(0, 0, _zone0, 1) * scale, Zone0Color);

            if (_zone1 > 0)
                renderer.AddQuad(offest + new Rect(0, 0, _zone0 + _zone1, 1) * scale, Zone1Color);
        }

        void UpdateBuffer()
        {
            _buffer = new List<float>();
            var max = 0;

            var step = (int) Math.Ceiling(_data.Count / (EngineApp.Instance.VideoMode.X * (double) GetScreenSize().X));

            for (var i = 0; i < _data.Count; i += step)
            {
                if (_data[i] > max)
                    max = _data[i];

                _buffer.Add(_data[i]);
            }

            for (var i = 0; i < _buffer.Count; i++)
                _buffer[i] = 1 - _buffer[i] / max;
        }

        public void SetData(List<int> buffer)
        {
            _data = buffer;
            UpdateBuffer();
        }

        public void AddData(int i)
        {
            _data.Add(i);
            UpdateBuffer();
        }

        public void RemoveData(int index)
        {
            _data.RemoveAt(index);
            UpdateBuffer();
        }

        public void ClearData()
        {
            _data.Clear();
            _buffer.Clear();
        }
    }
}