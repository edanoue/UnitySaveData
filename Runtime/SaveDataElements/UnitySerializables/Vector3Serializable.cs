#nullable enable

namespace Edanoue.SaveData
{
    using System;
    using UnityEngine;

    /// <summary>
    /// UnityEngine.Vector3 の Serializable 版の Wrapper
    /// </summary>
    [Serializable]
    internal struct Vector3Serializable : ISerializableWrapper<Vector3>
    {
        float X;
        float Y;
        float Z;

        Vector3 ISerializableWrapper<Vector3>.ConvertTo => new(X, Y, Z);
        void ISerializableWrapper<Vector3>.ConvertFrom(in Vector3 value)
        {
            X = value.x;
            Y = value.y;
            Z = value.z;
        }
    }
}
