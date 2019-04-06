using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using Engine;
using Engine.Renderer;
using Engine.MathEx;
using Engine.Renderer.ModelImporting;
using Engine.Utils;
using Engine.UISystem;
using FBXModelImport;
using RVertex = Engine.Renderer.DynamicMeshManager.Vertex;

namespace Game
{
    public class ModelViewWindow : Window
    {
        private static ModelViewWindow _instance;
        private MapingWinow _mapWindow;
        private List<byte> _vertex;
        private List<byte> _face;
        private List<byte> _skelet;
        private List<Vertex> _vertexs;
        private List<Face> _faces;
        private VFile _vFile;
        private SceneBox _viewport;
        private SceneBox.SceneBoxMesh _obj;
        private VertexType _vertexType;
        private Mesh _mesh;
        private int _vertexSize;
        private int _faceSize;
        private int _skeletSize;
        private float _zoom;
        private SphereDir _dir;
        private bool _viewportIsRotation;
        private Vec2 _viewportMouseOffest;
        private Vec2 _pointMouseOffset;
        private bool _pointIsMove;
        private ushort[] _indicesToMemory;
        private RVertex[] _verticesToMemory;

        public static ModelViewWindow Instance
        {
            get { return _instance; }
        }

        public ModelViewWindow(VFile vFile) : base("3dWindow")
        {
            _instance?.Close();
            _instance = this;

            Init(vFile);
        }

        public void Init(VFile vFile)
        {
            _vFile?.ClearCache();
            _vFile = vFile;

            window.Text = vFile.Name;
            _viewport = (SceneBox) window.Controls["viewport"];

            ((IntCounter) window.Controls["tab\\format\\size"]).Focus();
            ((IntCounter) window.Controls["tab\\lod\\pos"]).ValueChange += LodPosition_ValueChange;
            ((Button) window.Controls["tab\\format\\render"]).Click += OnUpdate;
            ((Button) window.Controls["manipul\\top"]).Click += MoveTop;
            ((Button) window.Controls["manipul\\reset"]).Click += MoveReset;
            ((Button) window.Controls["manipul\\down"]).Click += MoveDown;
            ((Button) window.Controls["maping"]).Click += Maping_Click;
            ((Button) window.Controls["tab\\export\\export"]).Click += ExportClick;
            window.Controls["tab\\lod\\point"].MouseDown += PointMouseDown;
            window.Controls["tab\\lod\\point"].MouseMove += PointMouseMove;
            window.Controls["tab\\lod\\point"].MouseUp += PointMouseUp;
            _viewport.MouseLeave += viewport_MouseLeave;
            _viewport.MouseUp += viewport_MouseUp;
            _viewport.MouseDown += viewport_MouseDown;
            _viewport.MouseMove += viewport_MouseMove;
            _viewport.MouseWheel += viewport_MouseWheel;

            _vertexSize = BitConverter.ToInt32(_vFile.Data.GetRange(4, 4).ToArray(), 0);
            
            if(_vFile.Data.Count <= _vertexSize + 12)
                return;

            _faceSize = BitConverter.ToInt32(_vFile.Data.GetRange(12 + _vertexSize, 4).ToArray(), 0);
            _skeletSize = BitConverter.ToInt32(_vFile.Data.GetRange(20 + _faceSize + _vertexSize, 4).ToArray(), 0);

            _vertex = _vFile.Data.GetRange(8, _vertexSize);
            _face = _vFile.Data.GetRange(16 + _vertexSize, _faceSize);

            AutoFormat();

            _obj = _viewport.FindObjectByName("model") as SceneBox.SceneBoxMesh;
            if (_obj != null)
            {
                var meshName = MeshManager.Instance.GetUniqueName("_viewport_temp");

                _mesh = MeshManager.Instance.CreateManual(meshName);

                var subMesh = _mesh.CreateSubMesh();
                subMesh.UseSharedVertices = false;

                var dec = subMesh.VertexData.VertexDeclaration;
                dec.AddElement(0, 0, VertexElementType.Float3, VertexElementSemantic.Position);
                dec.AddElement(0, 12, VertexElementType.Float3, VertexElementSemantic.Normal);
                dec.AddElement(0, 24, VertexElementType.Float2, VertexElementSemantic.TextureCoordinates, 0);

                var usage = HardwareBuffer.Usage.DynamicWriteOnly;
                var vertexBuffer = HardwareBufferManager.Instance.CreateVertexBuffer(
                    Marshal.SizeOf(typeof(RVertex)), _vertexSize, usage);
                
                subMesh.VertexData.VertexBufferBinding.SetBinding(0, vertexBuffer, true);
                subMesh.VertexData.VertexCount = _vertexSize;

                var indexBuffer =
                    HardwareBufferManager.Instance.CreateIndexBuffer(HardwareIndexBuffer.IndexType._16Bit, _faceSize,
                        usage);
                
                subMesh.IndexData.SetIndexBuffer(indexBuffer, true);
                subMesh.IndexData.IndexCount = _faceSize;

                _obj.MeshName = meshName;
                _obj.OverrideMaterial = "Blank";
            }

            _dir = new SphereDir();
            _zoom = 3;
            _viewport.CameraPosition = _dir.GetVector() * _zoom;

            OnUpdate(null);
        }

        private void Maping_Click(Button sender)
        {
            if (_mapWindow == null)
                _mapWindow = new MapingWinow();
            else
                _mapWindow.Focus();
        }

        protected override void OnRender()
        {
            if (_viewport.Camera == null)
                return;

            var dg = _viewport.Camera.DebugGeometry;

            if (((CheckBox) window.Controls["btn\\grid"]).Checked)
            {
                var size = 5;
                dg.Color = new ColorValue(1, 1, 1);
                for (var i = -size; i <= size; i++)
                {
                    dg.AddLine(new Vec3(i, -size, 0), new Vec3(i, size, 0));
                    dg.AddLine(new Vec3(-size, i, 0), new Vec3(size, i, 0));
                }
            }

            if (((CheckBox) window.Controls["btn\\gizmo"]).Checked)
            {
                dg.Color = new ColorValue(1, 0, 0);
                dg.AddLine(Vec3.Zero, Vec3.XAxis);
                dg.Color = new ColorValue(0, 1, 0);
                dg.AddLine(Vec3.Zero, Vec3.YAxis);
                dg.Color = new ColorValue(0, 0, 1);
                dg.AddLine(Vec3.Zero, Vec3.ZAxis);
            }

            if (((CheckBox) window.Controls["btn\\vertex"]).Checked && _vertexs != null)
            {
                foreach (var v in _vertexs)
                    dg.AddSphere(new Sphere(new Vec3(v.x, v.y, v.z) + _obj.Position, .005f), 16);
            }

            if (_viewport.Viewport != null)
                _viewport.Viewport.BackgroundColor = new ColorValue(.247f, .42f, 1);
        }

        private void viewport_MouseMove(Control sender)
        {
            if (!_viewportIsRotation)
                return;

            const float pi2 = (float) Math.PI / 2;
            const float multer = 2.5f;

            var pos = _viewport.MousePosition;
            var delta = pos - _viewportMouseOffest;

            _dir.Horizontal -= delta.X * multer;
            _dir.Vertical += delta.Y * multer;


            if (_dir.Vertical > (double) pi2)
                _dir.Vertical = pi2;
            if (_dir.Vertical < -(double) pi2)
                _dir.Vertical = -pi2;

            _viewport.CameraPosition = _dir.GetVector() * _zoom;
            _viewportMouseOffest = pos;
        }

        private void viewport_MouseDown(Control sender, EMouseButtons button)
        {
            if (button == EMouseButtons.Left)
            {
                _viewportMouseOffest = _viewport.MousePosition;
                _viewportIsRotation = true;
            }
        }

        private void viewport_MouseUp(Control sender, EMouseButtons button)
        {
            if (button == EMouseButtons.Left)
                _viewportIsRotation = false;
        }

        private void viewport_MouseLeave(Control sender)
        {
            _viewportIsRotation = false;
        }

        private void viewport_MouseWheel(Control sender, int delta)
        {
            _zoom += _zoom * 0.002f * delta;
            if (_zoom < 0.1f)
                _zoom = 0.1f;

            _viewport.CameraPosition = _dir.GetVector() * _zoom;
        }

        private void PointMouseUp(Control sender, EMouseButtons button)
        {
            if (button == EMouseButtons.Left)
                _pointIsMove = false;
        }

        private bool _updateFaceOfLodLevelFlag;

        private void UpdateFaceOfLodLevel(int x)
        {
            _updateFaceOfLodLevelFlag = true;
            var p = window.Controls["tab\\lod\\view"] as GraphLine2D;
            var c = window.Controls["tab\\lod\\point"];
            ((IntCounter) window.Controls["tab\\lod\\pos"]).Value = x;
            p.Zone0 = x / 100.0f;
            c.Position = new ScaleValue(ScaleType.Parent, new Vec2(x / 100.0f, c.Position.Value.Y));

            _updateFaceOfLodLevelFlag = false;
            _BuildFace();
            _CalculateNormal();
            _WriteToMemoryF();
        }

        private void PointMouseMove(Control sender)
        {
            if (_pointIsMove)
            {
                var p = window.Controls["tab\\lod\\view"] as GraphLine2D;

                var f = 1 / (p.Size.Value.X / sender.Size.Value.X);
                var x = p.MousePosition.X + _pointMouseOffset.X * f - f / 2;

                if (x > 1)
                    x = 1;
                else if (x < 0.01f)
                    x = 0.01f;

                UpdateFaceOfLodLevel((int) (x * 100));
            }
        }

        private void LodPosition_ValueChange(IntCounter control, int value)
        {
            if (_updateFaceOfLodLevelFlag)
                return;

            UpdateFaceOfLodLevel(value);
        }

        private void PointMouseDown(Control sender, EMouseButtons button)
        {
            if (button == EMouseButtons.Left)
            {
                _pointMouseOffset = sender.MousePosition;
                _pointIsMove = true;
            }
        }

        private void DeclarationToGui()
        {
            ((IntCounter) window.Controls["tab\\format\\size"]).Value = _vertexType.Size;
            ((IntCounter) window.Controls["tab\\format\\pos"]).Value = _vertexType.position;
            ((IntCounter) window.Controls["tab\\format\\uv"]).Value = _vertexType.texcoord0;
            ((IntCounter) window.Controls["tab\\format\\unknown"]).Value = _vertexType.unknown;
            ((IntCounter) window.Controls["tab\\format\\unknown_size"]).Value = _vertexType.Size - _vertexType.unknown;
        }

        private void DeclarationInGui()
        {
            _vertexType.Size = ((IntCounter) window.Controls["tab\\format\\size"]).Value;
            _vertexType.position = ((IntCounter) window.Controls["tab\\format\\pos"]).Value;
            _vertexType.texcoord0 = ((IntCounter) window.Controls["tab\\format\\uv"]).Value;
            _vertexType.unknown = ((IntCounter) window.Controls["tab\\format\\unknown"]).Value;
        }

        private void AutoFormat()
        {
            _vertexType = new VertexType();
            var sized = new List<int>();

            if (_vertexSize % 24 == 0)
                sized.Add(24);
            if (_vertexSize % 28 == 0)
                sized.Add(28);
            if (_vertexSize % 32 == 0)
                sized.Add(32);
            if (_vertexSize % 36 == 0)
                sized.Add(36);
            
            foreach (var i in sized)
            {
                if (IsValidVertexData(i))
                {
                    _vertexType.Size = i;
                    break;
                }
            }

            DeclarationToGui();
        }

        private bool IsValidVertexData(int size)
        {
            if ((_vertex[5 * size - 3] == 255) &&
                (_vertex[5 * size - 2] == 255) &&
                (_vertex[5 * size - 1] == 255))
                return true;

            if ((_vertex[7 * size - 3] == 255) &&
                (_vertex[7 * size - 2] == 255) &&
                (_vertex[7 * size - 1] == 255))
                return true;

            return false;
        }

        private void ExportClick(Button sender)
        {
            var saveWindow = new SaveFileDialog();
            saveWindow.OnFileSelect += Export;

            saveWindow.Show(_vFile.Name, new[] {"fbx"});
        }

        private void Export(string path)
        {
            switch (Path.GetExtension(path))
            {
                case ".fbx":
                    ExportFbx(path);
                    break;
            }
        }

        private void ExportFbx(string path)
        {
            var loader = new FBXModelImportLoader();

            var rotX = ((IntCounter) window.Controls["tab\\export\\rotateX"]).Value;
            var rotY = ((IntCounter) window.Controls["tab\\export\\rotateY"]).Value;
            var rotZ = ((IntCounter) window.Controls["tab\\export\\rotateZ"]).Value;
            var scale = ((IntCounter) window.Controls["tab\\export\\scale"]).Value;

            var mesh = _obj.MeshObject.Mesh;
            var items = mesh.SubMeshes.Select(subMesh => new ModelImportLoader.SaveGeometryItem
                {
                    Name = _obj.MeshObject.Mesh.Name,
                    IndexData = subMesh.IndexData,
                    VertexData = subMesh.VertexData,
                    Rotation = Quat.FromDirectionZAxisUp(new Vec3(rotX, rotY, rotZ)),
                    Scale = new Vec3(scale, scale, scale),
                })
                .ToList();

            loader.Save(items, path);
        }

        private void Render()
        {
            _BuildVertex();

            ReadFace();
            _BuildFace();

            _CalculateNormal();
            _WriteToMemoryV();
            _WriteToMemoryF();
        }

        private void ReadFace()
        {
            var buffer = new List<int>();
            _faces = Face.Read(_face);

            foreach (var f in _faces)
                buffer.Add(f.ToInt32());

            ((GraphLine2D) window.Controls["tab\\lod\\view"]).SetData(buffer);
        }

        private void _BuildFace()
        {
            if (_mesh == null)
                return;

            var end = _faces.Count / 100 * ((IntCounter) window.Controls["tab\\lod\\pos"]).Value * 3;

            _indicesToMemory = new ushort[_faceSize / 2];
            var offest = 0;

            foreach (var f in _faces)
            {
                _indicesToMemory[offest] = (ushort) (f.a - 1);
                _indicesToMemory[offest + 1] = (ushort) (f.b - 1);
                _indicesToMemory[offest + 2] = (ushort) (f.c - 1);
                offest += 3;

                if (offest >= end)
                    break;
            }
        }

        private void _BuildVertex()
        {
            if (_mesh == null)
                return;

            var b = new Bounds();
            _vertexs = Vertex.Read(_vertex, _vertexType);
            _verticesToMemory = new RVertex[_vertexSize / _vertexType.Size];
            var offest = 0;

            foreach (var v in _vertexs)
            {
                _verticesToMemory[offest] =
                    new RVertex(new Vec3(v.x, v.y, v.z), Vec3.Zero, new Vec2(v.u, v.v));

                if (v.x > b.Maximum.X)
                    b.Maximum = new Vec3(v.x, b.Maximum.Y, b.Maximum.Z);
                else if (v.x < b.Minimum.X)
                    b.Minimum = new Vec3(v.x, b.Minimum.Y, b.Minimum.Z);
                if (v.y > b.Maximum.Y)
                    b.Maximum = new Vec3(b.Maximum.X, v.y, b.Maximum.Z);
                else if (v.y < b.Minimum.Y)
                    b.Maximum = new Vec3(b.Maximum.X, v.y, b.Maximum.Z);
                if (v.z > b.Maximum.Z)
                    b.Maximum = new Vec3(b.Maximum.X, b.Minimum.Y, v.z);
                else if (v.z < b.Minimum.Z)
                    b.Maximum = new Vec3(b.Maximum.X, b.Minimum.Y, v.z);

                offest++;
            }

            _mesh.SetBoundsAndRadius(b, b.GetRadius());

            _zoom = b.GetRadius();
            _viewport.CameraPosition = _dir.GetVector() * _zoom;
            
            MoveReset(null);
        }

        private unsafe void _CalculateNormal()
        {
            fixed (RVertex* verticesToMemory = _verticesToMemory)
            {
                var triangleCount = _indicesToMemory.Length / 3;
                for (var n = 0; n < triangleCount; n++)
                {
                    int index0 = _indicesToMemory[n * 3 + 0];
                    int index1 = _indicesToMemory[n * 3 + 1];
                    int index2 = _indicesToMemory[n * 3 + 2];

                    var pos0 = verticesToMemory[index0].position;
                    var pos1 = verticesToMemory[index1].position;
                    var pos2 = verticesToMemory[index2].position;

                    var normal = Vec3.Cross(pos1 - pos0, pos2 - pos0);
                    normal.Normalize();

                    verticesToMemory[index0].normal += normal;
                    verticesToMemory[index1].normal += normal;
                    verticesToMemory[index2].normal += normal;
                }

                for (var n = 0; n < _verticesToMemory.Length; n++)
                    verticesToMemory[n].normal = verticesToMemory[n].normal.GetNormalize();
            }
        }

        private unsafe void _WriteToMemoryV()
        {
            var vertexBuffer = _mesh.SubMeshes[0].VertexData.VertexBufferBinding.GetBuffer(0);
            var buffer = vertexBuffer.Lock(HardwareBuffer.LockOptions.Discard);

            fixed (RVertex* pVertices = _verticesToMemory)
                NativeUtils.CopyMemory(buffer, (IntPtr) pVertices, _verticesToMemory.Length * sizeof(RVertex));

            vertexBuffer.Unlock();
        }

        private unsafe void _WriteToMemoryF()
        {
            var indexBuffer = _mesh.SubMeshes[0].IndexData.IndexBuffer;
            var buffer = indexBuffer.Lock(HardwareBuffer.LockOptions.Discard);

            fixed (ushort* pIndices = _indicesToMemory)
                NativeUtils.CopyMemory(buffer, (IntPtr) pIndices, _indicesToMemory.Length * sizeof(ushort));

            indexBuffer.Unlock();
        }

        private void MoveTop(object flag)
        {
            _obj.Position += Vec3.ZAxis * .1f;
        }

        private void MoveReset(object flag)
        {
            _obj.Position = new Vec3(_obj.Position.X, _obj.Position.Y, -1 * _mesh.Bounds.GetSize().Z / 2);
        }

        private void MoveDown(object flag)
        {
            _obj.Position -= Vec3.ZAxis * .1f;
        }

        private void OnUpdate(object flag)
        {
            DeclarationInGui();
            Render();
        }

        protected override void OnDetach()
        {
            base.OnDetach();

            _vFile?.ClearCache();

            if (this == _instance)
                _instance = null;
        }
    }
}