using System;
using System.Collections.Generic;

namespace Game
{
    public class Vertex
    {
        public int Type = 24;
        public float x, y, z;
        public float u, v;

        public Vertex(List<byte> Data, VertexType type)
        {
            var data = Data.ToArray();

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
            var Vertexs = new List<Vertex>();

            for (var i = 0; i < data.Count / type.Size; i++)
                Vertexs.Add(new Vertex(data.GetRange(i * type.Size, type.Size), type));

            return Vertexs;
        }

        public override string ToString()
        {
            return
                $"{x.ToString("0.00")} \t{y.ToString("0.00")} \t{z.ToString("0.00")} \t[{u.ToString("0.0000")} \t{v.ToString("0.0000")}]";
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