using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace MODFile
{
    [Serializable]
    public enum GxColorSrc : byte
    {
        Register = 0, // Use Register Colors
        Vertex = 1, // Use Vertex Colors
    }

    public enum GxDiffuseFunction : byte
    {
        None = 0,
        Signed = 1,
        Clamp = 2,
    }

    public class KeyInfoU8
    {
        public byte _00;
        public float _04;
        public float _08;

        public void Read(BinaryReader reader)
        {
            _00 = reader.ReadByte();
            reader.ReadByte();
            reader.ReadByte();
            reader.ReadByte();

            _04 = reader.ReadSingleBE();
            _08 = reader.ReadSingleBE();
        }
    }

    public class KeyInfoF32
    {
        public float _Value;
        public float _InTangent;
        public float _OutTangent;

        public void Read(BinaryReader reader)
        {
            _Value = reader.ReadSingleBE();
            _InTangent = reader.ReadSingleBE();
            _OutTangent = reader.ReadSingleBE();
        }
    }

    public class KeyInfoS10
    {
        public short _Frame;
        public float _StartValue;
        public float _EndValue;

        public void Read(BinaryReader reader)
        {
            _Frame = reader.ReadInt16BE();
            reader.ReadInt16BE();

            _StartValue = reader.ReadSingleBE();
            _EndValue = reader.ReadSingleBE();
        }
    }

    public class AnimInfoU8
    {
        public int _00;
        public KeyInfoU8[] mList;

        public void Read(BinaryReader reader)
        {
            _00 = reader.ReadInt32BE();

            for (int i = 0; i < mList.Length; i++)
            {
                mList[i] = new KeyInfoU8();
                mList[i].Read(reader);
            }
        }
    }

    public class AnimInfoF32
    {
        public int _00;
        public KeyInfoF32[] mList;

        public void Read(BinaryReader reader)
        {
            _00 = reader.ReadInt32BE();

            for (int i = 0; i < mList.Length; i++)
            {
                mList[i] = new KeyInfoF32();
                mList[i].Read(reader);
            }
        }
    }

    public class AnimInfoS10
    {
        public int _00;
        public KeyInfoS10[] mList;

        public void Read(BinaryReader reader)
        {
            _00 = reader.ReadInt32BE();

            for (int i = 0; i < mList.Length; i++)
            {
                mList[i] = new KeyInfoS10();
                mList[i].Read(reader);
            }
        }
    }

    public class PVWAnimInfo3_F32
    {
        public AnimInfoF32[] mList;
        public int mSize;

        public void Read(BinaryReader reader)
        {
            mSize = reader.ReadInt32BE();
            if (mSize > 0)
            {
                mList = new AnimInfoF32[mSize];

                for (int i = 0; i < mSize; i++)
                {
                    mList[i] = new AnimInfoF32 { mList = new KeyInfoF32[3] };
                    mList[i].Read(reader);
                }
            }
        }
    }

    public class PVWAnimInfo1_S10
    {
        public AnimInfoS10[] mList;
        public int mSize;

        public void Read(BinaryReader reader)
        {
            mSize = reader.ReadInt32BE();
            if (mSize > 0)
            {
                mList = new AnimInfoS10[mSize];

                for (int i = 0; i < mSize; i++)
                {
                    mList[i] = new AnimInfoS10 { mList = new KeyInfoS10[1] };
                    mList[i].Read(reader);
                }
            }
        }
    }

    public class PVWAnimInfo3_S10
    {
        public AnimInfoS10[] mList;
        public int mSize;

        public void Read(BinaryReader reader)
        {
            mSize = reader.ReadInt32BE();
            if (mSize > 0)
            {
                mList = new AnimInfoS10[mSize];

                for (int i = 0; i < mSize; i++)
                {
                    mList[i] = new AnimInfoS10 { mList = new KeyInfoS10[3] };
                    mList[i].Read(reader);
                }
            }
        }
    }

    [Serializable]
    public class PVWAnimInfo1_U8
    {
        public AnimInfoU8[] mList;
        public int mSize;

        public void Read(BinaryReader reader)
        {
            mSize = reader.ReadInt32BE();
            if (mSize > 0)
            {
                mList = new AnimInfoU8[mSize];

                for (int i = 0; i < mSize; i++)
                {
                    mList[i] = new AnimInfoU8 { mList = new KeyInfoU8[1] };
                    mList[i].Read(reader);
                }
            }
        }
    }

    public class PVWAnimInfo3_U8
    {
        public AnimInfoU8[] mList;
        public int mSize;

        public void Read(BinaryReader reader)
        {
            mSize = reader.ReadInt32BE();
            if (mSize > 0)
            {
                mList = new AnimInfoU8[mSize];

                for (int i = 0; i < mSize; i++)
                {
                    mList[i] = new AnimInfoU8 { mList = new KeyInfoU8[3] };
                    mList[i].Read(reader);
                }
            }
        }
    }

    [Serializable]
    public class TexGenData
    {
        public byte _DestinationCoordReg;
        public byte _Func;
        public byte _SourceParam;
        public byte _03;

        public void Read(BinaryReader reader)
        {
            _DestinationCoordReg = reader.ReadByte();
            _Func = reader.ReadByte();
            _SourceParam = reader.ReadByte();
            _03 = reader.ReadByte();
        }
    }

    [Serializable]
    public class TextureData
    {
        public int _TexAttrIndex;
        public short _04;
        public short _06;
        public byte _08;
        public byte _09;
        public byte _0A;
        public byte _0B;
        public int _0C;
        public int _AnimationLength;
        public float _AnimationSpeed;
        public Vector3 _10;
        public Vector3 _1C;
        public float _28;

        PVWAnimInfo3_F32 _AnimInfo = new();
        PVWAnimInfo3_F32 _AnimInfo2 = new();
        PVWAnimInfo3_F32 _AnimInfo3 = new();

        public void Read(BinaryReader reader)
        {
            _TexAttrIndex = reader.ReadInt32BE();
            _04 = reader.ReadInt16BE();
            _06 = reader.ReadInt16BE();

            _08 = reader.ReadByte();
            _09 = reader.ReadByte();
            _0A = reader.ReadByte();
            _0B = reader.ReadByte();

            _0C = reader.ReadInt32BE();

            _AnimationLength = reader.ReadInt32BE();
            _AnimationSpeed = reader.ReadSingleBE();

            _10 = reader.ReadVector3();
            _1C = reader.ReadVector3();

            _28 = reader.ReadSingleBE();

            _AnimInfo.Read(reader);
            _AnimInfo2.Read(reader);
            _AnimInfo3.Read(reader);
        }
    }

    [Serializable]
    public class TextureInfo
    {
        public int _00;
        public Vector3 _04;
        public int _TexGenDataSize;
        public TexGenData[] _TexGenData;
        public int _08;

        public int _TexDataSize;
        public TextureData[] _TexData;

        public void Read(BinaryReader reader)
        {
            _00 = reader.ReadInt32BE();
            _04 = reader.ReadVector3();

            _TexGenDataSize = reader.ReadInt32BE();
            if (_TexGenDataSize > 0)
            {
                _TexGenData = new TexGenData[_TexGenDataSize];
                for (int i = 0; i < _TexGenDataSize; i++)
                {
                    _TexGenData[i] = new TexGenData();
                    _TexGenData[i].Read(reader);
                }
            }

            _TexDataSize = reader.ReadInt32BE();
            if (_TexDataSize > 0)
            {
                _TexData = new TextureData[_TexDataSize];
                for (int i = 0; i < _TexDataSize; i++)
                {
                    _TexData[i] = new TextureData();
                    _TexData[i].Read(reader);
                }
            }
        }
    }

    [Serializable]
    public enum GXBlendMode
    {
        GX_BM_NONE = 0,
        GX_BM_BLEND = 1,
        GX_BM_LOGIC = 2,
        GX_BM_SUBTRACT = 3,
    }

    [Serializable]
    public enum GXLogicOp
    {
        GX_LO_CLEAR,
        GX_LO_AND,
        GX_LO_REVAND,
        GX_LO_COPY,
        GX_LO_INVAND,
        GX_LO_NOOP,
        GX_LO_XOR,
        GX_LO_OR,
        GX_LO_NOR,
        GX_LO_EQUIV,
        GX_LO_INV,
        GX_LO_REVOR,
        GX_LO_INVCOPY,
        GX_LO_INVOR,
        GX_LO_NAND,
        GX_LO_SET,
    };

    [Serializable]
    public enum GXCompare
    {
        GX_NEVER = 0,
        GX_LESS = 1,
        GX_EQUAL = 2,
        GX_LEQUAL = 3,
        GX_GREATER = 4,
        GX_NEQUAL = 5,
        GX_GEQUAL = 6,
        GX_ALWAYS = 7,
    };

    [Serializable]
    public class PVWPeInfo
    {
        [Serializable, Flags]
        public enum Flags
        {
            Enabled = 1,
        }

        public Flags _Flags; // _00

        public struct AlphaCompareFunction
        {
            public uint value;

            public uint Comp0
            {
                get => value & 0xF;
                set => this.value = (this.value & 0xFFFFFFF0) | (value & 0xF);
            }

            public uint Ref0
            {
                get => (value >> 4) & 0xFF;
                set => this.value = (this.value & 0xFFFF00FF) | ((value & 0xFF) << 4);
            }

            public uint Op
            {
                get => (value >> 12) & 0xF;
                set => this.value = (this.value & 0xFFFF0FFF) | ((value & 0xF) << 12);
            }

            public uint Comp1
            {
                get => (value >> 16) & 0xF;
                set => this.value = (this.value & 0xFFF0FFFF) | ((value & 0xF) << 16);
            }

            public uint Ref1
            {
                get => (value >> 20) & 0xFF;
                set => this.value = (this.value & 0xFF00FFFF) | ((value & 0xFF) << 20);
            }
        }

        public AlphaCompareFunction mAlphaCompareFunction; // _04

        // something in here is the Comparison Function
        public uint mZModeFunction; // _08, (& 1 == compare enabled, & 2 == update enabled),

        public struct BlendMode
        {
            public uint value;

            public uint Type
            {
                get => value & 0xF;
                set => this.value = (this.value & 0xFFFFFFF0) | (value & 0xF);
            }

            public uint SrcFactor
            {
                get => (value >> 4) & 0xF;
                set => this.value = (this.value & 0xFFFFFF0F) | ((value & 0xF) << 4);
            }

            public uint DstFactor
            {
                get => (value >> 8) & 0xF;
                set => this.value = (this.value & 0xFFFFF0FF) | ((value & 0xF) << 8);
            }

            public uint LogicOp
            {
                get => (value >> 12) & 0xF;
                set => this.value = (this.value & 0xFFFF0FFF) | ((value & 0xF) << 12);
            }
        }

        public BlendMode mBlendMode; // _0C

        public void Read(BinaryReader reader)
        {
            _Flags = (Flags)reader.ReadInt32BE();
            mAlphaCompareFunction = new AlphaCompareFunction { value = (uint)reader.ReadInt32BE() };
            mZModeFunction = (uint)reader.ReadInt32BE();
            mBlendMode = new BlendMode { value = (uint)reader.ReadInt32BE() };
        }
    }

    [Flags, Serializable]
    public enum MaterialFlags
    {
        IsEnabled = 0x1,
        Opaque = 0x100,
        AlphaClip = 0x200, // 0x80 cutoff
        TransparentBlend = 0x400,
        InvertSpecialBlend = 0x8000,
        Hidden = 0x10000,
    };

    [Serializable]
    public class Material : IReadable
    {
        public MaterialFlags _Flags = MaterialFlags.IsEnabled;
        public int _00;
        public ColourU8 _DiffuseColor = new();

        public int _TevGroupIndex;

        public int _ColourAnimLength;
        public float _ColourAnimSpeed;

        public PVWAnimInfo1_U8 _ColourAnimInfo = new();
        public PVWAnimInfo3_U8 _AlphaAnimInfo = new();

        public uint _LightingInfoFlags;
        public float _04;

        public PVWPeInfo _PEInfo = new();

        public TextureInfo _TextureInfo = new();

        // Lighting properties
        public bool LightingEnabledForChannelControl0 => _LightingInfoFlags.GetBit(0);
        public bool LightingEnabledForChannelControl1 => _LightingInfoFlags.GetBit(1);
        public bool LightingEnabledForChannelControl2 => _LightingInfoFlags.GetBit(2);

        public GxDiffuseFunction DiffuseFunctionForChannel0 =>
            (GxDiffuseFunction)_LightingInfoFlags.ExtractFromRight(3, 2);
        public GxDiffuseFunction DiffuseFunctionForChannel1 =>
            (GxDiffuseFunction)_LightingInfoFlags.ExtractFromRight(7, 2);
        public GxDiffuseFunction DiffuseFunctionForChannel2 =>
            (GxDiffuseFunction)_LightingInfoFlags.ExtractFromRight(5, 2);

        public GxColorSrc AmbientColorSrcForChannel0 =>
            (GxColorSrc)_LightingInfoFlags.ExtractFromRight(9, 1);
        public GxColorSrc AmbientColorSrcForChannel2 =>
            (GxColorSrc)_LightingInfoFlags.ExtractFromRight(10, 1);
        public GxColorSrc MaterialColorSrcForChannel0 =>
            (GxColorSrc)_LightingInfoFlags.ExtractFromRight(11, 1);
        public GxColorSrc MaterialColorSrcForChannel2 =>
            (GxColorSrc)_LightingInfoFlags.ExtractFromRight(12, 1);

        public void Read(BinaryReader reader)
        {
            _Flags = (MaterialFlags)reader.ReadInt32BE();
            _00 = reader.ReadInt32BE();

            _DiffuseColor.Read(reader);

            if (_Flags.HasFlag(MaterialFlags.IsEnabled))
            {
                _TevGroupIndex = reader.ReadInt32BE();
                _DiffuseColor.Read(reader);

                // PVWPolygonColourInfo
                _ColourAnimLength = reader.ReadInt32BE();
                _ColourAnimSpeed = reader.ReadSingleBE();
                _ColourAnimInfo.Read(reader);
                _AlphaAnimInfo.Read(reader);

                // PVWLightingInfo
                _LightingInfoFlags = (uint)reader.ReadInt32BE();
                _04 = reader.ReadSingleBE();

                // PVWPeInfo
                _PEInfo.Read(reader);

                // PVWTextureInfo
                _TextureInfo.Read(reader);
            }
        }
    }

    [Serializable]
    public class PVWTevColReg
    {
        public ColorU16 _Color = new();
        public int _AnimationLength;
        public float _AnimationSpeed;
        public PVWAnimInfo3_S10 _AnimInfo = new();
        public PVWAnimInfo1_S10 _AnimInfo2 = new();

        public void Read(BinaryReader reader)
        {
            _Color.Read(reader);
            _AnimationLength = reader.ReadInt32BE();
            _AnimationSpeed = reader.ReadSingleBE();
            _AnimInfo.Read(reader);
            _AnimInfo2.Read(reader);
        }
    }

    [Serializable]
    public class PVWCombiner
    {
        public byte[] mInputABCD = new byte[4]; // _00

        public byte mOp; // _04
        public byte mBias; // _05
        public byte mScale; // _06
        public byte mClamp; // _07
        public byte mOutReg; // _08

        public void Read(BinaryReader reader)
        {
            mInputABCD[0] = reader.ReadByte();
            mInputABCD[1] = reader.ReadByte();
            mInputABCD[2] = reader.ReadByte();
            mInputABCD[3] = reader.ReadByte();

            mOp = reader.ReadByte();
            mBias = reader.ReadByte();
            mScale = reader.ReadByte();
            mClamp = reader.ReadByte();

            mOutReg = reader.ReadByte();
            reader.ReadByte();
            reader.ReadByte();
            reader.ReadByte();
        }
    }

    [Serializable]
    public class PVWTevStage
    {
        public char[] _TevOrder = new char[4];
        public int[] _KonstSelection = new int[2]; // ([0] is KColor selection, [1] is KAlpha selection)

        PVWCombiner _TevColorCombiner = new();
        PVWCombiner _TevAlphaCombiner = new();

        public void Read(BinaryReader reader)
        {
            _TevOrder[0] = reader.ReadChar();
            _TevOrder[1] = reader.ReadChar();
            _TevOrder[2] = reader.ReadChar();
            _TevOrder[3] = reader.ReadChar();

            _KonstSelection[0] = reader.ReadChar();
            _KonstSelection[1] = reader.ReadChar();

            reader.ReadChar();
            reader.ReadChar();

            _TevColorCombiner.Read(reader);
            _TevAlphaCombiner.Read(reader);
        }
    }

    [Serializable]
    public class PVWTevInfo
    {
        public PVWTevColReg[] _TevColorRegs = new PVWTevColReg[3];
        public ColourU8[] _KonstColors = new ColourU8[4];
        public int _TevStageSize;
        public PVWTevStage[] _TevStageList;

        public void Read(BinaryReader reader)
        {
            for (int i = 0; i < 3; i++)
            {
                _TevColorRegs[i] = new PVWTevColReg();
                _TevColorRegs[i].Read(reader);
            }

            for (int i = 0; i < 4; i++)
            {
                _KonstColors[i] = new ColourU8();
                _KonstColors[i].Read(reader);
            }

            _TevStageSize = reader.ReadInt32BE();
            if (_TevStageSize > 0)
            {
                _TevStageList = new PVWTevStage[_TevStageSize];
                for (int i = 0; i < _TevStageSize; i++)
                {
                    _TevStageList[i] = new PVWTevStage();
                    _TevStageList[i].Read(reader);
                }
            }
        }
    }

    [Serializable]
    public class MaterialPacket
    {
        public int _MaterialCount;
        public int _TevInfoCount;

        public Material[] _Materials;
        public PVWTevInfo[] _TextureInfos;

        public void Read(BinaryReader reader)
        {
            _MaterialCount = reader.ReadInt32BE();
            _TevInfoCount = reader.ReadInt32BE();

            reader.AlignToMultiple(0x20);

            if (_TevInfoCount > 0)
            {
                _TextureInfos = new PVWTevInfo[_TevInfoCount];
                for (int i = 0; i < _TevInfoCount; i++)
                {
                    _TextureInfos[i] = new PVWTevInfo();
                    _TextureInfos[i].Read(reader);
                }
            }

            if (_MaterialCount > 0)
            {
                _Materials = new Material[_MaterialCount];
                for (int i = 0; i < _MaterialCount; i++)
                {
                    _Materials[i] = new Material();
                    _Materials[i].Read(reader);
                }
            }

            reader.AlignToMultiple(0x20);
        }

        public List<UnityEngine.Material> GetUnityMaterials(
            Dictionary<TextureAttributes, Texture2D> texMap,
            List<TextureAttributes> texAttributes
        )
        {
            List<UnityEngine.Material> unityMaterials = new();

            for (int i = 0; i < _MaterialCount; ++i)
            {
                MODFile.Material modMaterial = _Materials[i];
                UnityEngine.Material unityMaterial = CreateBaseMaterial(i);

                SetTexture(unityMaterial, modMaterial, texMap, texAttributes);
                if (modMaterial._PEInfo._Flags.HasFlag(PVWPeInfo.Flags.Enabled))
                {
                    ConfigurePEBlending(unityMaterial, modMaterial);
                }
                else
                {
                    ConfigureStandardBlending(unityMaterial, modMaterial);
                }
                ConfigureLighting(unityMaterial, modMaterial);
                unityMaterial.color = modMaterial._DiffuseColor.ToUnityColor();

                unityMaterials.Add(unityMaterial);
            }

            return unityMaterials;
        }

        private UnityEngine.Material CreateBaseMaterial(int index)
        {
            var material = new UnityEngine.Material(Shader.Find("Standard"))
            {
                name = $"material {index}",
            };

            material.SetFloat("_Metallic", 0);
            material.SetFloat("_Glossiness", 0);

            return material;
        }

        private void ConfigureLighting(UnityEngine.Material material, MODFile.Material modMaterial)
        {
            if (modMaterial.LightingEnabledForChannelControl0)
            {
                // Apply material color from register if specified
                if (modMaterial.MaterialColorSrcForChannel0 == GxColorSrc.Register)
                {
                    material.color = modMaterial._DiffuseColor.ToUnityColor();
                }
            }
            else
            {
                // Disable emission and minimize lighting influence
                material.DisableKeyword("_EMISSION");
                material.SetFloat("_Metallic", 0);
                material.SetFloat("_Glossiness", 0);
            }
        }

        private void SetTexture(
            UnityEngine.Material unityMaterial,
            MODFile.Material modMaterial,
            Dictionary<TextureAttributes, Texture2D> texMap,
            List<TextureAttributes> texAttributes
        )
        {
            TextureInfo modTexInfo = modMaterial._TextureInfo;
            UnityEngine.Texture unityTexture = null;

            if (modTexInfo._TexDataSize > 0)
            {
                TextureData textureInMaterial = modTexInfo._TexData[0];
                int texAttrIndex = textureInMaterial._TexAttrIndex;
                unityTexture = texMap[texAttributes[texAttrIndex]];
            }
            else if (texMap.Count == 1)
            {
                unityTexture = texMap.First().Value;
            }

            if (unityTexture != null)
            {
                unityMaterial.mainTexture = unityTexture;
            }
        }

        private void ConfigurePEBlending(
            UnityEngine.Material material,
            MODFile.Material modMaterial
        )
        {
            ConfigureBlendMode(material, modMaterial);
            ConfigureAlphaCompare(material, modMaterial);
            ConfigureZMode(material, modMaterial);
        }

        private void ConfigureBlendMode(UnityEngine.Material material, MODFile.Material modMaterial)
        {
            switch (modMaterial._PEInfo.mBlendMode.Type)
            {
                case (uint)GXBlendMode.GX_BM_BLEND:
                    SetTransparentBlending(material);
                    break;
                case (uint)GXBlendMode.GX_BM_SUBTRACT:
                    SetSubtractiveBlending(material);
                    break;
                case (uint)GXBlendMode.GX_BM_LOGIC:
                    SetLogicBlending(material, modMaterial);
                    break;
            }
        }

        private void SetTransparentBlending(UnityEngine.Material material)
        {
            material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            SetTransparentRenderingMode(material);
        }

        private void SetSubtractiveBlending(UnityEngine.Material material)
        {
            material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
            material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcColor);
            SetTransparentRenderingMode(material);
        }

        private void SetTransparentRenderingMode(UnityEngine.Material material)
        {
            material.SetOverrideTag("RenderType", "Transparent");
            material.EnableKeyword("_ALPHABLEND_ON");
            material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            material.DisableKeyword("_ALPHATEST_ON");
            material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
        }

        private void SetLogicBlending(UnityEngine.Material material, MODFile.Material modMaterial)
        {
            var (srcBlend, dstBlend) = GetLogicBlendModes(modMaterial);
            material.SetInt("_SrcBlend", srcBlend);
            material.SetInt("_DstBlend", dstBlend);
        }

        private (int srcBlend, int dstBlend) GetLogicBlendModes(MODFile.Material modMaterial)
        {
            return (GXLogicOp)modMaterial._PEInfo.mBlendMode.LogicOp switch
            {
                GXLogicOp.GX_LO_CLEAR => (
                    (int)UnityEngine.Rendering.BlendMode.Zero,
                    (int)UnityEngine.Rendering.BlendMode.Zero
                ),
                GXLogicOp.GX_LO_AND => (
                    (int)UnityEngine.Rendering.BlendMode.Zero,
                    (int)UnityEngine.Rendering.BlendMode.SrcColor
                ),
                GXLogicOp.GX_LO_REVAND => (
                    (int)UnityEngine.Rendering.BlendMode.Zero,
                    (int)UnityEngine.Rendering.BlendMode.OneMinusSrcColor
                ),
                GXLogicOp.GX_LO_COPY => (
                    (int)UnityEngine.Rendering.BlendMode.One,
                    (int)UnityEngine.Rendering.BlendMode.Zero
                ),
                GXLogicOp.GX_LO_INVAND => (
                    (int)UnityEngine.Rendering.BlendMode.One,
                    (int)UnityEngine.Rendering.BlendMode.OneMinusSrcColor
                ),
                GXLogicOp.GX_LO_NOOP => (
                    (int)UnityEngine.Rendering.BlendMode.One,
                    (int)UnityEngine.Rendering.BlendMode.Zero
                ),
                GXLogicOp.GX_LO_XOR => (
                    (int)UnityEngine.Rendering.BlendMode.OneMinusSrcColor,
                    (int)UnityEngine.Rendering.BlendMode.One
                ),
                GXLogicOp.GX_LO_OR => (
                    (int)UnityEngine.Rendering.BlendMode.One,
                    (int)UnityEngine.Rendering.BlendMode.One
                ),
                GXLogicOp.GX_LO_NOR => (
                    (int)UnityEngine.Rendering.BlendMode.OneMinusSrcColor,
                    (int)UnityEngine.Rendering.BlendMode.OneMinusSrcColor
                ),
                GXLogicOp.GX_LO_EQUIV => (
                    (int)UnityEngine.Rendering.BlendMode.OneMinusDstColor,
                    (int)UnityEngine.Rendering.BlendMode.One
                ),
                GXLogicOp.GX_LO_INV => (
                    (int)UnityEngine.Rendering.BlendMode.OneMinusDstColor,
                    (int)UnityEngine.Rendering.BlendMode.Zero
                ),
                GXLogicOp.GX_LO_REVOR => (
                    (int)UnityEngine.Rendering.BlendMode.OneMinusDstColor,
                    (int)UnityEngine.Rendering.BlendMode.OneMinusSrcColor
                ),
                GXLogicOp.GX_LO_INVCOPY => (
                    (int)UnityEngine.Rendering.BlendMode.OneMinusSrcColor,
                    (int)UnityEngine.Rendering.BlendMode.Zero
                ),
                GXLogicOp.GX_LO_INVOR => (
                    (int)UnityEngine.Rendering.BlendMode.OneMinusDstColor,
                    (int)UnityEngine.Rendering.BlendMode.One
                ),
                GXLogicOp.GX_LO_NAND => (
                    (int)UnityEngine.Rendering.BlendMode.OneMinusDstColor,
                    (int)UnityEngine.Rendering.BlendMode.OneMinusSrcColor
                ),
                GXLogicOp.GX_LO_SET => (
                    (int)UnityEngine.Rendering.BlendMode.One,
                    (int)UnityEngine.Rendering.BlendMode.One
                ),
                _ => (
                    (int)UnityEngine.Rendering.BlendMode.One,
                    (int)UnityEngine.Rendering.BlendMode.Zero
                ),
            };
        }

        private void ConfigureAlphaCompare(
            UnityEngine.Material material,
            MODFile.Material modMaterial
        )
        {
            var compareFunction = (GXCompare)modMaterial._PEInfo.mAlphaCompareFunction.Op;
            var unityCompareFunction = GetUnityCompareFunction(compareFunction);
            material.SetInt("_AlphaTest", (int)unityCompareFunction);
        }

        private void ConfigureZMode(UnityEngine.Material material, MODFile.Material modMaterial)
        {
            bool compareEnabled = (modMaterial._PEInfo.mZModeFunction & 1) != 0;
            bool updateEnabled = (modMaterial._PEInfo.mZModeFunction & 2) != 0;

            material.SetInt(
                "_ZTest",
                compareEnabled
                    ? (int)UnityEngine.Rendering.CompareFunction.LessEqual
                    : (int)UnityEngine.Rendering.CompareFunction.Always
            );

            material.SetInt(
                "_ZWrite",
                updateEnabled
                    ? (int)UnityEngine.Rendering.CompareFunction.Always
                    : (int)UnityEngine.Rendering.CompareFunction.Never
            );

            GXCompare depthFunction = (GXCompare)((modMaterial._PEInfo.mZModeFunction >> 2) & 0x3);
            material.SetInt("_ZTest", (int)GetUnityCompareFunction(depthFunction));
        }

        private UnityEngine.Rendering.CompareFunction GetUnityCompareFunction(GXCompare gxCompare)
        {
            return gxCompare switch
            {
                GXCompare.GX_NEVER => UnityEngine.Rendering.CompareFunction.Never,
                GXCompare.GX_LESS => UnityEngine.Rendering.CompareFunction.Less,
                GXCompare.GX_EQUAL => UnityEngine.Rendering.CompareFunction.Equal,
                GXCompare.GX_LEQUAL => UnityEngine.Rendering.CompareFunction.LessEqual,
                GXCompare.GX_GREATER => UnityEngine.Rendering.CompareFunction.Greater,
                GXCompare.GX_NEQUAL => UnityEngine.Rendering.CompareFunction.NotEqual,
                GXCompare.GX_GEQUAL => UnityEngine.Rendering.CompareFunction.GreaterEqual,
                GXCompare.GX_ALWAYS => UnityEngine.Rendering.CompareFunction.Always,
                _ => UnityEngine.Rendering.CompareFunction.Always,
            };
        }

        private void ConfigureStandardBlending(
            UnityEngine.Material material,
            MODFile.Material modMaterial
        )
        {
            if (modMaterial._Flags.HasFlag(MaterialFlags.Opaque))
            {
                SetOpaqueBlending(material);
            }
            else if (modMaterial._Flags.HasFlag(MaterialFlags.AlphaClip))
            {
                SetAlphaClipBlending(material);
            }
            else if (modMaterial._Flags.HasFlag(MaterialFlags.TransparentBlend))
            {
                SetTransparentBlending(material);
            }
            else if (modMaterial._Flags.HasFlag(MaterialFlags.InvertSpecialBlend))
            {
                SetInvertSpecialBlending(material);
            }
        }

        private void SetMaterialProperties(
            UnityEngine.Material material,
            float mode,
            UnityEngine.Rendering.BlendMode srcBlend,
            UnityEngine.Rendering.BlendMode dstBlend,
            bool zWrite,
            bool isAlphaTest,
            bool isTransparent,
            float cutoff = 0
        )
        {
            material.SetFloat("_Mode", mode);
            material.SetInt("_SrcBlend", (int)srcBlend);
            material.SetInt("_DstBlend", (int)dstBlend);
            material.SetInt("_ZWrite", zWrite ? 1 : 0);

            if (cutoff > 0)
                material.SetFloat("_Cutoff", cutoff);

            if (isAlphaTest)
                material.EnableKeyword("_ALPHATEST_ON");
            else
                material.DisableKeyword("_ALPHATEST_ON");

            if (isTransparent)
            {
                material.EnableKeyword("_ALPHABLEND_ON");
                material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
            }
            else
            {
                material.DisableKeyword("_ALPHABLEND_ON");
                material.renderQueue = isAlphaTest
                    ? (int)UnityEngine.Rendering.RenderQueue.AlphaTest
                    : (int)UnityEngine.Rendering.RenderQueue.Geometry;
            }
            material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
        }

        private void SetOpaqueBlending(UnityEngine.Material material) =>
            SetMaterialProperties(
                material,
                0,
                UnityEngine.Rendering.BlendMode.One,
                UnityEngine.Rendering.BlendMode.Zero,
                true,
                false,
                false
            );

        private void SetAlphaClipBlending(UnityEngine.Material material) =>
            SetMaterialProperties(
                material,
                1,
                UnityEngine.Rendering.BlendMode.One,
                UnityEngine.Rendering.BlendMode.Zero,
                true,
                true,
                false,
                0.5f
            );

        private void SetInvertSpecialBlending(UnityEngine.Material material) =>
            SetMaterialProperties(
                material,
                3,
                UnityEngine.Rendering.BlendMode.Zero,
                UnityEngine.Rendering.BlendMode.OneMinusSrcColor,
                false,
                false,
                true
            );
    }
}
