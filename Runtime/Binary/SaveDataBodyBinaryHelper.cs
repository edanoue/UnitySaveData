#nullable enable
namespace Edanoue.SaveData
{
    using System;
    using System.IO;
    using System.Runtime.Serialization.Formatters.Binary;

    [Serializable]
    internal class SaveDataBodyBinaryHelper
    {
        internal readonly ISaveDataElement[] Data;

        internal SaveDataBodyBinaryHelper(in ISaveDataElement[] data)
        {
            Data = data;
        }

        internal void ToBinary(out byte[] output)
        {
            // メンバフィールドを BinaryFormatter を使用して Binary列に変換する
            using var memoryStream = new MemoryStream();
            var binaryFormatter = new BinaryFormatter();
            binaryFormatter.Serialize(memoryStream, this);
            output = memoryStream.ToArray();
        }

        internal static SaveDataBodyBinaryHelper FromBinary(in byte[] input)
        {
            using var memoryStream = new MemoryStream();
            var binaryFormatter = new BinaryFormatter();

            memoryStream.Write(input, 0, input.Length);
            memoryStream.Seek(0, SeekOrigin.Begin);

            try
            {
                return (SaveDataBodyBinaryHelper)binaryFormatter.Deserialize(memoryStream);
            }
            catch (System.Runtime.Serialization.SerializationException)
            {
                // デシリアライズに失敗したときにここに落ちる
                // 1. Decompressがスルーされてしまった時
                throw new NotImplementedException();
            }

        }
    }
}
