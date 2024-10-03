using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BoneTool.Script.Runtime;
using LibGC.Texture;
using mod.schema;
using MODFile;
using Unity.Collections;
using Unity.Mathematics;
using UnityEditor;
using UnityEngine;

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

        BoneVisualiser bv = rootObj.gameObject.AddComponent<BoneVisualiser>();

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
                bv.RootNode = bone.transform;
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
                if ((modMaterial._Flags & targetFlags) != 0)
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
            // Add debug logging
            Debug.Log($"Processing envelope with {envelope.Indices.Count} influences");

            if (envelope.Indices.Count != envelope.Weights.Count)
            {
                Debug.LogError(
                    $"Mismatched envelope data: {envelope.Indices.Count} indices but {envelope.Weights.Count} weights"
                );
                continue;
            }

            // Validate weights before creating BoneWeight1 array
            float weightSum = envelope.Weights.Sum();
            if (Mathf.Approximately(weightSum, 0f))
            {
                Debug.LogWarning($"Envelope has zero total weight - normalizing to first bone");
                BoneWeight1[] defaultWeight = new BoneWeight1[]
                {
                    new() { boneIndex = envelope.Indices[0], weight = 1.0f },
                };
                envelopeBoneWeights.Add(defaultWeight);
                continue;
            }

            if (!Mathf.Approximately(weightSum, 1f))
            {
                Debug.LogError($"Envelope weights don't sum to 1 ({weightSum})");
            }

            BoneWeight1[] boneWeights = envelope
                .Indices.Select(
                    (boneIndex, i) =>
                        new BoneWeight1 { boneIndex = boneIndex, weight = envelope.Weights[i] }
                )
                .ToArray();

            // Debug log the weights
            Debug.Log(
                $"Envelope weights: {string.Join(", ", boneWeights.Select(w => $"Bone {w.boneIndex}: {w.weight:F3}"))}"
            );

            envelopeBoneWeights.Add(boneWeights);

            // Calculate the inverse matrix for the envelope
            Vector3 restPosition = Vector3.zero;
            Quaternion restRotation = Quaternion.identity;
            Vector3 restScale = Vector3.one;

            for (int i = 0; i < envelope.Indices.Count; i++)
            {
                Transform bone = bones[envelope.Indices[i]];
                float weight = boneWeights[i].weight; // Use normalized weights

                restPosition += bone.localPosition * weight;
                restRotation = Quaternion.Slerp(restRotation, bone.localRotation, weight);
                restScale += (bone.localScale - Vector3.one) * weight;
            }

            Matrix4x4 restMatrix = Matrix4x4.TRS(restPosition, restRotation, restScale);
            float4x4 inverseMatrix = math.inverse(restMatrix);
            envelopeInverseMatrices.Add(inverseMatrix);
        }
    }

    public enum BoneWeightType
    {
        BoneWeight,
        EnvelopeWeight,
    }

    public class BoneWeightIndex
    {
        public int _Index;
        public BoneWeightType _Type;
    }

    private void AssignVertexWeights(
        MODVertex vertex,
        BoneWeight1[] weights,
        int vertexIndex,
        Vector3 originalPosition,
        IReadOnlyList<Transform> _bones
    )
    {
        float weightSum = weights.Sum(w => w.weight);
        if (Mathf.Abs(weightSum - 1f) > 0.01f)
        {
            Debug.LogWarning($"Vertex {vertexIndex} weights don't sum to 1 ({weightSum})");

            // Normalize weights
            float scale = 1f / weightSum;
            weights = weights
                .Select(w => new BoneWeight1 { boneIndex = w.boneIndex, weight = w.weight * scale })
                .ToArray();
        }

        // Validate bone indices
        foreach (var weight in weights)
        {
            if (weight.boneIndex < 0 || weight.boneIndex >= _bones.Count)
            {
                Debug.LogError($"Invalid bone index {weight.boneIndex} for vertex {vertexIndex}");
                return;
            }
        }

        vertex.Weights = weights.ToList();
        vertex.Position = originalPosition; // Store the original position
    }

    private GameObject AddMesh(
        MODFile.Mesh _mesh,
        UnityEngine.Material _material,
        IReadOnlyList<Transform> _bones,
        List<BoneWeight1[]> _boneWeights,
        List<float4x4> _envelopeInverseMatrices
    )
    {
        SetupNewMesh(
            _material,
            out GameObject gameObj,
            out UnityEngine.Mesh gameMesh,
            out Renderer gameRenderer,
            out MeshFilter gameMeshFilter
        );

        // Setup vertex descriptor
        VertexDescriptor flags = new();
        flags.FromPikmin1((uint)_mesh.VertexDescriptor, _RawModelFile.VertexNormals.Count != 0);
        (GxAttribute, GxAttributeType?)[] descriptor = flags.ToArray();

        // Create room for the resultant data
        List<Vector3> vertices = new();
        List<Vector3> normals = new();
        List<Vector4> tangents = new();
        List<Color> colors = new();
        List<Vector2>[] uvs = new List<Vector2>[8];
        List<BoneWeight1> weights = new();
        List<int> boneCounts = new();
        List<int> triangles = new();

        foreach (MeshPacket meshPacket in _mesh.Packets)
        {
            foreach (DisplayList dlist in meshPacket.DisplayLists)
            {
                BinaryReader br = new(new MemoryStream(dlist.DisplayData));

                while (br.BaseStream.Position < br.BaseStream.Length)
                {
                    GxOpcode opcode = (GxOpcode)br.ReadByte();

                    if (opcode == GxOpcode.Nop)
                    {
                        continue;
                    }

                    if (opcode != GxOpcode.TriangleStrip && opcode != GxOpcode.TriangleFan)
                    {
                        continue;
                    }

                    List<ushort> positionIndices = new();
                    List<ushort> normalIndices = new();
                    List<ushort> colorIndices = new();
                    List<BoneWeight1[]> vertexWeights = new();
                    List<BoneWeightIndex> boneIndices = new();
                    List<ushort>[] texCoordIndices = new List<ushort>[8];
                    for (int t = 0; t < 8; ++t)
                    {
                        texCoordIndices[t] = new List<ushort>();
                    }

                    short faceCount = br.ReadInt16BE();
                    for (int f = 0; f < faceCount; f++)
                    {
                        foreach ((GxAttribute attr, GxAttributeType? format) in descriptor)
                        {
                            if (format == null)
                            {
                                byte info = br.ReadByte();

                                if (attr == GxAttribute.PNMTXIDX)
                                {
                                    // This is to represent one of the 10 active matrices.
                                    int activeMatrixIndex = info / 3;

                                    // This is the vertex matrix that is currently active.
                                    short vertexMatrixIndex = (short)
                                        meshPacket.Indices[activeMatrixIndex];

                                    if (vertexMatrixIndex == -1)
                                    {
                                        vertexMatrixIndex = _ActiveMatrices[activeMatrixIndex];
                                    }

                                    _ActiveMatrices[activeMatrixIndex] = vertexMatrixIndex;

                                    int attachmentIndex = _RawModelFile
                                        .VertexMatrices[vertexMatrixIndex]
                                        .Index;

                                    if (attachmentIndex >= 0)
                                    {
                                        // For direct bone attachment, weight should be 1
                                        vertexWeights.Add(
                                            new BoneWeight1[]
                                            {
                                                new()
                                                {
                                                    boneIndex = attachmentIndex,
                                                    weight = 1.0f,
                                                },
                                            }
                                        );
                                    }
                                    else
                                    {
                                        // For envelope weights, we need to ensure they're properly initialized
                                        int envelopeIndex = -1 - attachmentIndex;
                                        var envelopeWeights = _boneWeights[envelopeIndex];

                                        // Validate envelope weights
                                        if (
                                            envelopeWeights.Length == 0
                                            || envelopeWeights.All(w => w.weight == 0)
                                        )
                                        {
                                            Debug.LogWarning(
                                                $"Found zero weights in envelope {envelopeIndex}"
                                            );
                                            // Default to full weight on the first bone if weights are invalid
                                            vertexWeights.Add(
                                                new BoneWeight1[]
                                                {
                                                    new() { boneIndex = 0, weight = 1.0f },
                                                }
                                            );
                                        }
                                        else
                                        {
                                            vertexWeights.Add(envelopeWeights);
                                        }
                                    }
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
                                    {
                                        br.ReadInt16();
                                    }
                                    else
                                    {
                                        throw new Exception(
                                            $"Unknown attribute {attr} format: {format}"
                                        );
                                    }

                                    break;
                            }
                        }
                    }

                    // Create vertex list
                    List<MODVertex> vertexList = new();
                    for (int i = 0; i < positionIndices.Count; i++)
                    {
                        // Position is always enabled
                        MODVertex vertex = new()
                        {
                            Position = (Vector3)_RawModelFile.Vertices[positionIndices[i]],
                        };

                        if (normalIndices.Count > 0 && _RawModelFile.VertexNormals.Count > 0)
                        {
                            ushort normalIndex = normalIndices[i];

                            if (flags._NBTEnabled)
                            {
                                int nbtIndex = normalIndex;

                                Debug.Log(
                                    $"NBT index: {nbtIndex} / {_RawModelFile.VertexNBT.Count}"
                                );

                                vertex.LocalNormal = new Vector3(
                                    _RawModelFile.VertexNBT[normalIndex].Normal.Vector.x,
                                    _RawModelFile.VertexNBT[normalIndex].Normal.Vector.y,
                                    _RawModelFile.VertexNBT[normalIndex].Normal.Vector.z
                                );

                                vertex.LocalTangent = new Vector3(
                                    _RawModelFile.VertexNBT[normalIndex].Tangent.Vector.x,
                                    _RawModelFile.VertexNBT[normalIndex].Tangent.Vector.y,
                                    _RawModelFile.VertexNBT[normalIndex].Tangent.Vector.z
                                );
                            }
                            else if (normalIndex < _RawModelFile.VertexNormals.Count)
                            {
                                vertex.LocalNormal = new Vector3(
                                    _RawModelFile.VertexNormals[normalIndex].Vector.x,
                                    _RawModelFile.VertexNormals[normalIndex].Vector.y,
                                    _RawModelFile.VertexNormals[normalIndex].Vector.z
                                );
                            }
                            else
                            {
                                Debug.LogError(
                                    $"Vertex normal index {normalIndex} is out of range"
                                );

                                vertex.LocalNormal = Vector3.zero;
                            }
                        }

                        if (vertexWeights.Count > 0)
                        {
                            AssignVertexWeights(
                                vertex,
                                vertexWeights[i],
                                i,
                                vertex.Position,
                                _bones
                            );
                        }

                        if (colorIndices.Count > 0)
                        {
                            ushort colorIndex = colorIndices[i];
                            vertex.Color = new Color(
                                _RawModelFile.VertexColours[colorIndex].R,
                                _RawModelFile.VertexColours[colorIndex].G,
                                _RawModelFile.VertexColours[colorIndex].B,
                                _RawModelFile.VertexColours[colorIndex].A
                            );
                        }
                        else
                        {
                            vertex.Color = Color.white;
                        }

                        for (int channel = 0; channel < 8; channel++)
                        {
                            if (texCoordIndices[channel].Count > 0)
                            {
                                ushort txIndex = texCoordIndices[channel][i];

                                Vector2 coord = _RawModelFile.TextureCoordinates.GetTexCoord(
                                    channel,
                                    txIndex
                                );

                                if (vertex.Uvs[channel] == null)
                                    vertex.Uvs[channel] = new List<Vector2>();

                                vertex.Uvs[channel].Add(coord);
                            }
                        }

                        vertexList.Add(vertex);
                    }

                    // Process each triangle
                    int startIndex = vertices.Count;
                    foreach (MODVertex vertex in vertexList)
                    {
                        vertices.Add(vertex.Position);
                        normals.Add(vertex.LocalNormal);
                        if (vertex.LocalTangent != Vector3.zero)
                        {
                            tangents.Add(vertex.Tangent);
                        }

                        colors.Add(vertex.Color);

                        if (vertex.Weights != null)
                        {
                            weights.AddRange(vertex.Weights);
                            boneCounts.Add(vertex.Weights.Count);
                        }

                        for (int t = 0; t < 8; t++)
                        {
                            if (vertex.Uvs[t] != null)
                            {
                                if (uvs[t] == null)
                                    uvs[t] = new List<Vector2>();

                                uvs[t].AddRange(vertex.Uvs[t]);
                            }
                        }
                    }

                    if (opcode == GxOpcode.TriangleFan)
                    {
                        for (int i = 0; i < vertexList.Count - 2; i++)
                        {
                            triangles.Add(startIndex);
                            triangles.Add(startIndex + i + 1);
                            triangles.Add(startIndex + i + 2);
                        }
                    }
                    else if (opcode == GxOpcode.TriangleStrip)
                    {
                        for (int i = 0; i < vertexList.Count - 2; i++)
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
            }
        }

        // Assign the lists to the mesh
        gameMesh.vertices = vertices.ToArray();

        if (normals.Count > 0)
        {
            gameMesh.normals = normals.ToArray();
        }

        if (tangents.Count > 0)
        {
            gameMesh.tangents = tangents.ToArray();
        }

        if (colors.Count > 0)
        {
            gameMesh.colors = colors.ToArray();
        }

        for (int t = 0; t < 8; t++)
        {
            if (uvs[t] == null)
            {
                continue;
            }

            gameMesh.SetUVs(t, uvs[t]);
        }

        gameMesh.SetTriangles(triangles, 0);

        if (weights.Count > 0)
        {
            using (
                NativeArray<byte> bonesPerVertex = new(
                    boneCounts.Select(bc => (byte)bc).ToArray(),
                    Allocator.Temp
                )
            )
            using (
                NativeArray<BoneWeight1> weightsPerVertex = new(weights.ToArray(), Allocator.Temp)
            )
            {
                gameMesh.SetBoneWeights(bonesPerVertex, weightsPerVertex);
            }

            SkinnedMeshRenderer smr = (SkinnedMeshRenderer)gameRenderer;
            Transform rootBone = _bones[0];
            smr.rootBone = rootBone;
            smr.bones = _bones.ToArray();

            // Calculate the transformation from model space to root bone space
            // This accounts for any initial transformation that might be needed
            // Matrix4x4 modelToRootSpace = Matrix4x4.TRS(
            //     Vector3.zero,
            //     // Adjust this rotation to match your model's orientation
            //     Quaternion.Euler(-90, 0, 0), // Common adjustment for models exported from different coordinate systems
            //     Vector3.one
            // );

            // Calculate bind poses for each bone
            Matrix4x4[] bindPoses = new Matrix4x4[_bones.Count];
            for (int i = 0; i < _bones.Count; i++)
            {
                Transform bone = _bones[i];

                // Get the bone's world transform
                Matrix4x4 boneWorldToLocal = bone.worldToLocalMatrix;
                Matrix4x4 rootLocalToWorld = rootBone.localToWorldMatrix;

                // Include the model space transformation in the bind pose
                bindPoses[i] = boneWorldToLocal * rootLocalToWorld;
            }

            gameMesh.bindposes = bindPoses;

            // Transform vertices from model space to root bone space
            Vector3[] transformedVertices = new Vector3[vertices.Count];
            Vector3[] transformedNormals = new Vector3[normals.Count];

            for (int i = 0; i < vertices.Count; i++)
            {
                // Transform position
                Vector4 vertexPos = new Vector4(vertices[i].x, vertices[i].y, vertices[i].z, 1);
                transformedVertices[i] = new Vector3(vertexPos.x, vertexPos.y, vertexPos.z);

                // Transform normal if available
                if (i < normals.Count)
                {
                    // For normals, we don't apply translation, only rotation
                    Vector4 normalVec = new Vector4(normals[i].x, normals[i].y, normals[i].z, 0);
                    transformedNormals[i] = new Vector3(
                        normalVec.x,
                        normalVec.y,
                        normalVec.z
                    ).normalized;
                }
            }

            gameMesh.vertices = transformedVertices;
            if (normals.Count > 0)
            {
                gameMesh.normals = transformedNormals;
            }

            // Transform tangents if they exist
            if (tangents.Count > 0)
            {
                Vector4[] transformedTangents = new Vector4[tangents.Count];
                for (int i = 0; i < tangents.Count; i++)
                {
                    Vector4 tan = tangents[i];
                    Vector4 tangentVec = new Vector4(tan.x, tan.y, tan.z, 0);
                    transformedTangents[i] = new Vector4(
                        tangentVec.x,
                        tangentVec.y,
                        tangentVec.z,
                        tan.w // Preserve the handedness
                    );
                }
                gameMesh.tangents = transformedTangents;
            }
        }

        gameMesh.RecalculateBounds();

        // Position the mesh object relative to the root bone
        gameObj.transform.SetParent(_bones[0], false);
        gameObj.transform.localPosition = Vector3.zero;
        gameObj.transform.localRotation = Quaternion.identity;
        gameObj.transform.localScale = Vector3.one;

        // Assign the mesh to the renderer
        (gameRenderer as SkinnedMeshRenderer).sharedMesh = gameMesh;
        gameMeshFilter.sharedMesh = gameMesh;

        return gameObj;
    }

    private static void SetupNewMesh(
        UnityEngine.Material _material,
        out GameObject gameObj,
        out UnityEngine.Mesh gameMesh,
        out Renderer gameRenderer,
        out MeshFilter gameMeshFilter
    )
    {
        gameObj = new("Mesh", new[] { typeof(MeshFilter), typeof(SkinnedMeshRenderer) });
        gameObj.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
        gameMesh = new();
        gameMeshFilter = gameObj.GetComponent<MeshFilter>();

        gameRenderer = gameObj.GetComponent<SkinnedMeshRenderer>();
        gameRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        gameRenderer.material = _material;
    }

    private ushort DisplayListReadAttribute(BinaryReader reader, GxAttributeType format)
    {
        return format switch
        {
            GxAttributeType.Index16 => (ushort)reader.ReadInt16BE(),
            GxAttributeType.Index8 => reader.ReadByte(),
            _ => throw new Exception("Unknown attribute format: " + format),
        };
    }

    private void CreateTextures()
    {
        for (int i = 0; i < _RawModelFile.TextureAttributes.Count; i++)
        {
            TextureAttributes textureAttr = _RawModelFile.TextureAttributes[i];
            MODFile.Texture texture = _RawModelFile.Textures[textureAttr.Index];

            // Decode texture
            GcTextureFormatCodec tex;
            switch (texture.Format)
            {
                case MODFile.TextureFormat.RGB565:
                    tex = new GcTextureFormatCodecRGB565();
                    break;
                case MODFile.TextureFormat.CMPR:
                    tex = new GcTextureFormatCodecCMPR();
                    break;
                case MODFile.TextureFormat.RGB5A3:
                    tex = new GcTextureFormatCodecRGB5A3();
                    break;
                case MODFile.TextureFormat.I4:
                    tex = new GcTextureFormatCodecI4();
                    break;
                case MODFile.TextureFormat.I8:
                    tex = new GcTextureFormatCodecI8();
                    break;
                case MODFile.TextureFormat.IA4:
                    tex = new GcTextureFormatCodecIA4();
                    break;
                case MODFile.TextureFormat.IA8:
                    tex = new GcTextureFormatCodecIA8();
                    break;
                case MODFile.TextureFormat.RGBA32:
                    tex = new GcTextureFormatCodecRGBA8();
                    break;
                default:
                    tex = null;
                    Debug.LogError("Unknown texture format: " + texture.Format);
                    break;
            }

            // Decode the texture
            Texture2D newTex = new(
                texture.Width,
                texture.Height,
                UnityEngine.TextureFormat.RGBA32,
                false
            );

            byte[] destData = new byte[texture.Width * texture.Height * 4];

            tex.DecodeTexture(
                destData,
                0,
                texture.Width,
                texture.Height,
                texture.Width * 4,
                texture.Data,
                0,
                null,
                0
            );
            newTex.LoadRawTextureData(destData);
            newTex.wrapModeU = textureAttr.ModeS;
            newTex.wrapModeV = textureAttr.ModeT;
            newTex.alphaIsTransparency = true;
            newTex.Apply();

            _TextureMap.Add(textureAttr, newTex);
        }
    }
}
