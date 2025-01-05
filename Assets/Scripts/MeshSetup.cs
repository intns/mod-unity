using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;

public static class MeshSetup
{
    public static GameObject CreateSkinnedMesh(
        DisplayListReader.VertexData vertexData,
        Material material,
        IReadOnlyList<Transform> bones,
        List<float4x4> envelopeInverseMatrices
    )
    {
        GameObject gameObj = new("Mesh", new[] { typeof(MeshFilter), typeof(SkinnedMeshRenderer) });
        gameObj.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);

        SkinnedMeshRenderer renderer = gameObj.GetComponent<SkinnedMeshRenderer>();
        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

        Mesh mesh = new();

        // Basic mesh data
        mesh.vertices = vertexData.Positions.ToArray();
        mesh.normals = vertexData.Normals.ToArray();
        if (vertexData.Tangents.Count > 0)
        {
            mesh.tangents = vertexData.Tangents.ToArray();
        }
        mesh.colors = vertexData.Colors.ToArray();
        mesh.triangles = vertexData.Triangles.ToArray();

        // UV channels
        for (int i = 0; i < 8; i++)
        {
            if (vertexData.UVs[i]?.Count > 0)
                mesh.SetUVs(i, vertexData.UVs[i]);
        }

        // Set up skinning if weights exist
        if (vertexData.Weights.Count > 0)
        {
            SetupSkinning(mesh, renderer, vertexData, bones, envelopeInverseMatrices);
        }

        // Finalize renderer setup
        renderer.sharedMaterial = material;
        renderer.sharedMesh = mesh;

        return gameObj;
    }

    private static void SetupSkinning(
        Mesh mesh,
        SkinnedMeshRenderer renderer,
        DisplayListReader.VertexData vertexData,
        IReadOnlyList<Transform> bones,
        List<float4x4> envelopeInverseMatrices
    )
    {
        // The bone weights should be in descending order (most significant first)
        var sortedWeights = vertexData.Weights.OrderByDescending(w => w.weight).ToArray();

        // Set bone weights
        using (
            NativeArray<byte> bonesPerVertex = new(
                vertexData.BoneCounts.Select(bc => (byte)bc).ToArray(),
                Allocator.Temp
            )
        )
        using (NativeArray<BoneWeight1> weightsPerVertex = new(sortedWeights, Allocator.Temp))
        {
            mesh.SetBoneWeights(bonesPerVertex, weightsPerVertex);
        }

        // Setup bones
        Transform rootBone = bones[0];
        renderer.rootBone = rootBone;
        renderer.bones = bones.ToArray();

        // Assert the vertex count and weight count match
        Debug.Assert(
            mesh.vertexCount == vertexData.Weights.Count,
            $"Vertex count {mesh.vertexCount} does not match weight count {vertexData.Weights.Count}"
        );

        // Calculate bind poses using envelope inverse matrices
        mesh.bindposes = bones.Select(b => b.worldToLocalMatrix).ToArray();
    }
}
