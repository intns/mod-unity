using System;
using System.Collections.Generic;
using System.IO;
using LibGC.Texture;
using UnityEngine;

namespace MODFile
{
    [Serializable]
    public enum TextureFormat
    {
        RGB565 = 0,
        CMPR = 1,
        RGB5A3 = 2,
        I4 = 3,
        I8 = 4,
        IA4 = 5,
        IA8 = 6,
        RGBA32 = 7,
    };

    [Serializable, Flags]
    public enum TextureTilingFlags
    {
        Repeat = 0,
        WrapS_UseClamp = 1, // If not set, use repeat
        WrapT_UseClamp =
            0x100 // If not set, use repeat
        ,
    };

    [Serializable]
    public class TextureCoordinatePair
    {
        public int Channel;
        public List<Vector2Readable> Coordinate;
    }

    [Serializable]
    public class TextureCoordinate
    {
        public List<TextureCoordinatePair> TexCoords = new();

        public void Read(BinaryReader reader, int index)
        {
            int count = reader.ReadInt32BE();
            reader.AlignToMultiple(0x20);

            List<Vector2Readable> texCoordList = new();
            for (int i = 0; i < count; i++)
            {
                Vector2Readable texCoord = new();
                texCoord.Read(reader);
                texCoordList.Add(texCoord);
            }

            TextureCoordinatePair pair = new() { Channel = index, Coordinate = texCoordList };
            TexCoords.Add(pair);

            reader.AlignToMultiple(0x20);
        }

        public Vector2 GetTexCoord(int channel, int index)
        {
            return (Vector2)TexCoords[channel].Coordinate[index];
        }

        public void SetTexCoord(int channel, int index, Vector2 texCoord)
        {
            TexCoords[channel].Coordinate[index] = texCoord;
        }

        public void AddTexCoord(int channel, Vector2 texCoord)
        {
            while (TexCoords.Count <= channel)
            {
                TexCoords.Add(
                    new() { Channel = TexCoords.Count, Coordinate = new List<Vector2Readable>() }
                );
            }
            TexCoords[channel].Coordinate.Add(texCoord);
        }
    }

    [Serializable]
    public class TextureAttributes : IReadable
    {
        public ushort Index;
        public TextureTilingFlags WrapFlags;
        public short ForceTextureUseAttribute;

        public TextureWrapMode ModeS =>
            WrapFlags.HasFlag(TextureTilingFlags.WrapS_UseClamp)
                ? TextureWrapMode.Clamp
                : TextureWrapMode.Repeat;

        public TextureWrapMode ModeT =>
            WrapFlags.HasFlag(TextureTilingFlags.WrapT_UseClamp)
                ? TextureWrapMode.Clamp
                : TextureWrapMode.Repeat;
        public float Width;

        public void Read(BinaryReader reader)
        {
            Index = (ushort)reader.ReadInt16BE();
            reader.ReadInt16BE();

            WrapFlags = (TextureTilingFlags)reader.ReadInt16BE();
            ForceTextureUseAttribute = reader.ReadInt16BE();

            Width = reader.ReadSingleBE();
        }
    }

    [Serializable]
    public class Texture : IReadable
    {
        public ushort Width;
        public ushort Height;
        public TextureFormat Format;

        public byte[] Data;

        public void Read(BinaryReader reader)
        {
            Width = (ushort)reader.ReadInt16BE();
            Height = (ushort)reader.ReadInt16BE();
            Format = (TextureFormat)reader.ReadInt32BE();

            for (int i = 0; i < 5; i++)
            {
                reader.ReadInt32BE();
            }

            int dataSize = reader.ReadInt32BE();
            Data = reader.ReadBytes(dataSize);
        }

        public Texture2D ConvertToUnityTexture(TextureAttributes textureAttr)
        {
            GcTextureFormatCodec tex;
            switch (Format)
            {
                case TextureFormat.RGB565:
                    tex = new GcTextureFormatCodecRGB565();
                    break;
                case TextureFormat.CMPR:
                    tex = new GcTextureFormatCodecCMPR();
                    break;
                case TextureFormat.RGB5A3:
                    tex = new GcTextureFormatCodecRGB5A3();
                    break;
                case TextureFormat.I4:
                    tex = new GcTextureFormatCodecI4();
                    break;
                case TextureFormat.I8:
                    tex = new GcTextureFormatCodecI8();
                    break;
                case TextureFormat.IA4:
                    tex = new GcTextureFormatCodecIA4();
                    break;
                case TextureFormat.IA8:
                    tex = new GcTextureFormatCodecIA8();
                    break;
                case TextureFormat.RGBA32:
                    tex = new GcTextureFormatCodecRGBA8();
                    break;
                default:
                    Debug.LogError("Unknown texture format: " + Format);
                    return null;
            }

            Texture2D newTex = new(Width, Height, UnityEngine.TextureFormat.RGBA32, false);

            byte[] destData = new byte[Width * Height * 4];
            tex.DecodeTexture(destData, 0, Width, Height, Width * 4, Data, 0, null, 0);

            newTex.LoadRawTextureData(destData);
            newTex.wrapModeU = textureAttr.ModeS;
            newTex.wrapModeV = textureAttr.ModeT;
            newTex.alphaIsTransparency = true;
            newTex.Apply();
            return newTex;
        }
    }
}
