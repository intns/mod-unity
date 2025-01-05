using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BoneTool.Script.Runtime;
using MODFile;
using Unity.Mathematics;
using UnityEditor;
using UnityEngine;
using UnityEngine.XR;

public class MODVertex
{
    public Vector3 Position;
    public Vector3 LocalNormal;
    public Vector3 LocalTangent;
    public Color Color;
    public List<Vector2>[] Uvs = new List<Vector2>[8];
    public List<BoneWeight1> Weights;

    public Vector4 Tangent => new Vector4(LocalTangent.x, LocalTangent.y, LocalTangent.z, 1);
}

[Serializable]
public class MODUnity
{
    [Flags]
    public enum CreateFlags
    {
        None = 0,
        CreateCollision = 1 << 0,
        CreateTextures = 1 << 1,
        CreateSkeleton = 1 << 2,
    }

    public readonly Dictionary<TextureAttributes, Texture2D> _TextureMap = new();

    [SerializeField]
    private MOD _RawModelFile = new();

    private short[] _ActiveMatrices = new short[10];

    public MODUnity(BinaryReader reader)
    {
        _RawModelFile.Read(reader);
    }

    public bool HasVertices() => _RawModelFile.Vertices.Count > 0;

    public bool HasVertexNormals() => _RawModelFile.VertexNormals.Count > 0;

    public bool HasCollision() => _RawModelFile.CollisionTriangles.TriInfos.Count > 0;

    public bool HasJoints() => _RawModelFile.Joints.Count > 0;

    public bool HasTextures() => _RawModelFile.Textures.Count > 0;

    public void Create(CreateFlags flags, Transform rootObj)
    {
        if (flags.HasFlag(CreateFlags.CreateCollision))
        {
            if (HasCollision())
            {
                CreateCollisionMesh();
            }
        }

        if (HasTextures())
        {
            Debug.Log("Textures found: " + _RawModelFile.Textures.Count);
            CreateTextures();
        }
        else
        {
            Debug.Log("Warning: No textures found in file.");
        }

        if (flags.HasFlag(CreateFlags.CreateTextures))
        {
            // Clear the texture directory
            string textureDir = Path.Combine(Application.dataPath, "Textures");
            if (Directory.Exists(textureDir))
            {
                Directory.Delete(textureDir, true);
                Directory.CreateDirectory(textureDir);
            }

            // Write textures to disc
            int id = 0;
            foreach (TextureAttributes textureAttr in _RawModelFile.TextureAttributes)
            {
                Texture2D texture = _TextureMap[textureAttr];
                string path = Path.Combine(Application.dataPath, "Textures", id + ".png");
                id++;

                File.WriteAllBytes(path, texture.EncodeToPNG());
                Debug.Log("Texture saved to: " + path);
            }
        }

        if (!HasVertices() || !HasVertexNormals())
        {
            Debug.Log("Warning: No vertices or vertex normals found in file.");
            return;
        }

        if (flags.HasFlag(CreateFlags.CreateSkeleton))
        {
            if (HasJoints())
            {
                Debug.Log("Joints found: " + _RawModelFile.Joints.Count);
                CreateSkeleton(rootObj);
            }
            else
            {
                Debug.Log("Warning: No joints found in file.");
            }
        }
    }

    public GameObject CreateCollisionMesh()
    {
        GameObject collision = new("Collision");
        collision.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);

        collision.AddComponent<MeshFilter>().mesh = _RawModelFile.GetUnityCollisionTriangles();

        MeshRenderer meshRenderer = collision.AddComponent<MeshRenderer>();
        meshRenderer.material = new UnityEngine.Material(Shader.Find("Standard"));
        meshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

        return collision;
    }

    private List<Transform> CreateBoneTransforms(Transform rootObj, List<int>[] jointChildren)
    {
        List<Transform> bones = new();
        Queue<(int, Transform)> jointQueue = new();

        jointQueue.Enqueue((0, null));
        while (jointQueue.Count > 0)
        {
            (int jointIndex, Transform parent) = jointQueue.Dequeue();
            MODFile.Joint joint = _RawModelFile.Joints[jointIndex];

            GameObject bone = new();

            if (parent == null)
            {
                // Set the root node
                bone.name = "ROOT_NODE";
                bone.AddComponent<BoneVisualiser>().RootNode = bone.transform;
            }
            else if (_RawModelFile.JointNames.Count > 0)
            {
                // Use the joint name if available
                bone.name = _RawModelFile.JointNames[jointIndex];
            }
            else
            {
                // Otherwise use a generic name
                bone.name = $"Joint_{jointIndex}";
            }

            bone.transform.SetParent((parent != null) ? parent.transform : rootObj, false);

            Quaternion rotationZ = Quaternion.AngleAxis(
                joint.Rotation.Vector.z * Mathf.Rad2Deg,
                Vector3.forward
            );

            Quaternion rotationY = Quaternion.AngleAxis(
                joint.Rotation.Vector.y * Mathf.Rad2Deg,
                Vector3.up
            );

            Quaternion rotationX = Quaternion.AngleAxis(
                joint.Rotation.Vector.x * Mathf.Rad2Deg,
                Vector3.right
            );

            // Apply rotations in Z-Y-X order
            Quaternion rotation = rotationZ * rotationY * rotationX;
            bone.transform.localPosition = joint.Position.Vector;
            bone.transform.localRotation = rotation;
            bone.transform.localScale = joint.Scale.Vector;

            bones.Add(bone.transform);
            foreach (int childIndex in jointChildren[jointIndex])
            {
                jointQueue.Enqueue((childIndex, bone.transform));
            }
        }

        return bones;
    }

    private void AddSortedMatPolySiblings(
        LinkedList<MODFile.Joint.MatPoly> sortedMatPolys,
        IEnumerable<MODFile.Joint> joints,
        Dictionary<MODFile.Joint, List<MODFile.Joint>> childrenByJoint,
        MaterialFlags targetFlags
    )
    {
        foreach (MODFile.Joint joint in joints)
        {
            if (childrenByJoint.TryGetValue(joint, out List<MODFile.Joint> children))
            {
                AddSortedMatPolySiblings(sortedMatPolys, children, childrenByJoint, targetFlags);
            }

            foreach (MODFile.Joint.MatPoly matpoly in joint.MatPolys)
            {
                MODFile.Material modMaterial = _RawModelFile.Materials._Materials[
                    matpoly.MaterialIndex
                ];

                if (((int)modMaterial._Flags & ((int)targetFlags >> 8)) != 0)
                {
                    sortedMatPolys.AddFirst(matpoly);
                }
            }
        }
    }

    private void CreateSkeleton(Transform rootObj)
    {
        // Pass 1: Create lists of children for each joint
        int jointCount = _RawModelFile.Joints.Count;
        List<int>[] jointChildren = new List<int>[jointCount];
        for (int i = 0; i < jointCount; i++)
        {
            jointChildren[i] = new();
        }

        // Pass 2: Populate joint children based on parent index
        Dictionary<MODFile.Joint, List<MODFile.Joint>> childrenByJoint = new();
        for (int i = 0; i < jointCount; i++)
        {
            int parentIndex = _RawModelFile.Joints[i].ParentIndex;
            if (parentIndex != -1)
            {
                jointChildren[parentIndex].Add(i);

                if (!childrenByJoint.ContainsKey(_RawModelFile.Joints[parentIndex]))
                {
                    childrenByJoint[_RawModelFile.Joints[parentIndex]] = new List<MODFile.Joint>();
                }
                childrenByJoint[_RawModelFile.Joints[parentIndex]].Add(_RawModelFile.Joints[i]);
            }
        }

        // Create Unity bones
        List<Transform> bones = CreateBoneTransforms(rootObj, jointChildren);

        // Preallocate memory for the lists
        List<BoneWeight1[]> envelopeBoneWeights = new(_RawModelFile.Envelopes.Count);
        List<float4x4> envelopeInverseMatrices = new(_RawModelFile.Envelopes.Count);
        SetupEnvelopeMatrices(bones, envelopeBoneWeights, envelopeInverseMatrices);

        LinkedList<MODFile.Joint.MatPoly> sortedMatPolys = new LinkedList<MODFile.Joint.MatPoly>();
        AddSortedMatPolySiblings(
            sortedMatPolys,
            new List<MODFile.Joint> { _RawModelFile.Joints[0] },
            childrenByJoint,
            MaterialFlags.TransparentBlend
        );
        AddSortedMatPolySiblings(
            sortedMatPolys,
            new List<MODFile.Joint> { _RawModelFile.Joints[0] },
            childrenByJoint,
            MaterialFlags.AlphaClip
        );
        AddSortedMatPolySiblings(
            sortedMatPolys,
            new List<MODFile.Joint> { _RawModelFile.Joints[0] },
            childrenByJoint,
            MaterialFlags.Opaque
        );

        // Writes materials
        List<UnityEngine.Material> unityMaterials = _RawModelFile.Materials.GetUnityMaterials(
            _TextureMap,
            _RawModelFile.TextureAttributes
        );

        foreach (var bone in bones)
        {
            Debug.Log($"Bone: {bone.name}");
        }

        foreach (MODFile.Joint.MatPoly matPoly in sortedMatPolys)
        {
            MODFile.Mesh mesh = _RawModelFile.Meshes[matPoly.MeshIndex];
            UnityEngine.Material material = unityMaterials[matPoly.MaterialIndex];

            GameObject newMesh = AddMesh(
                mesh,
                material,
                bones,
                envelopeBoneWeights,
                envelopeInverseMatrices
            );

            newMesh.transform.SetParent(rootObj);
        }
    }

    private void SetupEnvelopeMatrices(
        List<Transform> bones,
        List<BoneWeight1[]> envelopeBoneWeights,
        List<float4x4> envelopeInverseMatrices
    )
    {
        foreach (Envelope envelope in _RawModelFile.Envelopes)
        {
            // Validate weights before creating BoneWeight1 array
            float weightSum = envelope.Weights.Sum();
            if (!Mathf.Approximately(weightSum, 1f))
            {
                Debug.LogError($"Envelope weights don't sum to 1 ({weightSum})");
            }

            BoneWeight1[] boneWeights = new BoneWeight1[envelope.Indices.Count];
            for (int i = 0; i < envelope.Indices.Count; i++)
            {
                boneWeights[i] = new BoneWeight1
                {
                    boneIndex = envelope.Indices[i],
                    weight = envelope.Weights[i],
                };
            }

            float4x4 inverseMatrix = float4x4.zero;
            for (int i = 0; i < envelope.Indices.Count; i++)
            {
                boneWeights[i] = new BoneWeight1
                {
                    boneIndex = envelope.Indices[i],
                    weight = envelope.Weights[i]
                };

                int boneIndex = boneWeights[i].boneIndex;
                float weight = boneWeights[i].weight;

                float4x4 boneMatrix = bones[boneIndex].localToWorldMatrix;
                inverseMatrix += boneMatrix * weight;
            }

            envelopeBoneWeights.Add(boneWeights);
            envelopeInverseMatrices.Add(inverseMatrix);
        }
    }

    private GameObject AddMesh(
        MODFile.Mesh _mesh,
        UnityEngine.Material _material,
        IReadOnlyList<Transform> _bones,
        List<BoneWeight1[]> _boneWeights,
        List<float4x4> _envelopeInverseMatrices
    )
    {
        DisplayListReader reader = new(_RawModelFile, _bones, _boneWeights);
        DisplayListReader.VertexData vertexData = reader.ReadMesh(_mesh);

        return MeshSetup.CreateSkinnedMesh(vertexData, _material, _bones, _envelopeInverseMatrices);
    }

    private void CreateTextures()
    {
        foreach (TextureAttributes texAttr in _RawModelFile.TextureAttributes)
        {
            MODFile.Texture texture = _RawModelFile.Textures[texAttr.Index];
            Texture2D convertedTexture = texture.ConvertToUnityTexture(texAttr);
            _TextureMap.Add(texAttr, convertedTexture);
        }
    }
}
