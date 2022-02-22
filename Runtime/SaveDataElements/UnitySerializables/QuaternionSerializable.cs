#nullable enable

namespace Edanoue.SaveData
{
    using System;
    using UnityEngine;

    /// <summary>
    /// UnityEngine.Quaternion の Serializable 版の Wrapper
    /// </summary>
    [Serializable]
    internal struct QuaternionSerializable : ISerializableWrapper<Quaternion>
    {
        float X;
        float Y;
        float Z;
        float W;

        Quaternion ISerializableWrapper<Quaternion>.ConvertTo => new(X, Y, Z, W);
        void ISerializableWrapper<Quaternion>.ConvertFrom(in Quaternion value)
        {
            X = value.x;
            Y = value.y;
            Z = value.z;
            W = value.w;
        }
    }
}
