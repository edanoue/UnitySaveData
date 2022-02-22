#nullable enable
namespace Edanoue.SaveData
{
    using System;
    using System.Runtime.InteropServices;

    /// <summary>
    /// 合計 32 byte のセーブデータのヘッダーファイルの構造体
    /// セーブデータ先頭 32 Bytes 部分にこの内容が書き込まれます
    /// </summary>
    /// <note>
    /// 一度にセーブデータのすべての内容を読み込んでしまうと重いので
    /// この部分のみを先読みするなどをします
    /// </note>
    [StructLayout(LayoutKind.Explicit, Size = 32)]
    internal struct SaveDataHeaderBinary
    {
        [Flags]
        internal enum HeaderFlag : byte
        {
            None = 0,
            // オートセーブかどうか
            IsAutoSave = 1 << 0,
            // gzip で圧縮されているかどうか
            IsCompressed = 1 << 1,
            // 暗号化されているかどうか
            IsCrypted = 1 << 2,
        }

        #region Data Structures

        /// <summary>
        /// 0 ~ 7 (8 bytes)
        /// The first eight bytes of a Edanoue Savedata file
        /// always contain the following (decimal) values: 65 64 73 76 68 65 61 64 (edsvhead)
        /// </summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 8)]
        [FieldOffset(0)]
        internal System.Byte[] Signature;

        /// <summary>
        /// 8 (1 byte)
        /// セーブデータのバージョン
        /// このセーブデータ形式自体のバージョン
        /// Data Layout が変換したら更新すること
        /// </summary>
        [FieldOffset(8)]
        internal System.Byte Version;

        /// <summary>
        /// 9 (1 byte)
        /// セーブデータのフラグ
        /// </summary>
        [FieldOffset(9)]
        internal System.Byte Flag;

        /// <summary>
        /// 10 (1 byte)
        /// ゲーム本編のMajorバージョン ()
        /// </summary>
        [FieldOffset(10)]
        internal System.Byte AppVersionMajor;

        /// <summary>
        /// 11 (1 byte)
        /// ゲーム本編のMinorバージョン ()
        /// </summary>
        [FieldOffset(11)]
        internal System.Byte AppVersionMinor;

        // 12 ~ 15
        // 4 bytesの予備

        /// <summary>
        /// 16 ~ 23 (8 byte)
        /// セーブデータが作成された時間
        /// DateTime(1970, 1, 1) からの totalMilliSeconds が保存されています(単位: UTC)
        /// </summary>
        /// <value></value>
        [FieldOffset(16)]
        internal System.UInt64 Created;

        /// <summary>
        /// 24 ~ 27 (4 byte)
        /// Body に含まれているセーブデータのByte数
        /// </summary>
        [FieldOffset(24)]
        internal System.UInt32 DataByteCount;

        // 28 ~ 31
        // 4bytes の予備

        #endregion // End Data Structures

        internal void Init(
            uint dataByteCount,
            bool isAutoSave = false,
            bool isCompressed = false,
            bool isCrypted = false,
            uint appVersionMajor = 0,
            uint appVersionMinor = 0
        )
        {
            // Signature を作成する
            // TODO: ソースコード見られるのであんま意味ない
            // edsvhead という 8 bytes のシグネチャを作成する
            // hex 表記だと以下の
            // 65 64 73 76 68 65 61 64
            // となる
            {
                Signature = new byte[8];
                string[] hexValuesSplit = { "65", "64", "73", "76", "68", "65", "61", "64" };
                int index = 0;
                foreach (var hex in hexValuesSplit)
                {
                    // Convert the number expressed in base-16 to an integer.
                    byte value = Convert.ToByte(hex, 16);
                    Signature[index++] = value;
                }
            }

            // バージョンを作成する
            {
                // これはこのセーブデータのフォーマット自体のバージョン
                Version = 1;
            }

            // ゲーム本編のバージョンを作成する
            {
                AppVersionMajor = (byte)appVersionMajor;
                AppVersionMinor = (byte)appVersionMinor;
            }

            // Flag を作成する
            {
                Flag = (byte)HeaderFlag.None;
                if (isAutoSave)
                    Flag |= (byte)HeaderFlag.IsAutoSave;
                if (isCompressed)
                    Flag |= (byte)HeaderFlag.IsCompressed;
                if (isCrypted)
                    Flag |= (byte)HeaderFlag.IsCrypted;
            }

            // 作成時刻を代入
            {
                var now = DateTime.UtcNow;
                ulong milliseconds = (ulong)(now - new DateTime(1970, 1, 1)).TotalMilliseconds;
                Created = milliseconds;
            }

            DataByteCount = dataByteCount;
        }

    }
}
