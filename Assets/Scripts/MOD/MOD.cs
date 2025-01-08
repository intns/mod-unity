using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;

namespace MODFile
{
    public enum MODFlags : byte
    {
        None = 0x00,
        UseNBT = 0x01,
        UseClassicScale = 0x08, // If not set, use SoftImage scale
    }

    [Serializable]
    public class MODHeader
    {
        [Serializable]
        public class DateTime
        {
            public ushort Year;
            public byte Month;
            public byte Day;
        }

        public DateTime _DateTime = new();
        public int Flags;

        public void Read(BinaryReader reader)
        {
            reader.AlignToMultiple(0x20);

            _DateTime.Year = (ushort)reader.ReadInt16BE();
            _DateTime.Month = (byte)reader.ReadChar();
            _DateTime.Day = (byte)reader.ReadChar();
            Flags = reader.ReadInt32BE();

            reader.AlignToMultiple(0x20);
        }
    }

    [Serializable]
    public enum EChunkType
    {
        Header = 0x00,

        Vertex = 0x10,
        VertexNormal = 0x11,
        VertexNBT = 0x12,
        VertexColour = 0x13,

        TexCoord0 = 0x18,
        TexCoord1 = 0x19,
        TexCoord2 = 0x1A,
        TexCoord3 = 0x1B,
        TexCoord4 = 0x1C,
        TexCoord5 = 0x1D,
        TexCoord6 = 0x1E,
        TexCoord7 = 0x1F,

        Texture = 0x20,
        TextureAttribute = 0x22,

        Material = 0x30,

        VertexMatrix = 0x40,
        MatrixEnvelope = 0x41,

        Mesh = 0x50,
        Joint = 0x60,
        JointName = 0x61,

        CollisionPrism = 0x100,
        CollisionGrid = 0x110,

        EndOfFile = 0xFFFF
    }

    [Serializable]
    public class MOD
    {
        public MODHeader Header = new();
        public List<Vector3Readable> Vertices = new();
        public List<Vector3Readable> VertexNormals = new();
        public List<NBT> VertexNBT = new();
        public List<ColourU8> VertexColours = new();
        public TextureCoordinate TextureCoordinates = new();
        public List<Texture> Textures = new();
        public List<TextureAttributes> TextureAttributes = new();
        public MaterialPacket Materials = new();
        public List<VtxMatrix> VertexMatrices = new();
        public List<Envelope> Envelopes = new();
        public List<Mesh> Meshes = new();
        public List<Joint> Joints = new();
        public List<string> JointNames = new();
        public CollTriInfo CollisionTriangles = new();
        public CollGrid CollisionGrid = new();
        public string EndOfFile;

        private static string GetChunkName(int opcode)
        {
            return Enum.GetName(typeof(EChunkType), opcode) ?? "Unknown Chunk";
        }

        private void ReadGenericChunk<T>(BinaryReader reader, List<T> list)
            where T : IReadable, new()
        {
            int count = reader.ReadInt32BE();

            reader.AlignToMultiple(0x20);

            for (int i = 0; i < count; i++)
            {
                T element = new();
                element.Read(reader);
                list.Add(element);
            }

            reader.AlignToMultiple(0x20);
        }

        public void DrawGizmos()
        {
            foreach (BaseCollTriInfo colltri in CollisionTriangles.TriInfos)
            {
                Gizmos.color = Color.red;
                Vector3 v1 = Vertices[colltri.VertexIndex1];
                Vector3 v2 = Vertices[colltri.VertexIndex2];
                Vector3 v3 = Vertices[colltri.VertexIndex3];

                Gizmos.DrawLine(v1, v2);
                Gizmos.DrawLine(v2, v3);
                Gizmos.DrawLine(v3, v1);
            }
        }

        public UnityEngine.Mesh GetUnityCollisionTriangles()
        {
            UnityEngine.Mesh mesh = new();

            List<Vector3> meshVertices = new();
            List<Vector2> uvs = new();
            List<int> meshTriangles = new();

            for (int i = 0; i < CollisionTriangles.TriInfos.Count; i++)
            {
                BaseCollTriInfo colltri = CollisionTriangles.TriInfos[i];
                Vector3 v1 = Vertices[colltri.VertexIndex1];
                Vector3 v2 = Vertices[colltri.VertexIndex2];
                Vector3 v3 = Vertices[colltri.VertexIndex3];

                Vector2 uv1 = new(Vector3.Dot(v1, Vector3.right), Vector3.Dot(v1, Vector3.up));
                Vector2 uv2 = new(Vector3.Dot(v2, Vector3.right), Vector3.Dot(v2, Vector3.up));
                Vector2 uv3 = new(Vector3.Dot(v3, Vector3.right), Vector3.Dot(v3, Vector3.up));

                uvs.Add(uv1);
                uvs.Add(uv2);
                uvs.Add(uv3);

                meshVertices.Add(v1);
                meshVertices.Add(v2);
                meshVertices.Add(v3);

                meshTriangles.Add(i * 3);
                meshTriangles.Add(i * 3 + 1);
                meshTriangles.Add(i * 3 + 2);
            }

            mesh.vertices = meshVertices.ToArray();
            mesh.triangles = meshTriangles.ToArray();
            mesh.SetUVs(0, uvs.ToArray());

            mesh.RecalculateNormals();

            return mesh;
        }

        public void Read(BinaryReader reader)
        {
            bool stopRead = false;
            int iterations = 0;
            while (!stopRead && iterations++ < 1000)
            {
                int opcode = reader.ReadInt32BE();
                int length = reader.ReadInt32BE();

                switch ((EChunkType)opcode)
                {
                    case EChunkType.Header:
                        Header.Read(reader);
                        break;
                    case EChunkType.Vertex:
                        ReadGenericChunk(reader, Vertices);

                        // Invert the Z axis
                        foreach (Vector3Readable v in Vertices)
                        {
                            v.Vector.z = -v.Vector.z;
                        }

                        break;
                    case EChunkType.VertexNormal:
                        ReadGenericChunk(reader, VertexNormals);
                        break;
                    case EChunkType.VertexNBT:
                        ReadGenericChunk(reader, VertexNBT);
                        break;
                    case EChunkType.VertexColour:
                        ReadGenericChunk(reader, VertexColours);
                        break;
                    case EChunkType.TexCoord0:
                    case EChunkType.TexCoord1:
                    case EChunkType.TexCoord2:
                    case EChunkType.TexCoord3:
                    case EChunkType.TexCoord4:
                    case EChunkType.TexCoord5:
                    case EChunkType.TexCoord6:
                    case EChunkType.TexCoord7:
                        TextureCoordinates.Read(reader, opcode - 0x18);
                        break;
                    case EChunkType.Texture:
                        ReadGenericChunk(reader, Textures);
                        break;
                    case EChunkType.TextureAttribute:
                        ReadGenericChunk(reader, TextureAttributes);
                        break;
                    case EChunkType.Material:
                        Materials.Read(reader);
                        break;
                    case EChunkType.VertexMatrix:
                        ReadGenericChunk(reader, VertexMatrices);
                        break;
                    case EChunkType.MatrixEnvelope:
                        ReadGenericChunk(reader, Envelopes);
                        break;
                    case EChunkType.Mesh:
                        ReadGenericChunk(reader, Meshes);
                        break;
                    case EChunkType.Joint:
                        ReadGenericChunk(reader, Joints);
                        break;
                    case EChunkType.JointName:
                        int jointNameCount = reader.ReadInt32BE();
                        reader.AlignToMultiple(0x20);

                        for (int i = 0; i < jointNameCount; i++)
                        {
                            JointNames.Add(new string(reader.ReadChars(reader.ReadInt32BE())));
                        }

                        reader.AlignToMultiple(0x20);
                        break;
                    case EChunkType.CollisionPrism:
                        CollisionTriangles.Read(reader);
                        break;
                    case EChunkType.CollisionGrid:
                        CollisionGrid.Read(reader);
                        break;
                    case EChunkType.EndOfFile:
                        reader.BaseStream.Seek(length, SeekOrigin.Current);

                        if (reader.BaseStream.Position != reader.BaseStream.Length)
                        {
                            byte[] remainingBytes = reader.ReadBytes(
                                (int)(reader.BaseStream.Length - reader.BaseStream.Position)
                            );

                            EndOfFile = Encoding.Default.GetString(remainingBytes);
                        }

                        stopRead = true;
                        break;
                    default:
                        Debug.Log($"Unknown chunk type {opcode} : {GetChunkName(opcode)}");
                        reader.BaseStream.Seek(length, SeekOrigin.Current);
                        break;
                }
            }
        }
    }
}
