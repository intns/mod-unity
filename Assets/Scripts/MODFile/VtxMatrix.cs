using System;
using System.IO;

namespace MODFile
{
    [Serializable]
    public class VtxMatrix : IReadable
    {
        public int Index;

        public void Read(BinaryReader reader)
        {
            Index = reader.ReadInt16BE();
        }
    }
}
