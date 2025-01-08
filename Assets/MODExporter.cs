using System.Collections.Generic;
using System.IO;
using System.Linq;
using mod.schema;
using MODFile;
using UnityEngine;
using UnityEngine.UIElements;
using static DisplayListReader;

public class MODExporter : MonoBehaviour
{
    [SerializeField]
    Transform _Target;

    [SerializeField]
    MOD _ModTarget;

    void Awake()
    {
        _ModTarget = new();
        UnityEngine.Mesh unityMesh = _Target.GetComponent<MeshFilter>().sharedMesh;

        VertexData vertexData = PopulateVertexData(unityMesh);
        foreach (var position in vertexData.Positions)
        {
            _ModTarget.Vertices.Add(new Vector3Readable(position.x, position.y, position.z));
        }

        // Write normals
        foreach (var normal in vertexData.Normals)
        {
            _ModTarget.VertexNormals.Add(new Vector3Readable(normal.x, normal.y, normal.z));
        }

        // Write UVs for each channel
        for (int channel = 0; channel < 8; channel++)
        {
            if (vertexData.UVs[channel] != null && vertexData.UVs[channel].Count > 0)
            {
                foreach (var uv in vertexData.UVs[channel])
                {
                    _ModTarget.TextureCoordinates.AddTexCoord(channel, uv);
                }
            }
        }

        // Write weights and bones
        // Create VtxMatrices for each bone
        if (_Target.TryGetComponent(out SkinnedMeshRenderer skinnedMeshRenderer))
        {
            foreach (Transform bone in skinnedMeshRenderer.bones)
            {
                VtxMatrix vtxMatrix = new()
                {
                    Index = skinnedMeshRenderer.bones.ToList().IndexOf(bone),
                };

                _ModTarget.VertexMatrices.Add(vtxMatrix);
            }
        }

        // Create Envelopes for each vertex's weights
        foreach (BoneWeight unityBoneWeight in unityMesh.boneWeights)
        {
            Envelope envelope = new();

            // Add non-zero weights and their indices
            if (unityBoneWeight.weight0 > 0)
            {
                envelope.Indices.Add((ushort)unityBoneWeight.boneIndex0);
                envelope.Weights.Add(unityBoneWeight.weight0);
            }
            if (unityBoneWeight.weight1 > 0)
            {
                envelope.Indices.Add((ushort)unityBoneWeight.boneIndex1);
                envelope.Weights.Add(unityBoneWeight.weight1);
            }
            if (unityBoneWeight.weight2 > 0)
            {
                envelope.Indices.Add((ushort)unityBoneWeight.boneIndex2);
                envelope.Weights.Add(unityBoneWeight.weight2);
            }
            if (unityBoneWeight.weight3 > 0)
            {
                envelope.Indices.Add((ushort)unityBoneWeight.boneIndex3);
                envelope.Weights.Add(unityBoneWeight.weight3);
            }

            _ModTarget.Envelopes.Add(envelope);
        }

        // Create mesh and packets
        MODFile.Mesh modMesh = new MODFile.Mesh();
        MeshPacket packet = new MeshPacket();
        DisplayList displayList = new DisplayList();
        int[] triangles = unityMesh.triangles;

        // Create display list from triangles
        using (MemoryStream ms = new MemoryStream())
        using (BinaryWriter writer = new BinaryWriter(ms))
        {
            // Convert triangles to strips
            writer.Write((byte)GxOpcode.TriangleStrip);
            for (int i = 0; i < triangles.Length; i++)
            {
                writer.Write((ushort)triangles[i]);
            }

            displayList.DisplayData = ms.ToArray();
            displayList.CommandCount = triangles.Length / 3;
            displayList.Flags = 0; // Front-facing
        }

        packet.DisplayLists = new[] { displayList };
        modMesh.Packets = new[] { packet };
        modMesh.BoneIndex = skinnedMeshRenderer != null ? 0 : -1;

        _ModTarget.Meshes.Add(modMesh);

        MODUnity newMod = new MODUnity(_ModTarget);
        newMod.Create(MODUnity.CreateFlags.CreateSkeleton, transform);
    }

    private VertexData PopulateVertexData(UnityEngine.Mesh unityMesh)
    {
        VertexData vertexData = new VertexData
        {
            Positions = new List<Vector3>(unityMesh.vertices),
            Normals = new List<Vector3>(unityMesh.normals),
            Colors = new List<Color>(unityMesh.colors),
            Triangles = new List<int>(unityMesh.triangles),
            UVs = new List<Vector2>[8],
            WeightsByVertex = new List<BoneWeight1[]>(),
            BoneCounts = new List<int>(),
        };

        // Handle UVs for all channels
        Vector2[][] uvsArray =
        {
            unityMesh.uv,
            unityMesh.uv2,
            unityMesh.uv3,
            unityMesh.uv4,
            unityMesh.uv5,
            unityMesh.uv6,
            unityMesh.uv7,
            unityMesh.uv8,
        };

        for (int i = 0; i < uvsArray.Length; i++)
        {
            if (uvsArray[i].Length > 0)
            {
                vertexData.UVs[i] = new List<Vector2>(uvsArray[i]);
            }
        }

        // Handle bone weights if mesh is skinned
        if (unityMesh.boneWeights.Length > 0)
        {
            foreach (BoneWeight bw in unityMesh.boneWeights)
            {
                List<BoneWeight1> weights = new List<BoneWeight1>();

                if (bw.weight0 > 0)
                    weights.Add(new BoneWeight1 { boneIndex = bw.boneIndex0, weight = bw.weight0 });
                if (bw.weight1 > 0)
                    weights.Add(new BoneWeight1 { boneIndex = bw.boneIndex1, weight = bw.weight1 });
                if (bw.weight2 > 0)
                    weights.Add(new BoneWeight1 { boneIndex = bw.boneIndex2, weight = bw.weight2 });
                if (bw.weight3 > 0)
                    weights.Add(new BoneWeight1 { boneIndex = bw.boneIndex3, weight = bw.weight3 });

                vertexData.WeightsByVertex.Add(weights.ToArray());
                vertexData.BoneCounts.Add(weights.Count);
            }
        }

        return vertexData;
    }
}
