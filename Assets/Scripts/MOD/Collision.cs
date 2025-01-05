using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace MODFile
{
    [Serializable]
    public class BaseRoomInfo : IReadable
    {
        public int Index;

        public void Read(BinaryReader reader)
        {
            Index = reader.ReadInt32BE();
        }
    }

    [Serializable]
    public class BaseCollTriInfo : IReadable
    {
        public int MapCode;
        public int VertexIndex1;
        public int VertexIndex2;
        public int VertexIndex3;

        public ushort _10;
        public ushort _12;
        public ushort _14;
        public ushort _16;

        public Plane CollPlane;

        public void Read(BinaryReader reader)
        {
            MapCode = reader.ReadInt32BE();
            VertexIndex1 = reader.ReadInt32BE();
            VertexIndex2 = reader.ReadInt32BE();
            VertexIndex3 = reader.ReadInt32BE();

            _10 = (ushort)reader.ReadInt16BE();
            _12 = (ushort)reader.ReadInt16BE();
            _14 = (ushort)reader.ReadInt16BE();
            _16 = (ushort)reader.ReadInt16BE();

            CollPlane = new Plane();
            CollPlane.Read(reader);
        }
    }

    [Serializable]
    public class CollTriInfo : IReadable
    {
        public List<BaseCollTriInfo> TriInfos = new();
        public List<BaseRoomInfo> RoomInfos = new();

        public void Read(BinaryReader reader)
        {
            TriInfos = new List<BaseCollTriInfo>(reader.ReadInt32BE());
            RoomInfos = new List<BaseRoomInfo>(reader.ReadInt32BE());

            reader.AlignToMultiple(0x20);

            for (int i = 0; i < RoomInfos.Capacity; i++)
            {
                BaseRoomInfo roomInfo = new();
                roomInfo.Read(reader);
                RoomInfos.Add(roomInfo);
            }

            reader.AlignToMultiple(0x20);

            for (int i = 0; i < TriInfos.Capacity; i++)
            {
                BaseCollTriInfo triInfo = new();
                triInfo.Read(reader);
                TriInfos.Add(triInfo);
            }

            reader.AlignToMultiple(0x20);
        }
    }

    [Serializable]
    public class CollGroup : IReadable
    {
        public List<int> _00 = new();
        public List<int> _04 = new();

        public void Read(BinaryReader reader)
        {
            _00 = new List<int>(reader.ReadInt16BE());
            _04 = new List<int>(reader.ReadInt16BE());

            for (int i = 0; i < _04.Capacity; i++)
            {
                _04.Add(reader.ReadInt32BE());
            }

            for (int i = 0; i < _00.Capacity; i++)
            {
                _00.Add(reader.ReadByte());
            }
        }
    }

    [Serializable]
    public class CollGrid : IReadable
    {
        public Bounds BoundingBox = new();
        float Unknown1;
        float GridX;
        float GridY;
        List<CollGroup> Groups = new();

        public List<int> Unknown2 = new();

        public void Read(BinaryReader reader)
        {
            reader.AlignToMultiple(0x20);

            BoundingBox = new Bounds();
            BoundingBox.SetMinMax(reader.ReadVector3(), reader.ReadVector3());

            Unknown1 = reader.ReadSingleBE();
            GridX = reader.ReadInt32BE();
            GridY = reader.ReadInt32BE();

            Groups = new List<CollGroup>(reader.ReadInt32BE());
            for (int i = 0; i < Groups.Capacity; i++)
            {
                CollGroup group = new();
                group.Read(reader);
                Groups.Add(group);
            }

            for (int x = 0; x < GridX; x++)
            {
                for (int y = 0; y < GridY; y++)
                {
                    Unknown2.Add(reader.ReadInt32BE());
                }
            }

            reader.AlignToMultiple(0x20);
        }
    }
}
