using System.Collections.Generic;
using System.Linq;
using MODFile;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;

public static class MeshSetup
{
    public static GameObject CreateSkinnedMesh(
        DisplayListReader.VertexData vertexData,
        UnityEngine.Material material,
        Transform[] bones
    )
    {
        GameObject gameObj = new("Mesh", new[] { typeof(MeshFilter), typeof(SkinnedMeshRenderer) });
        gameObj.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);

        SkinnedMeshRenderer renderer = gameObj.GetComponent<SkinnedMeshRenderer>();
        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

        // Set up basic mesh data
        UnityEngine.Mesh mesh = new()
        {
            vertices = vertexData.Positions.ToArray(),
            normals = vertexData.Normals.ToArray(),
            triangles = vertexData.Triangles.ToArray(),
        };

        if (vertexData.Tangents.Count > 0)
        {
            mesh.tangents = vertexData.Tangents.ToArray();
        }

        if (vertexData.Colors.Count > 0)
        {
            mesh.colors = vertexData.Colors.ToArray();
        }

        // UV channels
        for (int i = 0; i < 8; i++)
        {
            if (vertexData.UVs[i]?.Count > 0)
            {
                mesh.SetUVs(i, vertexData.UVs[i]);
            }
        }

        // Set up skinning if weights exist
        if (vertexData.WeightsByVertex.Count > 0)
        {
            // Assert the vertex count and weight count match
            Debug.Assert(mesh.vertexCount == vertexData.WeightsByVertex.Count);

            var bonesPerVertex = new NativeArray<byte>(
                vertexData.WeightsByVertex.Select(w => (byte)w.Length).ToArray(),
                Allocator.Temp
            );

            var weights = new NativeArray<BoneWeight1>(
                vertexData.WeightsByVertex.SelectMany(w => w).ToArray(),
                Allocator.Temp
            );

            mesh.SetBoneWeights(bonesPerVertex, weights);

            renderer.bones = bones;
            renderer.quality = SkinQuality.Bone4;
            mesh.bindposes = bones.Select(b => b.worldToLocalMatrix).ToArray();
        }

        // Finalize renderer setup
        renderer.sharedMaterial = material;
        renderer.sharedMesh = mesh;

        return gameObj;
    }

    public static GameObject CreateStaticMesh(
        DisplayListReader.VertexData vertexData,
        UnityEngine.Material material
    )
    {
        GameObject gameObj = new("Mesh", new[] { typeof(MeshFilter), typeof(MeshRenderer) });
        gameObj.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);

        UnityEngine.Mesh mesh = new()
        {
            vertices = vertexData.Positions.ToArray(),
            normals = vertexData.Normals.ToArray(),
            triangles = vertexData.Triangles.ToArray(),
        };

        if (vertexData.Tangents.Count > 0)
        {
            mesh.tangents = vertexData.Tangents.ToArray();
        }

        if (vertexData.Colors.Count > 0)
        {
            mesh.colors = vertexData.Colors.ToArray();
        }

        for (int i = 0; i < 8; i++)
        {
            if (vertexData.UVs[i]?.Count > 0)
            {
                mesh.SetUVs(i, vertexData.UVs[i]);
            }
        }

        MeshFilter filter = gameObj.GetComponent<MeshFilter>();
        filter.sharedMesh = mesh;

        if (material != null)
        {
            MeshRenderer renderer = gameObj.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
        }

        return gameObj;
    }
}
