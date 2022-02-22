#nullable enable
namespace Edanoue.SaveData
{
    using System.Collections.Generic;

    /// <summary>
    /// Runtime で使用する SaveData Body
    /// </summary>
    public class SaveDataRuntimeBody
    {
        private readonly Dictionary<string, ISaveDataElement> m_data = new();

        internal SaveDataRuntimeBody()
        {
        }

        // FromBinary から呼ばれるコンストラクタ
        private SaveDataRuntimeBody(in ISaveDataElement[] data)
        {
            // 配列の要素から辞書を構築する
            foreach (var element in data)
            {
                Add(element);
            }
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="element"></param>
        private void Add(in ISaveDataElement element)
        {
            // ISaveDataElement から キーを取得する
            var key = element.Key;

            // キーが存在しない場合は新規で作成する
            if (!ContainsKey(key))
            {
                m_data.Add(key, element);
            }
            // すでに値が存在していた場合は上書きする
            // ここでは型の一致を確認せずに強制的に上書きするので注意
            else
            {
                m_data[key] = element;
            }
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="element"></param>
        internal void Add<T>(in ISaveDataElement element)
        {
            // ISaveDataElement から キーを取得する
            var key = element.Key;

            // キーが存在しない場合は新規で作成する
            if (!ContainsKey(key))
            {
                m_data.Add(key, element);
                return;
            }

            // すでにキーが存在していた場合は 型の一致を確認する
            // 型が一致している場合は更新
            // 一致していない場合は例外を送出する
            if (TryGetValue<T>(key, out var _))
            {
                // 値を更新する
                m_data[key] = element;
            }
            // 型が一致しなかったので例外を創出する
            else
            {
                throw new System.ArgumentException($"Does not match value type. key: {key}", nameof(T));
            }
        }

        /// <summary>
        /// 指定されたキーでのアクセスに成功して, 型が一致した場合のみ値を返す関数
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        internal bool TryGetValue<T>(in string key, out ISaveDataElement<T>? value)
        {
            // 同じキーが存在しているかどうか
            if (m_data.TryGetValue(key, out var valueRaw))
            {
                // 値のキャストに成功するかどうか
                if (valueRaw is ISaveDataElement<T> valueRawCasted)
                {
                    value = valueRawCasted;
                    return true;
                }

            }
            value = null;
            return false;
        }

        /// <summary>
        /// 指定されたキーが存在しているかどうか
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public bool ContainsKey(in string key) => m_data.ContainsKey(key);

        /// <summary>
        /// 保存されている要素数
        /// </summary>
        public int Count => m_data.Count;

        /// <summary>
        ///
        /// </summary>
        /// <param name="output"></param>
        internal void ToBinary(out byte[] output)
        {
            // 辞書の要素数と同じだけの配列を作成する
            var temp = new ISaveDataElement[Count];
            // 辞書の値にある ISaveDataElement を配列にコピ
            // TODO: 重い
            m_data.Values.CopyTo(temp, 0);

            // BinaryHelper を作成する
            var helper = new SaveDataBodyBinaryHelper(in temp);

            // Helperによりバイナリを作成する
            helper.ToBinary(out output);
        }

        internal byte[] ToBinary()
        {
            ToBinary(out var output);
            return output;
        }

        internal static SaveDataRuntimeBody FromBinary(in byte[] input)
        {
            var b = SaveDataBodyBinaryHelper.FromBinary(in input);
            return new SaveDataRuntimeBody(in b.Data);
        }
    }
}
