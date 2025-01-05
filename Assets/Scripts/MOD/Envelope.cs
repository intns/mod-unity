using System;
using System.Collections.Generic;
using System.IO;

namespace MODFile
{
    [Serializable]
    public class Envelope : IReadable
    {
        public List<ushort> Indices = new();
        public List<float> Weights = new();

        public void Read(BinaryReader reader)
        {
            int length = reader.ReadInt16BE();
            Indices = new List<ushort>(length);
            Weights = new List<float>(length);

            for (int i = 0; i < length; i++)
            {
                Indices.Add((ushort)reader.ReadInt16BE());
                Weights.Add(reader.ReadSingleBE());
            }
        }

        public override string ToString()
        {
            string str = "";
            for (int i = 0; i < Indices.Count; i++)
            {
                str += $"[{Indices[i]}] {Weights[i]}\n";
            }
            return str;
        }
    }
}
