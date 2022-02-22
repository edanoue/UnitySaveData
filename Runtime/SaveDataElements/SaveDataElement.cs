#nullable enable
using System;
using System.Collections.Generic;

namespace Edanoue.SaveData
{
    using System;

    /// <summary>
    /// セーブデータにシリアライズされるベースクラス
    /// </summary>
    /// <typeparam name="T"></typeparam>
    [Serializable]
    public class SaveDataElement<T> : ISaveDataElement<T>
    {
        #region ISaveDataElements impls
        // Getter only なので Serialize されません

        string ISaveDataElement.Key => this._key;
        T ISaveDataElement<T>.Value => _GetValue();

        #endregion

        #region Serializable Fields

        /// <summary>
        /// Element の Key
        /// </summary>
        /// <value></value>
        private readonly string _key;

        /// <summary>
        /// 保存されている値 (object版)
        /// </summary>
        /// <value></value>
        private readonly object _value;

        #endregion

        private T _GetValue()
        {
            if (s_serializedWrapperMap.ContainsKey(typeof(T)))
            {
                var castedValue = (ISerializableWrapper<T>)this._value;
                return castedValue.ConvertTo;
            }
            else
            {
                return (T)this._value;
            }
        }

        private static Dictionary<Type, Type> s_serializedWrapperMap = new()
        {
            { typeof(UnityEngine.Vector3), typeof(Vector3Serializable) },
            { typeof(UnityEngine.Quaternion), typeof(QuaternionSerializable) },
        };

        public SaveDataElement(in string key, in T value)
        {
            // Arguments Validations
            {
                // key is not null
                if (System.String.IsNullOrEmpty(key))
                {
                    throw new ArgumentNullException(nameof(key));
                }

                // value is not null
                if (value is null || value == null)
                {
                    throw new ArgumentNullException(nameof(value));
                }

                // Type check
                var _type = typeof(T);

                // type not exist in whitelist
                if (!s_serializedWrapperMap.ContainsKey(_type))
                {
                    // value must be Serializable
                    if (!_type.IsSerializable)
                    {
                        throw new ArgumentException($"{typeof(T)} is not serializable", nameof(value));
                    }
                }
            }

            // for Specic types operations
            if (s_serializedWrapperMap.TryGetValue(typeof(T), out var wrapperType))
            {
                var wrapper = (ISerializableWrapper<T>)Activator.CreateInstance(wrapperType);
                wrapper.ConvertFrom(value);
                _key = key;
                _value = wrapper;
            }
            else
            {
                _key = key;
                _value = value;
            }
        }
    }
}
