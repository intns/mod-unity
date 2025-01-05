using System;
using System.IO;

namespace MODFile
{
    [Serializable]
    public class ColourU8 : IReadable
    {
        public byte R;
        public byte G;
        public byte B;
        public byte A;

        public void Read(BinaryReader reader)
        {
            R = reader.ReadByte();
            G = reader.ReadByte();
            B = reader.ReadByte();
            A = reader.ReadByte();
        }

        public UnityEngine.Color ToUnityColor()
        {
            return new UnityEngine.Color(R / 255f, G / 255f, B / 255f, A / 255f);
        }

        public UnityEngine.Color32 ToUnityColor32()
        {
            return new UnityEngine.Color32(R, G, B, A);
        }

        public override string ToString()
        {
            return $"R: {R}, G: {G}, B: {B}, A: {A}";
        }

        public static ColourU8 FromColor32(UnityEngine.Color32 color)
        {
            return new ColourU8
            {
                R = color.r,
                G = color.g,
                B = color.b,
                A = color.a
            };
        }

        public static ColourU8 FromColor(UnityEngine.Color color)
        {
            return new ColourU8
            {
                R = (byte)(color.r * 255),
                G = (byte)(color.g * 255),
                B = (byte)(color.b * 255),
                A = (byte)(color.a * 255)
            };
        }
    }

    [Serializable]
    public class ColorU16 : IReadable
    {
        public ushort R;
        public ushort G;
        public ushort B;
        public ushort A;

        public void Read(BinaryReader reader)
        {
            R = (ushort)reader.ReadInt16BE();
            G = (ushort)reader.ReadInt16BE();
            B = (ushort)reader.ReadInt16BE();
            A = (ushort)reader.ReadInt16BE();
        }

        public UnityEngine.Color ToUnityColor()
        {
            return new UnityEngine.Color(R / 65535f, G / 65535f, B / 65535f, A / 65535f);
        }

        public UnityEngine.Color32 ToUnityColor32()
        {
            return new UnityEngine.Color32(
                (byte)(R / 256),
                (byte)(G / 256),
                (byte)(B / 256),
                (byte)(A / 256)
            );
        }

        public override string ToString()
        {
            return $"R: {R}, G: {G}, B: {B}, A: {A}";
        }

        public static ColorU16 FromColor32(UnityEngine.Color32 color)
        {
            return new ColorU16
            {
                R = (ushort)(color.r * 256),
                G = (ushort)(color.g * 256),
                B = (ushort)(color.b * 256),
                A = (ushort)(color.a * 256)
            };
        }

        public static ColorU16 FromColor(UnityEngine.Color color)
        {
            return new ColorU16
            {
                R = (ushort)(color.r * 65535),
                G = (ushort)(color.g * 65535),
                B = (ushort)(color.b * 65535),
                A = (ushort)(color.a * 65535)
            };
        }
    }
}
