using System;
using System.IO;

namespace MODFile
{
    [Serializable]
    public class Plane : IReadable
    {
        public Vector3Readable Normal;
        public float Distance;

        public void Read(BinaryReader reader)
        {
            Normal = reader.ReadVector3();
            Distance = reader.ReadSingleBE();
        }
    }
}
