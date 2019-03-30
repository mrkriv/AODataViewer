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
    public class V3dWindow : Window
    {
        static V3dWindow instance;
        MapingWinow mapWindow;
        List<byte> vertex;
        List<byte> face;
        List<byte> skelet;
        List<Vertex> Vertexs;
        List<Face> Faces;
        Data.File File;
        SceneBox viewport;
        SceneBox.SceneBoxMesh obj;
        VertexType declaration;
        Mesh mesh;
        int vertex_size;
        int face_size;
        int skelet_size;
        float zoom;
        SphereDir dir;
        private bool ViewportIsRotation = false;
        private Vec2 ViewportMouseOffest;
        private Vec2 PointMouseOffset;
        private bool PointIsMove = false;
        ushort[] indicesToMemory;
        Engine.Renderer.DynamicMeshManager.Vertex[] verticesToMemory;

        public static V3dWindow Instance
        {
            get { return V3dWindow.instance; }
        }

        public V3dWindow(Data.File file)
            : base("3dWindow")
        {
            if (instance != null)
                instance.Close();

            instance = this;

            Init(file);
        }

        public void Init(Data.File file)
        {
            if (File != null)
                File.ClearCache();
            File = file;
            File.ReadData(true);

            window.Text = file.GetOnlyName();
            viewport = (SceneBox)window.Controls["viewport"];

            ((IntCounter)window.Controls["tab\\format\\size"]).Focus();
            ((IntCounter)window.Controls["tab\\lod\\pos"]).ValueChange += LodPosition_ValueChange;
            ((Button)window.Controls["tab\\format\\render"]).Click += OnUpdate;
            ((Button)window.Controls["manipul\\top"]).Click += MoveTop;
            ((Button)window.Controls["manipul\\reset"]).Click += MoveReset;
            ((Button)window.Controls["manipul\\down"]).Click += MoveDown;
            ((Button)window.Controls["maping"]).Click += Maping_Click;
            ((Button)window.Controls["tab\\export\\export"]).Click += ExportClick;
            window.Controls["tab\\lod\\point"].MouseDown += PointMouseDown;
            window.Controls["tab\\lod\\point"].MouseMove += PointMouseMove;
            window.Controls["tab\\lod\\point"].MouseUp += PointMouseUp;
            viewport.MouseLeave += viewport_MouseLeave;
            viewport.MouseUp += viewport_MouseUp;
            viewport.MouseDown += viewport_MouseDown;
            viewport.MouseMove += viewport_MouseMove;
            viewport.MouseWheel += viewport_MouseWheel;

            vertex_size = BitConverter.ToInt32(File.Data.GetRange(4, 4).ToArray(), 0);
            face_size = BitConverter.ToInt32(File.Data.GetRange(12 + vertex_size, 4).ToArray(), 0);
            skelet_size = BitConverter.ToInt32(File.Data.GetRange(20 + face_size + vertex_size, 4).ToArray(), 0);

            vertex = File.Data.GetRange(8, vertex_size);
            face = File.Data.GetRange(16 + vertex_size, face_size);

            AutoFormat();

            obj = viewport.FindObjectByName("model") as SceneBox.SceneBoxMesh;
            if (obj != null)
            {
                string meshName = MeshManager.Instance.GetUniqueName("_viewport_temp");

                mesh = MeshManager.Instance.CreateManual(meshName);

                SubMesh subMesh = mesh.CreateSubMesh();
                subMesh.UseSharedVertices = false;

                VertexDeclaration dec = subMesh.VertexData.VertexDeclaration;
                dec.AddElement(0, 0, VertexElementType.Float3, VertexElementSemantic.Position);
                dec.AddElement(0, 12, VertexElementType.Float3, VertexElementSemantic.Normal);
                dec.AddElement(0, 24, VertexElementType.Float2, VertexElementSemantic.TextureCoordinates, 0);

                HardwareBuffer.Usage usage = HardwareBuffer.Usage.DynamicWriteOnly;
                HardwareVertexBuffer vertexBuffer = HardwareBufferManager.Instance.CreateVertexBuffer(Marshal.SizeOf(typeof(Engine.Renderer.DynamicMeshManager.Vertex)), vertex_size, usage);
                subMesh.VertexData.VertexBufferBinding.SetBinding(0, vertexBuffer, true);
                subMesh.VertexData.VertexCount = vertex_size;

                HardwareIndexBuffer indexBuffer = HardwareBufferManager.Instance.CreateIndexBuffer(HardwareIndexBuffer.IndexType._16Bit, face_size, usage);
                subMesh.IndexData.SetIndexBuffer(indexBuffer, true);
                subMesh.IndexData.IndexCount = face_size;

                obj.MeshName = meshName;
                obj.OverrideMaterial = "Blank";
            }

            zoom = 3;
            dir = new SphereDir();
            viewport.CameraPosition = dir.GetVector() * zoom;
        }

        void Maping_Click(Button sender)
        {
            if (mapWindow == null)
                mapWindow = new MapingWinow();
            else
                mapWindow.Focus();
        }

        protected override void OnRender()
        {
            if (viewport.Camera == null)
                return;

            DebugGeometry dg = viewport.Camera.DebugGeometry;

            if (((CheckBox)window.Controls["btn\\grid"]).Checked)
            {
                int size = 5;
                dg.Color = new ColorValue(1, 1, 1);
                for (int i = -size; i <= size; i++)
                {
                    dg.AddLine(new Vec3(i, -size, 0), new Vec3(i, size, 0));
                    dg.AddLine(new Vec3(-size, i, 0), new Vec3(size, i, 0));
                }
            }

            if (((CheckBox)window.Controls["btn\\gizmo"]).Checked)
            {
                dg.Color = new ColorValue(1, 0, 0);
                dg.AddLine(Vec3.Zero, Vec3.XAxis);
                dg.Color = new ColorValue(0, 1, 0);
                dg.AddLine(Vec3.Zero, Vec3.YAxis);
                dg.Color = new ColorValue(0, 0, 1);
                dg.AddLine(Vec3.Zero, Vec3.ZAxis);
            }

            if (((CheckBox)window.Controls["btn\\vertex"]).Checked && Vertexs != null)
            {
                foreach (Vertex v in Vertexs)
                    dg.AddSphere(new Sphere(new Vec3(v.x, v.y, v.z)+obj.Position, .005f), 16);
            }

            if (viewport.Viewport != null)
                viewport.Viewport.BackgroundColor = new ColorValue(.247f, .42f, 1);
        }

        void viewport_MouseMove(Control sender)
        {
            if (ViewportIsRotation)
            {
                Vec2 pos = viewport.MousePosition;
                Vec2 delta = pos - ViewportMouseOffest;
                float multer = 2.5f;

                dir.Horizontal -= delta.X * multer;
                dir.Vertical += delta.Y * multer;
                float num = 1.560796f;
                if ((double)dir.Vertical > (double)num)
                    dir.Vertical = num;
                if ((double)dir.Vertical < -(double)num)
                    dir.Vertical = -num;

                viewport.CameraPosition = dir.GetVector() * zoom;
                ViewportMouseOffest = pos;
            }
        }

        void viewport_MouseDown(Control sender, EMouseButtons button)
        {
            if (button == EMouseButtons.Left)
            {
                ViewportMouseOffest = viewport.MousePosition;
                ViewportIsRotation = true;
            }
        }

        void viewport_MouseUp(Control sender, EMouseButtons button)
        {
            if (button == EMouseButtons.Left)
            {
                ViewportIsRotation = false;
            }
        }

        void viewport_MouseLeave(Control sender)
        {
            ViewportIsRotation = false;
        }

        void viewport_MouseWheel(Control sender, int delta)
        {
            zoom += zoom * 0.002f * delta;
            if (zoom < 0.1f)
                zoom = 0.1f;

            viewport.CameraPosition = dir.GetVector() * zoom;
        }

        void PointMouseUp(Control sender, EMouseButtons button)
        {
            if (button == EMouseButtons.Left)
                PointIsMove = false;
        }

        bool flag1 = false;
        void UpdateFaceOfLodLevel(int x)
        {
            flag1 = true;
            GraphLine2D p = window.Controls["tab\\lod\\view"] as GraphLine2D;
            Control c = window.Controls["tab\\lod\\point"];
            ((IntCounter)window.Controls["tab\\lod\\pos"]).Value = x;
            p.Zone0 = x / 100.0f;
            c.Position = new ScaleValue(ScaleType.Parent, new Vec2(x / 100.0f, c.Position.Value.Y));

            flag1 = false;
            _BuildFace();
            _CalculateNormal();
            _WriteToMemoryF();
        }

        void PointMouseMove(Control sender)
        {
            if (PointIsMove)
            {
                GraphLine2D p = window.Controls["tab\\lod\\view"] as GraphLine2D;

                float f = 1 / (p.Size.Value.X / sender.Size.Value.X);
                float x = p.MousePosition.X + PointMouseOffset.X * f - f / 2;

                if (x > 1)
                    x = 1;
                else if (x < 0.01f)
                    x = 0.01f;

                UpdateFaceOfLodLevel((int)(x * 100));
            }
        }

        void LodPosition_ValueChange(IntCounter control, int value)
        {
            if (flag1)
                return;

            UpdateFaceOfLodLevel(value);
        }

        void PointMouseDown(Control sender, EMouseButtons button)
        {
            if (button == EMouseButtons.Left)
            {
                PointMouseOffset = sender.MousePosition;
                PointIsMove = true;
            }
        }

        void DeclarationToGUI()
        {
            ((IntCounter)window.Controls["tab\\format\\size"]).Value = declaration.Size;
            ((IntCounter)window.Controls["tab\\format\\pos"]).Value = declaration.position;
            ((IntCounter)window.Controls["tab\\format\\uv"]).Value = declaration.texcoord0;
            ((IntCounter)window.Controls["tab\\format\\unknown"]).Value = declaration.unknown;
            ((IntCounter)window.Controls["tab\\format\\unknown_size"]).Value = declaration.Size - declaration.unknown;
        }

        void DeclarationInGUI()
        {
            declaration.Size = ((IntCounter)window.Controls["tab\\format\\size"]).Value;
            declaration.position = ((IntCounter)window.Controls["tab\\format\\pos"]).Value;
            declaration.texcoord0 = ((IntCounter)window.Controls["tab\\format\\uv"]).Value;
            declaration.unknown = ((IntCounter)window.Controls["tab\\format\\unknown"]).Value;
        }

        void AutoFormat()
        {
            declaration = new VertexType();
            List<int> sized = new List<int>();

            if (vertex_size % 24 == 0)
                sized.Add(24);
            if (vertex_size % 28 == 0)
                sized.Add(28);
            if (vertex_size % 32 == 0)
                sized.Add(32);
            if (vertex_size % 36 == 0)
                sized.Add(36);

            if (sized.Count != 0)
                declaration.Size = sized[0];
            
            if (sized.Count > 1)
            {
                foreach (int i in sized)
                {
                    if (IsValidVertexData(i))
                    {
                        declaration.Size = i;
                        break;
                    }
                }
            }

            DeclarationToGUI();
        }

        bool IsValidVertexData(int size)
        {
            if ((vertex[5 * size - 3] == (byte)255) &&
                (vertex[5 * size - 2] == (byte)255) &&
                (vertex[5 * size - 1] == (byte)255))
                return true;

            if ((vertex[7 * size - 3] == (byte)255) &&
                (vertex[7 * size - 2] == (byte)255) &&
                (vertex[7 * size - 1] == (byte)255))
                return true;

            return false;
        }

        void ExportClick(Button sender)
        {
            string perfis = "bin";
            List<byte> buffer = new List<byte>();

            switch (((ComboBox)window.Controls["export\\format"]).SelectedItem as string)
            {
                case "obj":
                    perfis = "bin";

                    break;
            }

            new SaveFileDialog(File.GetOnlyName(), buffer.ToArray(), perfis);
        }

        void Render()
        {
            _BuildVertex();

            ReadFace();
            _BuildFace();

            _CalculateNormal();
            _WriteToMemoryV();
            _WriteToMemoryF();
        }

        void ReadFace()
        {
            List<int> buffer = new List<int>();
            Faces = Face.Read(face);

            foreach (Face f in Faces)
                buffer.Add(f.ToInt32());

            ((GraphLine2D)window.Controls["tab\\lod\\view"]).SetData(buffer);
        }

        void _BuildFace()
        {
            if (mesh == null)
                return;

            int end = (int)(Faces.Count / 100 * ((IntCounter)window.Controls["tab\\lod\\pos"]).Value * 3);

            indicesToMemory = new ushort[face_size / 2];
            int offest = 0;

            foreach (Face f in Faces)
            {
                indicesToMemory[offest] = (ushort)(f.a - 1);
                indicesToMemory[offest + 1] = (ushort)(f.b - 1);
                indicesToMemory[offest + 2] = (ushort)(f.c - 1);
                offest += 3;

                if (offest >= end)
                    break;
            }
        }

        void _BuildVertex()
        {
            if (mesh == null)
                return;

            Bounds b = new Bounds();
            Vertexs = Vertex.Read(vertex, declaration);
            verticesToMemory = new Engine.Renderer.DynamicMeshManager.Vertex[vertex_size / declaration.Size];
            int offest = 0;

            foreach (Vertex v in Vertexs)
            {
                verticesToMemory[offest] = new DynamicMeshManager.Vertex(new Vec3(v.x, v.y, v.z), Vec3.Zero, new Vec2(v.u, v.v));

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

            mesh.SetBoundsAndRadius(b, b.GetRadius());
            MoveReset(null);
        }

        unsafe void _CalculateNormal()
        {
            fixed (Engine.Renderer.DynamicMeshManager.Vertex* VerticesToMemory = verticesToMemory)
            {
                int triangleCount = indicesToMemory.Length / 3;
                for (int n = 0; n < triangleCount; n++)
                {
                    int index0 = indicesToMemory[n * 3 + 0];
                    int index1 = indicesToMemory[n * 3 + 1];
                    int index2 = indicesToMemory[n * 3 + 2];

                    Vec3 pos0 = VerticesToMemory[index0].position;
                    Vec3 pos1 = VerticesToMemory[index1].position;
                    Vec3 pos2 = VerticesToMemory[index2].position;

                    Vec3 normal = Vec3.Cross(pos1 - pos0, pos2 - pos0);
                    normal.Normalize();

                    VerticesToMemory[index0].normal += normal;
                    VerticesToMemory[index1].normal += normal;
                    VerticesToMemory[index2].normal += normal;
                }

                for (int n = 0; n < verticesToMemory.Length; n++)
                    VerticesToMemory[n].normal = VerticesToMemory[n].normal.GetNormalize();
            }
        }

        unsafe void _WriteToMemoryV()
        {
            HardwareVertexBuffer vertexBuffer = mesh.SubMeshes[0].VertexData.VertexBufferBinding.GetBuffer(0);
            IntPtr buffer = vertexBuffer.Lock(HardwareBuffer.LockOptions.Discard);
            fixed (Engine.Renderer.DynamicMeshManager.Vertex* pVertices = verticesToMemory)
                NativeUtils.CopyMemory(buffer, (IntPtr)pVertices, verticesToMemory.Length * sizeof(Engine.Renderer.DynamicMeshManager.Vertex));
            vertexBuffer.Unlock();
        }

        unsafe void _WriteToMemoryF()
        {
            HardwareIndexBuffer indexBuffer = mesh.SubMeshes[0].IndexData.IndexBuffer;
            IntPtr buffer = indexBuffer.Lock(HardwareBuffer.LockOptions.Discard);
            fixed (ushort* pIndices = indicesToMemory)
                NativeUtils.CopyMemory(buffer, (IntPtr)pIndices, indicesToMemory.Length * sizeof(ushort));
            indexBuffer.Unlock();
        }

        void MoveTop(object flag)
        {
            obj.Position += Vec3.ZAxis * .1f;
        }

        void MoveReset(object flag)
        {
            obj.Position = new Vec3(obj.Position.X, obj.Position.Y, -1 * mesh.Bounds.GetSize().Z / 2);
        }

        void MoveDown(object flag)
        {
            obj.Position -= Vec3.ZAxis * .1f;
        }

        void OnUpdate(object flag)
        {
            DeclarationInGUI();
            Render();
        }

        protected override void OnDetach()
        {
            base.OnDetach();

            if (File != null)
                File.ClearCache();

            if (this == instance)
                instance = null;
        }
    }
}