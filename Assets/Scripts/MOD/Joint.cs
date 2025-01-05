using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace MODFile
{
    [Serializable]
    public class Joint : IReadable
    {
        [Serializable]
        public class MatPoly : IReadable
        {
            public ushort MaterialIndex;
            public ushort MeshIndex;

            public void Read(BinaryReader reader)
            {
                MaterialIndex = (ushort)reader.ReadInt16BE();
                MeshIndex = (ushort)reader.ReadInt16BE();
            }
        }

        public int ParentIndex;
        public int Flags;
        Bounds BoundingBox = new();
        public float VolumeRadius;
        public Vector3Readable Scale;
        public Vector3Readable Rotation;
        public Vector3Readable Position;
        public List<MatPoly> MatPolys = new();

        public void Read(BinaryReader reader)
        {
            ParentIndex = reader.ReadInt32BE();
            Flags = reader.ReadInt32BE();

            BoundingBox = new Bounds();
            Vector3 max = reader.ReadVector3();
            Vector3 min = reader.ReadVector3();
            BoundingBox.SetMinMax(min, max);

            VolumeRadius = reader.ReadSingleBE();

            Scale = reader.ReadVector3();
            Rotation = reader.ReadVector3();
            Position = reader.ReadVector3();

            MatPolys = new List<MatPoly>(reader.ReadInt32BE());
            for (int i = 0; i < MatPolys.Capacity; i++)
            {
                MatPoly newPoly = new();
                newPoly.Read(reader);
                MatPolys.Add(newPoly);
            }
        }
    }
}
