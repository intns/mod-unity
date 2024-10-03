using System;
using System.IO;

namespace MODFile
{
    [Serializable]
    public class NBT : IReadable
    {
        public Vector3Readable Normal;
        public Vector3Readable Binormal;
        public Vector3Readable Tangent;

        public void Read(BinaryReader reader)
        {
            Normal = reader.ReadVector3();
            Binormal = reader.ReadVector3();
            Tangent = reader.ReadVector3();
        }
    }
}
