﻿using System;
using System.Collections.Generic;

namespace Game.Structures
{
    class Face
    {
        public Int16 a, b, c;

        public static List<Face> Read(List<byte> Data)
        {
            var Faces = new List<Face>();
            var data = Data.ToArray();

            for (var i = 0; i < Data.Count / 6; i++)
            {
                var f = new Face();
                f.a = BitConverter.ToInt16(data, i * 6);
                f.b = BitConverter.ToInt16(data, i * 6 + 2);
                f.c = BitConverter.ToInt16(data, i * 6 + 4);

                Faces.Add(f++);
            }

            return Faces;
        }

        public static Face operator +(Face one, int two)
        {
            one.a += (Int16)two;
            one.b += (Int16)two;
            one.c += (Int16)two;

            return one;
        }

        public static Face operator ++(Face one)
        {
            one.a++;
            one.b++;
            one.c++;

            return one;
        }

        public int ToInt32()
        {
            return (a + b + c) / 3;
        }

        public string ToString(bool vt = false)
        {
            if (vt)
                return string.Format("{0}/{0} {1}/{1} {2}/{2}", a.ToString(), b.ToString(), c.ToString());
            return $"{a.ToString()} {b.ToString()} {c.ToString()}";
        }
    }
}
