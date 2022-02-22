#nullable enable

namespace Edanoue.SaveData
{
    /// <summary>
    /// UnityEngine 名前空間などにある non-serializable な型を serializable として取り扱うための Wrapper Interface
    /// アセンブリ内部でのみ使用される
    /// </summary>
    /// <typeparam name="T"></typeparam>
    internal interface ISerializableWrapper<T>
    {
        /// <summary>
        /// オリジナルの T オブジェクトからの変換処理を実装する
        /// </summary>
        /// <param name="value"></param>
        void ConvertFrom(in T value);

        /// <summary>
        /// T オブジェクトへの変換処理を実装する
        /// </summary>
        /// <value></value>
        T ConvertTo { get; }
    }
}
