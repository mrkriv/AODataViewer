using System;
using System.Collections.Generic;
using Engine;
using Engine.UISystem;
using Engine.MathEx;

namespace Game
{
    public class Vertex
    {
        public int Type = 24;
        public float x, y, z;
        public float u, v;

        public Vertex(List<byte> Data, VertexType type)
        {
            byte[] data = Data.ToArray();

            if (type.position != 255)
            {
                x = BitConverter.ToSingle(data, type.position);
                y = BitConverter.ToSingle(data, type.position + 4);
                z = BitConverter.ToSingle(data, type.position + 8);

                //Rotation (X:90 Y:180 Z:0)
                /*float f = y;
                y = -z;
                z = f;*/
            }
            if (type.texcoord0 != 255)
            {
                u = BitConverter.ToSingle(data, type.texcoord0);
                v = BitConverter.ToSingle(data, type.texcoord0 + 4);
            }
        }

        public static List<Vertex> Read(List<byte> data, VertexType type)
        {
            List<Vertex> Vertexs = new List<Vertex>();

            for (int i = 0; i < data.Count / type.Size; i++)
                Vertexs.Add(new Vertex(data.GetRange(i * type.Size, type.Size), type));

            return Vertexs;
        }

        public override string ToString()
        {
            return string.Format("{0} \t{1} \t{2} \t[{3} \t{4}]", x.ToString("0.00"), y.ToString("0.00"), z.ToString("0.00"), u.ToString("0.0000"), v.ToString("0.0000"));
        }
    }

    public class VertexType
    {
        public int Size = 24;
        public int position = 0;
        public int texcoord0 = 12;
        public int unknown = 16;
    }
}