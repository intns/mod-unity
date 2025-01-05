using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace MODFile
{
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
        public float _00;
        public float _04;
        public float _08;

        public void Read(BinaryReader reader)
        {
            _00 = reader.ReadSingleBE();
            _04 = reader.ReadSingleBE();
            _08 = reader.ReadSingleBE();
        }
    }

    public class KeyInfoS10
    {
        public short _00;
        public float _04;
        public float _08;

        public void Read(BinaryReader reader)
        {
            _00 = reader.ReadInt16BE();
            reader.ReadInt16BE();

            _04 = reader.ReadSingleBE();
            _08 = reader.ReadSingleBE();
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
        GX_BM_SUBTRACT = 3
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
        GX_LO_SET
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
        GX_ALWAYS = 7
    };

    [Serializable]
    public class PVWPeInfo
    {
        [Serializable, Flags]
        public enum Flags
        {
            Enabled = 1
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

                UnityEngine.Material unityMaterial =
                    new(Shader.Find("Standard")) { name = $"material {i}" };

                if (unityTexture != null)
                {
                    unityMaterial.mainTexture = unityTexture;

                    // Set smoothness and metallic to 0
                    unityMaterial.SetFloat("_Metallic", 0);
                    unityMaterial.SetFloat("_Glossiness", 0);

                    if (modMaterial._PEInfo._Flags.HasFlag(PVWPeInfo.Flags.Enabled))
                    {
                        // Set blend mode
                        switch (modMaterial._PEInfo.mBlendMode.Type)
                        {
                            case (uint)GXBlendMode.GX_BM_BLEND:
                                unityMaterial.SetInt(
                                    "_SrcBlend",
                                    (int)UnityEngine.Rendering.BlendMode.SrcAlpha
                                );
                                unityMaterial.SetInt(
                                    "_DstBlend",
                                    (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha
                                );
                                unityMaterial.EnableKeyword("_ALPHABLEND_ON");
                                unityMaterial.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                                unityMaterial.DisableKeyword("_ALPHATEST_ON");
                                unityMaterial.renderQueue = (int)
                                    UnityEngine.Rendering.RenderQueue.Transparent;
                                break;
                            case (uint)GXBlendMode.GX_BM_SUBTRACT:
                                // dst_pix_clr = dst_pix_clr - src_pix_clr [clamped to zero]
                                unityMaterial.SetInt(
                                    "_SrcBlend",
                                    (int)UnityEngine.Rendering.BlendMode.One
                                );
                                unityMaterial.SetInt(
                                    "_DstBlend",
                                    (int)UnityEngine.Rendering.BlendMode.OneMinusSrcColor
                                );
                                unityMaterial.EnableKeyword("_ALPHABLEND_ON");
                                unityMaterial.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                                unityMaterial.DisableKeyword("_ALPHATEST_ON");
                                unityMaterial.renderQueue = (int)
                                    UnityEngine.Rendering.RenderQueue.Transparent;
                                break;
                            case (uint)GXBlendMode.GX_BM_LOGIC:
                                int srcBlend;
                                int dstBlend;
                                switch ((GXLogicOp)modMaterial._PEInfo.mBlendMode.LogicOp)
                                {
                                    case GXLogicOp.GX_LO_CLEAR:
                                        srcBlend = (int)UnityEngine.Rendering.BlendMode.Zero;
                                        dstBlend = (int)UnityEngine.Rendering.BlendMode.Zero;
                                        break;
                                    case GXLogicOp.GX_LO_AND:
                                        srcBlend = (int)UnityEngine.Rendering.BlendMode.Zero;
                                        dstBlend = (int)UnityEngine.Rendering.BlendMode.SrcColor;
                                        break;
                                    case GXLogicOp.GX_LO_REVAND:
                                        srcBlend = (int)UnityEngine.Rendering.BlendMode.Zero;
                                        dstBlend = (int)
                                            UnityEngine.Rendering.BlendMode.OneMinusSrcColor;
                                        break;
                                    case GXLogicOp.GX_LO_COPY:
                                        srcBlend = (int)UnityEngine.Rendering.BlendMode.One;
                                        dstBlend = (int)UnityEngine.Rendering.BlendMode.Zero;
                                        break;
                                    case GXLogicOp.GX_LO_INVAND:
                                        srcBlend = (int)UnityEngine.Rendering.BlendMode.One;
                                        dstBlend = (int)
                                            UnityEngine.Rendering.BlendMode.OneMinusSrcColor;
                                        break;

                                    default:
                                    case GXLogicOp.GX_LO_NOOP:
                                        srcBlend = (int)UnityEngine.Rendering.BlendMode.One;
                                        dstBlend = (int)UnityEngine.Rendering.BlendMode.Zero;
                                        break;
                                    case GXLogicOp.GX_LO_XOR:
                                        srcBlend = (int)
                                            UnityEngine.Rendering.BlendMode.OneMinusSrcColor;
                                        dstBlend = (int)UnityEngine.Rendering.BlendMode.One;
                                        break;
                                    case GXLogicOp.GX_LO_OR:
                                        srcBlend = (int)UnityEngine.Rendering.BlendMode.One;
                                        dstBlend = (int)UnityEngine.Rendering.BlendMode.One;
                                        break;
                                    case GXLogicOp.GX_LO_NOR:
                                        srcBlend = (int)
                                            UnityEngine.Rendering.BlendMode.OneMinusSrcColor;
                                        dstBlend = (int)
                                            UnityEngine.Rendering.BlendMode.OneMinusSrcColor;
                                        break;
                                    case GXLogicOp.GX_LO_EQUIV:
                                        srcBlend = (int)
                                            UnityEngine.Rendering.BlendMode.OneMinusDstColor;
                                        dstBlend = (int)UnityEngine.Rendering.BlendMode.One;
                                        break;
                                    case GXLogicOp.GX_LO_INV:
                                        srcBlend = (int)
                                            UnityEngine.Rendering.BlendMode.OneMinusDstColor;
                                        dstBlend = (int)UnityEngine.Rendering.BlendMode.Zero;
                                        break;
                                    case GXLogicOp.GX_LO_REVOR:
                                        srcBlend = (int)
                                            UnityEngine.Rendering.BlendMode.OneMinusDstColor;
                                        dstBlend = (int)
                                            UnityEngine.Rendering.BlendMode.OneMinusSrcColor;
                                        break;
                                    case GXLogicOp.GX_LO_INVCOPY:
                                        srcBlend = (int)
                                            UnityEngine.Rendering.BlendMode.OneMinusSrcColor;
                                        dstBlend = (int)UnityEngine.Rendering.BlendMode.Zero;
                                        break;
                                    case GXLogicOp.GX_LO_INVOR:
                                        srcBlend = (int)
                                            UnityEngine.Rendering.BlendMode.OneMinusDstColor;
                                        dstBlend = (int)UnityEngine.Rendering.BlendMode.One;
                                        break;
                                    case GXLogicOp.GX_LO_NAND:
                                        srcBlend = (int)
                                            UnityEngine.Rendering.BlendMode.OneMinusDstColor;
                                        dstBlend = (int)
                                            UnityEngine.Rendering.BlendMode.OneMinusSrcColor;
                                        break;
                                    case GXLogicOp.GX_LO_SET:
                                        srcBlend = (int)UnityEngine.Rendering.BlendMode.One;
                                        dstBlend = (int)UnityEngine.Rendering.BlendMode.One;
                                        break;
                                }

                                unityMaterial.SetInt("_SrcBlend", srcBlend);
                                unityMaterial.SetInt("_DstBlend", dstBlend);

                                break;
                        }

                        // Set Alpha compare
                        switch ((GXCompare)modMaterial._PEInfo.mAlphaCompareFunction.Op)
                        {
                            case GXCompare.GX_NEVER:
                                unityMaterial.SetInt(
                                    "_AlphaTest",
                                    (int)UnityEngine.Rendering.CompareFunction.Never
                                );
                                break;
                            case GXCompare.GX_LESS:
                                unityMaterial.SetInt(
                                    "_AlphaTest",
                                    (int)UnityEngine.Rendering.CompareFunction.Less
                                );
                                break;
                            case GXCompare.GX_EQUAL:
                                unityMaterial.SetInt(
                                    "_AlphaTest",
                                    (int)UnityEngine.Rendering.CompareFunction.Equal
                                );
                                break;
                            case GXCompare.GX_LEQUAL:
                                unityMaterial.SetInt(
                                    "_AlphaTest",
                                    (int)UnityEngine.Rendering.CompareFunction.LessEqual
                                );
                                break;
                            case GXCompare.GX_GREATER:
                                unityMaterial.SetInt(
                                    "_AlphaTest",
                                    (int)UnityEngine.Rendering.CompareFunction.Greater
                                );
                                break;
                            case GXCompare.GX_NEQUAL:
                                unityMaterial.SetInt(
                                    "_AlphaTest",
                                    (int)UnityEngine.Rendering.CompareFunction.NotEqual
                                );
                                break;
                            case GXCompare.GX_GEQUAL:
                                unityMaterial.SetInt(
                                    "_AlphaTest",
                                    (int)UnityEngine.Rendering.CompareFunction.GreaterEqual
                                );
                                break;
                            case GXCompare.GX_ALWAYS:
                                unityMaterial.SetInt(
                                    "_AlphaTest",
                                    (int)UnityEngine.Rendering.CompareFunction.Always
                                );
                                break;
                        }

                        // Set Z Mode
                        bool compare_enabled = (modMaterial._PEInfo.mZModeFunction & 1) != 0;
                        bool update_enabled = (modMaterial._PEInfo.mZModeFunction & 2) != 0;
                        if (compare_enabled)
                        {
                            unityMaterial.SetInt(
                                "_ZTest",
                                (int)UnityEngine.Rendering.CompareFunction.LessEqual
                            );
                        }
                        else
                        {
                            unityMaterial.SetInt(
                                "_ZTest",
                                (int)UnityEngine.Rendering.CompareFunction.Always
                            );
                        }

                        if (update_enabled)
                        {
                            unityMaterial.SetInt(
                                "_ZWrite",
                                (int)UnityEngine.Rendering.CompareFunction.Always
                            );
                        }
                        else
                        {
                            unityMaterial.SetInt(
                                "_ZWrite",
                                (int)UnityEngine.Rendering.CompareFunction.Never
                            );
                        }

                        // 0x00-00-DD-00
                        // where DD is the depth function
                        GXCompare depthFunction = (GXCompare)(
                            (modMaterial._PEInfo.mZModeFunction >> 2) & 0x3
                        );
                        // Set the ZMode compare function
                        switch (depthFunction)
                        {
                            case GXCompare.GX_NEVER:
                                unityMaterial.SetInt(
                                    "_ZTest",
                                    (int)UnityEngine.Rendering.CompareFunction.Never
                                );
                                break;
                            case GXCompare.GX_LESS:
                                unityMaterial.SetInt(
                                    "_ZTest",
                                    (int)UnityEngine.Rendering.CompareFunction.Less
                                );
                                break;
                            case GXCompare.GX_EQUAL:
                                unityMaterial.SetInt(
                                    "_ZTest",
                                    (int)UnityEngine.Rendering.CompareFunction.Equal
                                );
                                break;
                            case GXCompare.GX_LEQUAL:
                                unityMaterial.SetInt(
                                    "_ZTest",
                                    (int)UnityEngine.Rendering.CompareFunction.LessEqual
                                );
                                break;
                            case GXCompare.GX_GREATER:
                                unityMaterial.SetInt(
                                    "_ZTest",
                                    (int)UnityEngine.Rendering.CompareFunction.Greater
                                );
                                break;
                            case GXCompare.GX_NEQUAL:
                                unityMaterial.SetInt(
                                    "_ZTest",
                                    (int)UnityEngine.Rendering.CompareFunction.NotEqual
                                );
                                break;
                            case GXCompare.GX_GEQUAL:
                                unityMaterial.SetInt(
                                    "_ZTest",
                                    (int)UnityEngine.Rendering.CompareFunction.GreaterEqual
                                );
                                break;
                            case GXCompare.GX_ALWAYS:
                                unityMaterial.SetInt(
                                    "_ZTest",
                                    (int)UnityEngine.Rendering.CompareFunction.Always
                                );
                                break;
                        }
                    }
                    else
                    {
                        if (modMaterial._Flags.HasFlag(MaterialFlags.Opaque))
                        {
                            unityMaterial.SetFloat("_Mode", 0);
                            unityMaterial.SetInt(
                                "_SrcBlend",
                                (int)UnityEngine.Rendering.BlendMode.One
                            );
                            unityMaterial.SetInt(
                                "_DstBlend",
                                (int)UnityEngine.Rendering.BlendMode.Zero
                            );
                            unityMaterial.SetInt("_ZWrite", 1);
                            unityMaterial.DisableKeyword("_ALPHATEST_ON");
                            unityMaterial.DisableKeyword("_ALPHABLEND_ON");
                            unityMaterial.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                            unityMaterial.renderQueue = -1;
                        }
                        else if (modMaterial._Flags.HasFlag(MaterialFlags.AlphaClip))
                        {
                            unityMaterial.SetFloat("_Mode", 1);
                            unityMaterial.SetInt(
                                "_SrcBlend",
                                (int)UnityEngine.Rendering.BlendMode.One
                            );
                            unityMaterial.SetInt(
                                "_DstBlend",
                                (int)UnityEngine.Rendering.BlendMode.Zero
                            );
                            unityMaterial.SetFloat("_Cutoff", 0.5f);
                            unityMaterial.SetInt("_ZWrite", 1);
                            unityMaterial.EnableKeyword("_ALPHATEST_ON");
                            unityMaterial.DisableKeyword("_ALPHABLEND_ON");
                            unityMaterial.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                            unityMaterial.renderQueue = (int)
                                UnityEngine.Rendering.RenderQueue.AlphaTest;
                        }
                        else if (modMaterial._Flags.HasFlag(MaterialFlags.TransparentBlend))
                        {
                            unityMaterial.SetFloat("_Mode", 3);
                            unityMaterial.SetInt(
                                "_SrcBlend",
                                (int)UnityEngine.Rendering.BlendMode.SrcAlpha
                            );
                            unityMaterial.SetInt(
                                "_DstBlend",
                                (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha
                            );
                            unityMaterial.SetInt("_ZWrite", 0);
                            unityMaterial.DisableKeyword("_ALPHATEST_ON");
                            unityMaterial.EnableKeyword("_ALPHABLEND_ON");
                            unityMaterial.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                            unityMaterial.renderQueue = (int)
                                UnityEngine.Rendering.RenderQueue.Transparent;
                        }
                        else if (modMaterial._Flags.HasFlag(MaterialFlags.InvertSpecialBlend))
                        {
                            unityMaterial.SetFloat("_Mode", 3);
                            unityMaterial.SetInt(
                                "_SrcBlend",
                                (int)UnityEngine.Rendering.BlendMode.Zero
                            );
                            unityMaterial.SetInt(
                                "_DstBlend",
                                (int)UnityEngine.Rendering.BlendMode.OneMinusSrcColor
                            );
                            unityMaterial.SetInt("_ZWrite", 0);
                            unityMaterial.DisableKeyword("_ALPHATEST_ON");
                            unityMaterial.EnableKeyword("_ALPHABLEND_ON");
                            unityMaterial.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                            unityMaterial.renderQueue = (int)
                                UnityEngine.Rendering.RenderQueue.Transparent;
                        }
                    }
                }

                unityMaterial.color = modMaterial._DiffuseColor.ToUnityColor();

                unityMaterials.Add(unityMaterial);
            }

            return unityMaterials;
        }
    }
}
