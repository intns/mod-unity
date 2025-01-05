using System;
using System.IO;
using UnityEngine;

public static class BigEndianBinaryReaderExtensions
{
    public static short ReadInt16BE(this BinaryReader reader)
    {
        byte[] buffer = reader.ReadBytes(2);
        return (short)(buffer[0] << 8 | buffer[1]);
    }

    public static int ReadInt32BE(this BinaryReader reader)
    {
        byte[] buffer = reader.ReadBytes(4);
        return buffer[0] << 24 | buffer[1] << 16 | buffer[2] << 8 | buffer[3];
    }

    public static float ReadSingleBE(this BinaryReader reader)
    {
        byte[] buffer = reader.ReadBytes(4);
        Array.Reverse(buffer);
        return BitConverter.ToSingle(buffer, 0);
    }

    public static Vector3 ReadVector3(this BinaryReader reader)
    {
        return new Vector3(reader.ReadSingleBE(), reader.ReadSingleBE(), reader.ReadSingleBE());
    }

    public static void AlignToMultiple(this BinaryReader reader, int x)
    {
        long currentPosition = reader.BaseStream.Position;
        long remainder = currentPosition % x;

        if (remainder != 0)
        {
            long newPosition = currentPosition + (x - remainder);
            long res = newPosition - currentPosition;
            reader.BaseStream.Seek(res, SeekOrigin.Current);
        }
    }
}
