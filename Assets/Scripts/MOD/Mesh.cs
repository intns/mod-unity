using System;
using System.Collections.Generic;
using System.IO;

namespace MODFile
{
    [Serializable]
    public class DisplayList : IReadable
    {
        public int Flags;
        public int CommandCount;
        public byte[] DisplayData;

        public void Read(BinaryReader reader)
        {
            Flags = reader.ReadInt32BE();
            CommandCount = reader.ReadInt32BE();
            int size = reader.ReadInt32BE();

            reader.AlignToMultiple(0x20);

            DisplayData = reader.ReadBytes(size);
        }
    }

    [Serializable]
    public class MeshPacket : IReadable
    {
        public List<ushort> Indices = new();
        public DisplayList[] DisplayLists;

        public void Read(BinaryReader reader)
        {
            int count = reader.ReadInt32BE();
            if (count != 0)
            {
                Indices = new List<ushort>();
                for (int i = 0; i < count; i++)
                {
                    Indices.Add((ushort)reader.ReadInt16BE());
                }
            }

            int displayListCount = reader.ReadInt32BE();
            if (displayListCount != 0)
            {
                DisplayLists = new DisplayList[displayListCount];
                for (int i = 0; i < displayListCount; i++)
                {
                    DisplayList displayList = new();
                    displayList.Read(reader);
                    DisplayLists[i] = displayList;
                }
            }
        }
    }

    [Serializable]
    public class Mesh : IReadable
    {
        public int BoneIndex;
        public int VertexDescriptor;
        public MeshPacket[] Packets;

        public void Read(BinaryReader reader)
        {
            BoneIndex = reader.ReadInt32BE();
            VertexDescriptor = reader.ReadInt32BE();
            int packetSize = reader.ReadInt32BE();

            Packets = new MeshPacket[packetSize];
            for (int i = 0; i < packetSize; i++)
            {
                Packets[i] = new MeshPacket();
                Packets[i].Read(reader);
            }
        }
    }
}
