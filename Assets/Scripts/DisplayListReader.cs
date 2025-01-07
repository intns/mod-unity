using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using mod.schema;
using MODFile;
using UnityEngine;

public class DisplayListReader
{
    // TODO: There is a lot of overlap between these classes, consider merging them
    public class MODVertex
    {
        public Vector3 Position;
        public Vector3 LocalNormal;
        public Vector3? LocalTangent;
        public Color Color;
        public List<Vector2>[] UVs = new List<Vector2>[8];
        public List<BoneWeight1> Weights;
    }

    private class PrimitiveData
    {
        public List<MODVertex> Vertices { get; } = new();
        public List<BoneWeight1[]> VertexWeights { get; } = new();
    }

    public class VertexData
    {
        public List<Vector3> Positions { get; } = new();
        public List<Vector3> Normals { get; } = new();
        public List<Vector4> Tangents { get; } = new();
        public List<Color> Colors { get; } = new();
        public List<Vector2>[] UVs { get; } = new List<Vector2>[8];
        public List<BoneWeight1[]> WeightsByVertex { get; set; } = new();
        public List<int> BoneCounts { get; } = new();
        public List<int> Triangles { get; } = new();
    }

    private readonly MOD _RawFile;
    private readonly short[] _ActiveMatrices;
    private readonly IReadOnlyList<Transform> _Bones;
    private readonly List<BoneWeight1[]> _EnvelopeBoneWeights;

    // Bone weights indexed by bone
    Dictionary<int, List<BoneWeight1>> _BoneWeightsByBone = new();

    public DisplayListReader(
        MOD rawFile,
        IReadOnlyList<Transform> bones,
        List<BoneWeight1[]> boneWeights
    )
    {
        _RawFile = rawFile;
        _Bones = bones;
        _EnvelopeBoneWeights = boneWeights;

        // GX supports 10 active matrices
        _ActiveMatrices = new short[10];
        for (int i = 0; i < _ActiveMatrices.Length; i++)
        {
            _ActiveMatrices[i] = 0;
        }
    }

    private MODVertex CreateVertex(
        int vertexIndex,
        ushort posIndex,
        ushort normalIndex,
        ushort colorIndex,
        List<ushort>[] texCoordIndices,
        List<int?> boneIndices,
        BoneWeight1[] weights,
        VertexDescriptor flags
    )
    {
        var vertex = new MODVertex { Position = (Vector3)_RawFile.Vertices[posIndex] };

        if (_RawFile.VertexNormals.Count > 0)
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

        // TODO: here?
        if (weights != null)
        {
            vertex.Weights = weights.ToList();

            int? boneIndex = boneIndices[vertexIndex];
            if (boneIndex.HasValue)
            {
                var boneMatrix = _Bones[boneIndex.Value].localToWorldMatrix;

                // Single bone influence
                vertex.Position = boneMatrix.MultiplyPoint(vertex.Position);
                vertex.LocalNormal = boneMatrix.MultiplyVector(vertex.LocalNormal);
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

        for (int channel = 0; channel < 8; channel++)
        {
            if (texCoordIndices[channel].Count > 0)
            {
                ushort txIndex = texCoordIndices[channel][vertexIndex];
                Vector2 coord = _RawFile.TextureCoordinates.GetTexCoord(channel, txIndex);

                if (vertex.UVs[channel] == null)
                    vertex.UVs[channel] = new List<Vector2>();

                vertex.UVs[channel].Add(coord);
            }
        }

        return vertex;
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

            // Only process Triangle Strip and Triangle Fan
            if (opcode == GxOpcode.Nop)
                continue;

            if (opcode != GxOpcode.TriangleStrip && opcode != GxOpcode.TriangleFan)
                continue;

            PrimitiveData primitiveData = ReadPrimitiveData(br, descriptor, meshPacket, flags);

            // Add the primitive data to the vertex data
            int startIndex = vertexData.Positions.Count;
            foreach (var vertex in primitiveData.Vertices)
            {
                vertexData.Positions.Add(vertex.Position);
                vertexData.Normals.Add(vertex.LocalNormal);

                if (vertex.LocalTangent != null)
                {
                    var localTangent = vertex.LocalTangent.Value;
                    vertexData.Tangents.Add(new Vector4(localTangent.x, localTangent.y, localTangent.z, 1));
                }

                vertexData.Colors.Add(vertex.Color);

                if (vertex.Weights != null)
                {
                    // TODO: here?
                    vertexData.WeightsByVertex.Add(vertex.Weights.ToArray());
                    vertexData.BoneCounts.Add(vertex.Weights.Count);
                }

                for (int t = 0; t < 8; t++)
                {
                    if (vertex.UVs[t] != null)
                    {
                        if (vertexData.UVs[t] == null)
                            vertexData.UVs[t] = new List<Vector2>();

                        vertexData.UVs[t].AddRange(vertex.UVs[t]);
                    }
                }
            }

            int vertexCount = primitiveData.Vertices.Count;
            if (opcode == GxOpcode.TriangleFan)
            {
                for (int i = 0; i < vertexCount - 2; i++)
                {
                    vertexData.Triangles.Add(startIndex);
                    vertexData.Triangles.Add(startIndex + i + 1);
                    vertexData.Triangles.Add(startIndex + i + 2);
                }
            }
            else if (opcode == GxOpcode.TriangleStrip)
            {
                for (int i = 0; i < vertexCount - 2; i++)
                {
                    if (i % 2 == 0)
                    {
                        vertexData.Triangles.Add(startIndex + i);
                        vertexData.Triangles.Add(startIndex + i + 1);
                        vertexData.Triangles.Add(startIndex + i + 2);
                    }
                    else
                    {
                        vertexData.Triangles.Add(startIndex + i + 1);
                        vertexData.Triangles.Add(startIndex + i);
                        vertexData.Triangles.Add(startIndex + i + 2);
                    }
                }
            }
        }
    }

    private void ProcessMatrixIndex(
        byte info,
        MeshPacket meshPacket,
        List<int?> boneIndices,
        List<BoneWeight1[]> allVertexWeights
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

        // TODO: here?
        if (attachmentIndex >= 0)
        {
            int bIndex = attachmentIndex;

            if (!_BoneWeightsByBone.TryGetValue(bIndex, out var weights))
            {
                weights = new List<BoneWeight1>
                {
                    new() { boneIndex = bIndex, weight = 1.0f },
                };
                _BoneWeightsByBone[bIndex] = weights.ToList();
            }

            allVertexWeights.Add(weights.ToArray());
            boneIndices.Add(bIndex);
        }
        else
        {
            int envelopeIndex = -1 - attachmentIndex;
            var envelopeWeights = _EnvelopeBoneWeights[envelopeIndex];

            allVertexWeights.Add(envelopeWeights);
            boneIndices.Add(null);
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
            int vertexIndex = i;
            ushort posIndex = positionIndices[i];
            ushort normalIndex = normalIndices.ElementAtOrDefault(i);
            ushort colorIndex = colorIndices.ElementAtOrDefault(i);
            BoneWeight1[] weights = data.VertexWeights.Count > 0 ? data.VertexWeights[i] : null;

            MODVertex vertex = CreateVertex(
                vertexIndex,
                posIndex,
                normalIndex,
                colorIndex,
                texCoordIndices,
                boneIndices,
                weights,
                flags
            );

            data.Vertices.Add(vertex);
        }

        return data;
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
}
