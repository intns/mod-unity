using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using mod.schema;
using MODFile;
using UnityEngine;

public class DisplayListReader
{
    private readonly MOD _RawFile;
    private readonly short[] _ActiveMatrices;
    private readonly IReadOnlyList<Transform> _Bones;
    private readonly List<BoneWeight1[]> _BoneWeights;

    public class VertexData
    {
        public List<Vector3> Positions { get; } = new();
        public List<Vector3> Normals { get; } = new();
        public List<Vector4> Tangents { get; } = new();
        public List<Color> Colors { get; } = new();
        public List<Vector2>[] UVs { get; } = new List<Vector2>[8];
        public List<BoneWeight1> Weights { get; } = new();
        public List<int> BoneCounts { get; } = new();
        public List<int> Triangles { get; } = new();
    }

    public DisplayListReader(
        MOD rawFile,
        IReadOnlyList<Transform> bones,
        List<BoneWeight1[]> boneWeights
    )
    {
        _RawFile = rawFile;
        _Bones = bones;
        _BoneWeights = boneWeights;
        _ActiveMatrices = new short[10]; // GX supports 10 active matrices
    }

    public VertexData ReadMesh(MODFile.Mesh mesh)
    {
        var vertexData = new VertexData();
        var flags = new VertexDescriptor();
        flags.FromPikmin1((uint)mesh.VertexDescriptor, _RawFile.VertexNormals.Count != 0);
        var descriptor = flags.ToArray();

        foreach (var meshPacket in mesh.Packets)
        {
            foreach (var displayList in meshPacket.DisplayLists)
            {
                ProcessDisplayList(displayList, meshPacket, descriptor, flags, vertexData);
            }
        }

        return vertexData;
    }

    private void ProcessDisplayList(
        DisplayList displayList,
        MeshPacket meshPacket,
        (GxAttribute, GxAttributeType?)[] descriptor,
        VertexDescriptor flags,
        VertexData vertexData
    )
    {
        using var br = new BinaryReader(new MemoryStream(displayList.DisplayData));

        while (br.BaseStream.Position < br.BaseStream.Length)
        {
            var opcode = (GxOpcode)br.ReadByte();

            if (opcode == GxOpcode.Nop)
                continue;

            if (opcode != GxOpcode.TriangleStrip && opcode != GxOpcode.TriangleFan)
                continue;

            var primitiveData = ReadPrimitiveData(br, descriptor, meshPacket, flags);
            ProcessPrimitiveData(primitiveData, opcode, vertexData);
        }
    }

    private class PrimitiveData
    {
        public List<MODVertex> Vertices { get; } = new();
        public List<BoneWeight1[]> VertexWeights { get; } = new();
    }

    private PrimitiveData ReadPrimitiveData(
        BinaryReader br,
        (GxAttribute, GxAttributeType?)[] descriptor,
        MeshPacket meshPacket,
        VertexDescriptor flags
    )
    {
        var data = new PrimitiveData();
        var positionIndices = new List<ushort>();
        var normalIndices = new List<ushort>();
        var colorIndices = new List<ushort>();
        var texCoordIndices = new List<ushort>[8];
        var boneIndices = new List<int?>();

        for (int t = 0; t < 8; ++t)
        {
            texCoordIndices[t] = new List<ushort>();
        }

        short faceCount = br.ReadInt16BE();
        for (int f = 0; f < faceCount; f++)
        {
            foreach (var (attr, format) in descriptor)
            {
                if (format == null)
                {
                    byte info = br.ReadByte();
                    if (attr == GxAttribute.PNMTXIDX)
                    {
                        ProcessMatrixIndex(info, meshPacket, boneIndices, data.VertexWeights);
                    }
                    continue;
                }

                switch (attr)
                {
                    case GxAttribute.POS:
                        positionIndices.Add(DisplayListReadAttribute(br, format.Value));
                        break;
                    case GxAttribute.NRM:
                        normalIndices.Add(DisplayListReadAttribute(br, format.Value));
                        break;
                    case GxAttribute.CLR0:
                        colorIndices.Add(DisplayListReadAttribute(br, format.Value));
                        break;
                    case >= GxAttribute.TEX0
                    and <= GxAttribute.TEX7:
                        texCoordIndices[attr - GxAttribute.TEX0]
                            .Add(DisplayListReadAttribute(br, format.Value));
                        break;
                    default:
                        if (format == GxAttributeType.Index16)
                            br.ReadInt16();
                        else
                            throw new Exception($"Unknown attribute {attr} format: {format}");
                        break;
                }
            }
        }

        // Create vertices from the gathered data
        for (int i = 0; i < positionIndices.Count; i++)
        {
            var vertex = CreateVertex(
                positionIndices[i],
                normalIndices.Count > 0 ? normalIndices[i] : (ushort)0,
                colorIndices.Count > 0 ? colorIndices[i] : (ushort)0,
                texCoordIndices,
                boneIndices,
                i,
                flags,
                data.VertexWeights.Count > 0 ? data.VertexWeights[i] : null
            );

            data.Vertices.Add(vertex);
        }

        return data;
    }

    private MODVertex CreateVertex(
        ushort posIndex,
        ushort normalIndex,
        ushort colorIndex,
        List<ushort>[] texCoordIndices,
        List<int?> boneIndices,
        int vertexIndex,
        VertexDescriptor flags,
        BoneWeight1[] weights
    )
    {
        var vertex = new MODVertex { Position = (Vector3)_RawFile.Vertices[posIndex] };

        if (_RawFile.VertexNormals.Count > 0)
        {
            ProcessNormal(vertex, normalIndex, flags);
        }

        if (weights != null)
        {
            vertex.Weights = weights.ToList();

            if (boneIndices[vertexIndex] != null)
            {
                vertex.Position = _Bones[boneIndices[vertexIndex].Value]
                    .localToWorldMatrix.MultiplyPoint(vertex.Position);
            }
        }

        vertex.Color =
            colorIndex < _RawFile.VertexColours.Count
                ? new Color(
                    _RawFile.VertexColours[colorIndex].R,
                    _RawFile.VertexColours[colorIndex].G,
                    _RawFile.VertexColours[colorIndex].B,
                    _RawFile.VertexColours[colorIndex].A
                )
                : Color.white;

        ProcessUVs(vertex, texCoordIndices, vertexIndex);

        return vertex;
    }

    private void ProcessMatrixIndex(
        byte info,
        MeshPacket meshPacket,
        List<int?> boneIndices,
        List<BoneWeight1[]> vertexWeights
    )
    {
        int activeMatrixIndex = info / 3;
        short vertexMatrixIndex = (short)meshPacket.Indices[activeMatrixIndex];

        if (vertexMatrixIndex == -1)
        {
            vertexMatrixIndex = _ActiveMatrices[activeMatrixIndex];
        }

        _ActiveMatrices[activeMatrixIndex] = vertexMatrixIndex;
        int attachmentIndex = _RawFile.VertexMatrices[vertexMatrixIndex].Index;

        if (attachmentIndex >= 0)
        {
            vertexWeights.Add(
                new[]
                {
                    new BoneWeight1 { boneIndex = attachmentIndex, weight = 1.0f },
                }
            );

            boneIndices.Add(attachmentIndex);
        }
        else
        {
            int envelopeIndex = -1 - attachmentIndex;
            var envelopeWeights = _BoneWeights[envelopeIndex];

            vertexWeights.Add(envelopeWeights);
            boneIndices.Add(null);
        }
    }

    private void ProcessNormal(MODVertex vertex, ushort normalIndex, VertexDescriptor flags)
    {
        if (flags._NBTEnabled && normalIndex < _RawFile.VertexNBT.Count)
        {
            var nbt = _RawFile.VertexNBT[normalIndex];
            vertex.LocalNormal = new Vector3(
                nbt.Normal.Vector.x,
                nbt.Normal.Vector.y,
                nbt.Normal.Vector.z
            );
            vertex.LocalTangent = new Vector3(
                nbt.Tangent.Vector.x,
                nbt.Tangent.Vector.y,
                nbt.Tangent.Vector.z
            );
        }
        else if (normalIndex < _RawFile.VertexNormals.Count)
        {
            var normal = _RawFile.VertexNormals[normalIndex].Vector;
            vertex.LocalNormal = new Vector3(normal.x, normal.y, normal.z);
        }
        else
        {
            Debug.LogError($"Vertex normal index {normalIndex} is out of range");
            vertex.LocalNormal = Vector3.zero;
        }
    }

    private void ProcessUVs(MODVertex vertex, List<ushort>[] texCoordIndices, int vertexIndex)
    {
        for (int channel = 0; channel < 8; channel++)
        {
            if (texCoordIndices[channel].Count > 0)
            {
                ushort txIndex = texCoordIndices[channel][vertexIndex];
                Vector2 coord = _RawFile.TextureCoordinates.GetTexCoord(channel, txIndex);

                if (vertex.Uvs[channel] == null)
                    vertex.Uvs[channel] = new List<Vector2>();

                vertex.Uvs[channel].Add(coord);
            }
        }
    }

    private void ProcessPrimitiveData(PrimitiveData data, GxOpcode opcode, VertexData vertexData)
    {
        int startIndex = vertexData.Positions.Count;

        foreach (var vertex in data.Vertices)
        {
            vertexData.Positions.Add(vertex.Position);
            vertexData.Normals.Add(vertex.LocalNormal);

            if (vertex.LocalTangent != Vector3.zero)
            {
                vertexData.Tangents.Add(vertex.Tangent);
            }

            vertexData.Colors.Add(vertex.Color);

            if (vertex.Weights != null)
            {
                vertexData.Weights.AddRange(vertex.Weights);
                vertexData.BoneCounts.Add(vertex.Weights.Count);
            }

            for (int t = 0; t < 8; t++)
            {
                if (vertex.Uvs[t] != null)
                {
                    if (vertexData.UVs[t] == null)
                        vertexData.UVs[t] = new List<Vector2>();

                    vertexData.UVs[t].AddRange(vertex.Uvs[t]);
                }
            }
        }

        AddTriangles(vertexData.Triangles, startIndex, data.Vertices.Count, opcode);
    }

    private void AddTriangles(List<int> triangles, int startIndex, int vertexCount, GxOpcode opcode)
    {
        if (opcode == GxOpcode.TriangleFan)
        {
            for (int i = 0; i < vertexCount - 2; i++)
            {
                triangles.Add(startIndex);
                triangles.Add(startIndex + i + 1);
                triangles.Add(startIndex + i + 2);
            }
        }
        else if (opcode == GxOpcode.TriangleStrip)
        {
            for (int i = 0; i < vertexCount - 2; i++)
            {
                if (i % 2 == 0)
                {
                    triangles.Add(startIndex + i);
                    triangles.Add(startIndex + i + 1);
                    triangles.Add(startIndex + i + 2);
                }
                else
                {
                    triangles.Add(startIndex + i + 1);
                    triangles.Add(startIndex + i);
                    triangles.Add(startIndex + i + 2);
                }
            }
        }
    }

    private static ushort DisplayListReadAttribute(BinaryReader br, GxAttributeType format)
    {
        return format switch
        {
            GxAttributeType.Index16 => (ushort)br.ReadInt16BE(),
            GxAttributeType.Index8 => br.ReadByte(),
            _ => throw new Exception($"Unknown format: {format}"),
        };
    }
}
