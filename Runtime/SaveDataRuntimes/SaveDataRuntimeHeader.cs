#nullable enable
namespace Edanoue.SaveData
{
    using System;

    /// <summary>
    /// SaveDataHeaderBinary の情報を実行環境で取得するラッパークラス
    /// </summary>
    public class SaveDataRuntimeHeader
    {
        #region Public API

        /// <summary>
        /// セーブデータの保存時刻をUtcで取得
        /// </summary>
        /// <value></value>
        public DateTime SavedTimeUtc
        {
            get
            {
                var ms = m_header.Created;
                var time = TimeSpan.FromMilliseconds(ms);
                return new DateTime(1970, 1, 1) + time;
            }
        }

        /// <summary>
        /// セーブデータの保存時刻を実行マシンのLocalTimeで取得
        /// </summary>
        /// <value></value>
        public DateTime SavedTimeLocal => SavedTimeUtc.ToLocalTime();

        /// <summary>
        /// オートセーブとしてマークされている SaveData かどうか
        /// </summary>
        /// <returns></returns>
        public bool IsAutoSave => (m_header.Flag & (byte)SaveDataHeaderBinary.HeaderFlag.IsAutoSave) != 0;

        /// <summary>
        /// セーブデータが最後に保存されたアプリケーション本体のメジャーバージョン
        /// </summary>
        public int AppVersionMajor => m_header.AppVersionMajor;

        /// <summary>
        /// セーブデータが最後に保存されたアプリケーション本体のマイナーバージョン
        /// </summary>
        public int AppVersionMinor => m_header.AppVersionMinor;

        #endregion

        #region Internal API

        /// <summary>
        /// コンストラクタ, Internal なので外部から生成することができない
        /// </summary>
        /// <param name="header"></param>
        internal SaveDataRuntimeHeader(SaveDataHeaderBinary header)
        {
            m_header = header;
        }

        /// <summary>
        /// Signature や バージョンが正しい 現在取り扱える セーブデータかどうか
        /// TODO: 実装や要件がふわふわです
        /// </summary>
        /// <value></value>
        internal bool IsValid
        {
            get
            {
                // TODO: Header の バリデーションを行う
                // TODO: Signature の確認 など, この時点でできることをやる
                // TODO: 実装する
                return true;
            }
        }

        /// <summary>
        /// 圧縮されているセーブデータかどうか (内部向けの情報)
        /// </summary>
        /// <returns></returns>
        internal bool IsCompressed => (m_header.Flag & (byte)SaveDataHeaderBinary.HeaderFlag.IsCompressed) != 0;

        /// <summary>
        /// 暗号化されているセーブデータかどうか (内部向けの情報)
        /// </summary>
        /// <returns></returns>
        internal bool IsCrypted => (m_header.Flag & (byte)SaveDataHeaderBinary.HeaderFlag.IsCrypted) != 0;

        /// <summary>
        /// Body 部分の総Byte 数 (内部向けの情報)
        /// </summary>
        /// <returns></returns>
        internal int DataByteCount => (int)m_header.DataByteCount;

        #endregion

        #region 内部処理用

        /// <summary>
        /// Binary の参照
        /// </summary>
        readonly SaveDataHeaderBinary m_header;

        #endregion
    }
}
