using System;
using System.Collections;
using System.Collections.Generic;

namespace mod.schema
{
    public enum GxOpcode : byte
    {
        Nop = 0x0,
        TriangleStrip = 0x98,
        TriangleFan = 0xA0,
    }

    public enum GxAttributeType
    {
        None = 0,
        Direct,
        Index8,
        Index16,
    }

    public enum GxAttribute : uint
    {
        PNMTXIDX = 0,
        TEX0MTXIDX = 1,
        TEX1MTXIDX = 2,
        TEX2MTXIDX = 3,
        TEX3MTXIDX = 4,
        TEX4MTXIDX = 5,
        TEX5MTXIDX = 6,
        TEX6MTXIDX = 7,
        TEX7MTXIDX = 8,
        POS = 9,
        NRM = 10,
        CLR0 = 11,
        CLR1 = 12,
        TEX0 = 13,
        TEX1 = 14,
        TEX2 = 15,
        TEX3 = 16,
        TEX4 = 17,
        TEX5 = 18,
        TEX6 = 19,
        TEX7 = 20,
        POS_MTX_ARRAY = 21,
        NRM_MTX_ARRAY = 22,
        TEX_MTX_ARRAY = 23,
        LIGHT_ARRAY = 24,
        NBT = 25,
        MAX = NBT,
        NULL = 0xFF,
    }

    public class VertexDescriptor : IEnumerable<(GxAttribute, GxAttributeType?)>
    {
        public bool _IsPositionMatrix = false;

        public bool[] _TextureMatrix = { false, false, false, false, false, false, false, false, };

        public GxAttributeType _Position = GxAttributeType.None;

        public GxAttributeType _Normal = GxAttributeType.None;
        public bool _NBTEnabled;

        public GxAttributeType _Color0 = GxAttributeType.None;
        public GxAttributeType _Color1 = GxAttributeType.None;

        public readonly GxAttributeType[] _Texcoord = new[]
        {
            GxAttributeType.None,
            GxAttributeType.None,
            GxAttributeType.None,
            GxAttributeType.None,
            GxAttributeType.None,
            GxAttributeType.None,
            GxAttributeType.None,
            GxAttributeType.None,
        };

        public IEnumerator<(GxAttribute, GxAttributeType?)> GetEnumerator() => ActiveAttributes();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public IEnumerator<(GxAttribute, GxAttributeType?)> ActiveAttributes()
        {
            foreach (object attr in Enum.GetValues(typeof(GxAttribute)))
            {
                if (!Exists((GxAttribute)attr))
                {
                    continue;
                }

                if (attr is >= GxAttribute.POS and <= GxAttribute.TEX7)
                {
                    yield return ((GxAttribute)attr, GetFormat((GxAttribute)attr));
                }
                else
                {
                    yield return ((GxAttribute)attr, null);
                }
            }
        }

        public bool Exists(GxAttribute enumval)
        {
            switch (enumval)
            {
                case GxAttribute.NULL:
                    return false;

                case >= GxAttribute.TEX0MTXIDX
                and <= GxAttribute.TEX7MTXIDX:
                {
                    uint texMatId = enumval - GxAttribute.TEX0MTXIDX;
                    return _TextureMatrix[texMatId];
                }

                case >= GxAttribute.TEX0
                and <= GxAttribute.TEX7:
                {
                    uint texCoordId = enumval - GxAttribute.TEX0;
                    return _Texcoord[texCoordId] != GxAttributeType.None;
                }

                case GxAttribute.PNMTXIDX:
                    return _IsPositionMatrix;
                case GxAttribute.POS:
                    return _Position != GxAttributeType.None;
                case GxAttribute.NRM:
                    return _Normal != GxAttributeType.None;
                case GxAttribute.CLR0:
                    return _Color0 != GxAttributeType.None;
                case GxAttribute.CLR1:
                    return _Color1 != GxAttributeType.None;
                case >= GxAttribute.POS_MTX_ARRAY:
                    return false;
            }
        }

        public GxAttributeType GetFormat(GxAttribute enumval)
        {
            switch (enumval)
            {
                case GxAttribute.POS:
                    return _Position;
                case GxAttribute.NRM:
                    return _Normal;
                case GxAttribute.CLR0:
                    return _Color0;
                case GxAttribute.CLR1:
                    return _Color1;
                case >= GxAttribute.TEX0
                and <= GxAttribute.TEX7:
                {
                    uint texcoordid = enumval - GxAttribute.TEX0;
                    return _Texcoord[texcoordid];
                }

                default:
                    return GxAttributeType.None;
            }
        }

        public void FromPikmin1(uint attributeFlags, bool hasNormals = false)
        {
            // Position is always enabled
            _Position = GxAttributeType.Index16;

            // Process attribute flags
            for (int i = 0; i < 10; ++i)
            {
                bool isAttributeEnabled = (attributeFlags & 0b1) == 1;
                attributeFlags >>= 1;

                switch (i)
                {
                    case 0: // Position
                        _IsPositionMatrix = isAttributeEnabled;
                        break;
                    case 1: // Texture matrix
                        _TextureMatrix[1] = isAttributeEnabled;
                        break;
                    case 2: // Color 0 (vertex color)
                        if (isAttributeEnabled)
                        {
                            _Color0 = GxAttributeType.Index16;
                        }
                        break;
                    default:
                        // Texture coordinates
                        if (isAttributeEnabled)
                        {
                            _Texcoord[i - 3] = GxAttributeType.Index16;
                        }
                        break;
                }
            }

            _NBTEnabled = (attributeFlags & 0x20) != 0;

            if (hasNormals)
            {
                _Normal = GxAttributeType.Index16;
            }
        }
    }
}
