#nullable enable

namespace Edanoue.SaveData
{
    using System;
    using System.Runtime.InteropServices;

    /// <summary>
    ///
    /// </summary>
    [StructLayout(LayoutKind.Explicit)]
    internal struct SaveDataBinary
    {
        /// <summary>
        /// 合計 32 bytes の ヘッダー
        /// </summary>
        [FieldOffset(0)]
        internal SaveDataHeaderBinary Header;

        /// <summary>
        /// セーブデータ本体のほう
        /// </summary>
        [FieldOffset(32)]
        internal System.Byte[] Data;
    }

}
