using System;
using System.IO;
using UnityEngine;

namespace MODFile
{
    /// <summary>
    /// Represents an interface for reading data from a binary reader.
    /// </summary>
    public interface IReadable
    {
        /// <summary>
        /// Reads data from the specified binary reader.
        /// </summary>
        /// <param name="reader">The binary reader to read data from.</param>
        void Read(BinaryReader reader);
    }

    /// <summary>
    /// Represents a serializable class that can be read from a binary stream and converted to a Vector3.
    /// </summary>
    [Serializable]
    public class Vector3Readable : IReadable
    {
        public Vector3 Vector;

        public Vector3Readable() { }

        public Vector3Readable(float x, float y, float z)
        {
            Vector = new Vector3(x, y, z);
        }

        public void Read(BinaryReader reader)
        {
            Vector = reader.ReadVector3();
        }

        public bool NearZero(float epsilon = 0.0001f)
        {
            return Mathf.Abs(Vector.x) < epsilon
                && Mathf.Abs(Vector.y) < epsilon
                && Mathf.Abs(Vector.z) < epsilon;
        }

        public static implicit operator Vector3(Vector3Readable v)
        {
            return v.Vector;
        }

        public static implicit operator Vector3Readable(Vector3 v)
        {
            return new Vector3Readable { Vector = v };
        }
    }

    /// <summary>
    /// Represents a serializable class that can be read from a binary stream and converted to a Vector2.
    /// </summary>
    [Serializable]
    public class Vector2Readable : IReadable
    {
        public Vector2 Vector;

        public void Read(BinaryReader reader)
        {
            Vector = new Vector2(reader.ReadSingleBE(), reader.ReadSingleBE());
        }

        public static implicit operator Vector2(Vector2Readable v)
        {
            return v.Vector;
        }

        public static implicit operator Vector2Readable(Vector2 v)
        {
            return new Vector2Readable { Vector = v };
        }
    }
}
