using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BoneTool.Script.Runtime;
using MODFile;
using Unity.Mathematics;
using UnityEngine;

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
    public MOD _RawModelFile = new();

    public MODUnity(BinaryReader reader)
    {
        _RawModelFile.Read(reader);
    }

    public MODUnity(MOD model)
    {
        _RawModelFile = model;
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
            // Convert textures to Unity textures and store them in a dictionary
            foreach (TextureAttributes texAttr in _RawModelFile.TextureAttributes)
            {
                MODFile.Texture texture = _RawModelFile.Textures[texAttr.Index];
                Texture2D convertedTexture = texture.ConvertToUnityTexture(texAttr);
                _TextureMap.Add(texAttr, convertedTexture);
            }
        }
        else
        {
            Debug.Log("Warning: No textures found in file.");
        }

        if (flags.HasFlag(CreateFlags.CreateTextures))
        {
            string textureDir = Path.Combine(Application.dataPath, "Textures");

            // Clear the textures directory if it exists, then recreate it
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
            }
        }

        // Don't create a skeleton if there are no vertices or vertex normals to display
        if (!HasVertices() || !HasVertexNormals())
        {
            Debug.Log("Warning: No vertices or vertex normals found in file.");
            return;
        }

        if (flags.HasFlag(CreateFlags.CreateSkeleton) && HasJoints())
        {
            CreateSkeleton(rootObj);
        }
        else
        {
            // Create a mesh if no skeleton is required
            Debug.Log("Creating static mesh.");

            // Create Unity materials
            var unityMaterials = _RawModelFile.Materials.GetUnityMaterials(
                _TextureMap,
                _RawModelFile.TextureAttributes
            );

            // Create each mesh in the sorted order
            int meshIndex = 0;
            foreach (var mesh in _RawModelFile.Meshes)
            {
                // UnityEngine.Material material = unityMaterials[meshIndex];

                DisplayListReader reader = new DisplayListReader(_RawModelFile, null, null);

                // Read and parse the mesh data
                DisplayListReader.VertexData vertexData = reader.ReadMesh(mesh);

                // Create the Unity gameobject mesh
                GameObject newMesh = MeshSetup.CreateStaticMesh(vertexData, null);
                newMesh.transform.SetParent(rootObj);
                newMesh.name = $"Mesh {meshIndex++}";
            }
        }

        // Static mesh support coming soon (TM) lol
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

    private Transform[] CreateBoneTransforms(Transform rootObj, List<int>[] jointChildren)
    {
        Transform[] bones = new Transform[jointChildren.Length];
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
                bone.name = "Root";
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
                bone.name = $"Bone {jointIndex}";
            }

            bone.transform.SetParent((parent != null) ? parent.transform : rootObj, false);

            // For some reason, applying the transforms in a normal way results
            // in the bones being mirrored across z. We have to apply these
            // rotations and fiddle with the joint position to get the bones
            // in the right place.
            Quaternion rotationZ = Quaternion.AngleAxis(
                -joint.Rotation.Vector.z * Mathf.Rad2Deg,
                Vector3.back
            );

            Quaternion rotationY = Quaternion.AngleAxis(
                -joint.Rotation.Vector.y * Mathf.Rad2Deg,
                Vector3.up
            );

            Quaternion rotationX = Quaternion.AngleAxis(
                -joint.Rotation.Vector.x * Mathf.Rad2Deg,
                Vector3.right
            );

            // Apply rotations in Z-Y-X order
            Quaternion rotation = rotationZ * rotationY * rotationX;

            bone.transform.SetLocalPositionAndRotation(
                this.FlipPosition(joint.Position.Vector),
                rotation
            );
            bone.transform.localScale = joint.Scale.Vector;

            bones[jointIndex] = bone.transform;
            foreach (int childIndex in jointChildren[jointIndex])
            {
                jointQueue.Enqueue((childIndex, bone.transform));
            }
        }

        return bones;
    }

    private Vector3 FlipPosition(Vector3 vec3)
    {
        return new Vector3(vec3.x, vec3.y, -vec3.z);
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
        // Build a map of parent joints to their children joints
        int jointCount = _RawModelFile.Joints.Count;
        var jointChildren = new List<int>[jointCount];
        var boneChildrenMap = new Dictionary<MODFile.Joint, List<MODFile.Joint>>();
        for (int i = 0; i < jointCount; i++)
        {
            jointChildren[i] = new();

            var joint = _RawModelFile.Joints[i];

            // For each joint with a parent, add it to the parent's children list and the bone map
            if (joint.ParentIndex != -1)
            {
                // Track child indices for each parent joint
                jointChildren[joint.ParentIndex].Add(i);

                // Build map of parent joints to their children joints
                var parentJoint = _RawModelFile.Joints[joint.ParentIndex];
                if (!boneChildrenMap.TryGetValue(parentJoint, out var children))
                {
                    children = new List<MODFile.Joint>();
                    boneChildrenMap[parentJoint] = children;
                }

                children.Add(joint);
            }
        }

        // Create Unity gameobject skeleton
        Transform[] bones = CreateBoneTransforms(rootObj, jointChildren);

        // Calculate envelope matrices
        // TODO: We don't use the envelope inverse matrices anywhere
        List<BoneWeight1[]> envelopeBoneWeights = new(_RawModelFile.Envelopes.Count);
        List<float4x4> envelopeInverseMatrices = new(_RawModelFile.Envelopes.Count);
        SetupEnvelopeMatrices(bones, envelopeBoneWeights, envelopeInverseMatrices);

        // Create Unity materials
        var unityMaterials = _RawModelFile.Materials.GetUnityMaterials(
            _TextureMap,
            _RawModelFile.TextureAttributes
        );

        // Sort the polygons by material flags
        var polygonQueue = new LinkedList<MODFile.Joint.MatPoly>();
        var rootJoint = new List<MODFile.Joint>() { _RawModelFile.Joints[0] };
        AddSortedMatPolySiblings(
            polygonQueue,
            rootJoint,
            boneChildrenMap,
            MaterialFlags.TransparentBlend
        );
        AddSortedMatPolySiblings(polygonQueue, rootJoint, boneChildrenMap, MaterialFlags.AlphaClip);
        AddSortedMatPolySiblings(polygonQueue, rootJoint, boneChildrenMap, MaterialFlags.Opaque);

        // Create each mesh in the sorted order
        int meshIndex = 0;
        foreach (MODFile.Joint.MatPoly matPolygonData in polygonQueue)
        {
            MODFile.Mesh mesh = _RawModelFile.Meshes[matPolygonData.MeshIndex];
            UnityEngine.Material material = unityMaterials[matPolygonData.MaterialIndex];

            DisplayListReader reader = new DisplayListReader(
                _RawModelFile,
                bones,
                envelopeBoneWeights
            );

            // Read and parse the mesh data
            DisplayListReader.VertexData vertexData = reader.ReadMesh(mesh);

            // Create the Unity gameobject mesh
            GameObject newMesh = MeshSetup.CreateSkinnedMesh(vertexData, material, bones);
            newMesh.transform.SetParent(rootObj);
            newMesh.name = $"Mesh {meshIndex++}";
        }
    }

    private void SetupEnvelopeMatrices(
        Transform[] bones,
        List<BoneWeight1[]> envelopeBoneWeights,
        List<float4x4> envelopeInverseMatrices
    )
    {
        foreach (Envelope envelope in _RawModelFile.Envelopes)
        {
            BoneWeight1[] boneWeights = new BoneWeight1[envelope.Indices.Count];
            float4x4 inverseMatrix = float4x4.zero;

            for (int i = 0; i < envelope.Indices.Count; i++)
            {
                boneWeights[i] = new BoneWeight1
                {
                    boneIndex = envelope.Indices[i],
                    weight = envelope.Weights[i],
                };

                float4x4 boneMatrix = bones[boneWeights[i].boneIndex].localToWorldMatrix;
                inverseMatrix += boneMatrix * boneWeights[i].weight;
            }

            envelopeBoneWeights.Add(boneWeights);
            envelopeInverseMatrices.Add(inverseMatrix);
        }
    }
}
