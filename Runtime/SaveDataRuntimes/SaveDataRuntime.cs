#nullable enable
namespace Edanoue.SaveData
{
    using System;
    using System.IO;
    using System.Runtime.InteropServices;

    /// <summary>
    /// アプリケーション内でロードされるセーブデータのかたまり のクラス
    /// <note>
    /// 軽量な Header, 実データの Body という二段構えになっています
    /// タイトル時点とかでは Body はロードせずに Header だけ読み込むという運用を想定しています
    /// </note>
    /// </summary>
    public class SaveDataRuntime
    {
        #region Public API

        /// <summary>
        /// Header 情報にアクセス
        /// </summary>
        public SaveDataRuntimeHeader Header => m_header;

        /// <summary>
        /// Body 部分のデータをロード済みかどうか
        /// </summary>
        public bool IsExistedBody => m_body is not null;

        /// <summary>
        /// SaveDataElement を取得する関数
        /// 事前に Body 部分のデータがロード済みである必要があります
        /// </summary>
        /// <param name="key">取得するキー</param>
        /// <param name="value">取得できた値</param>
        /// <returns>取得に成功した場合は true, key が存在しない場合は false</returns>
        public bool TryGetValue<T>(string key, out T? value)
        {
            if (Body.TryGetValue<T>(key, out var element))
            {
                value = element!.Value;
                return true;
            }
            value = default;
            return false;
        }

        /// <summary>
        /// SaveDataElement を追加する関数
        /// 新規作成/更新 の場合はこの関数を使用する
        /// SaveAsync を呼ぶまで保存されないので注意
        /// 事前に Body 部分のデータがロード済みである必要があります
        /// </summary>
        /// <param name="element">保存する SaveDataElement</param>
        public void Commit<T>(string key, T value)
        {
            var e = new SaveDataElement<T>(key, value);
            Body.Add<T>(e);
        }

        /// <summary>
        /// 既存のセーブデータを元に作成された場合, 読み込み時の.savのファイル名を取得
        /// 新規作成されたセーブデータの場合空欄
        /// </summary>
        /// <value></value>
        public string FileNameWhenLoading
        {
            get
            {
                if (LoadingFilePath == "")
                {
                    return "";
                }
                return Path.GetFileNameWithoutExtension(LoadingFilePath);
            }
        }

        #endregion

        #region Internal API

        /// <summary>
        /// 空のセーブデータを新規作成する
        /// ゲームを始めからスタートするときに呼ばれる
        /// </summary>
        /// <returns></returns>
        internal static SaveDataRuntime CreateEmpty()
        {
            return new SaveDataRuntime();
        }

        /// <summary>
        /// 既存の .sav より セーブデータを生成する
        /// </summary>
        /// <param name="header"></param>
        /// <param name="filepath"></param>
        /// <returns></returns>
        internal static SaveDataRuntime CreateFromHeader(SaveDataHeaderBinary header, in string filepath)
        {
            return new SaveDataRuntime(header, filepath);
        }

        /// <summary>
        /// ロード時に読み込みもととなったファイルパス
        /// 新規作成時には代入されていない
        /// セーブ時は全く異なるところに保存してもよい
        /// </summary>
        /// <value></value>
        internal string LoadingFilePath { get; private set; } = "";

        #endregion

        readonly SaveDataRuntimeHeader m_header;
        SaveDataRuntimeBody? m_body;
        SaveDataRuntimeBody Body => IsExistedBody ? m_body! : throw new InvalidOperationException("Body がロードされていません");


        // Body 部分の読み込みを行う
        // TODO: Async化する
        internal void LoadBodyAsync()
        {
            // Header が Invalid である
            if (!Header.IsValid)
            {
                // TODO: 考えよう
                throw new NotImplementedException();
            }

            // すでに読み込み済みならエラーを送出する
            if (IsExistedBody)
            {
                throw new InvalidOperationException("すでに Body が読み込まれています");
            }

            // Filepath が存在していない
            if (LoadingFilePath == "")
            {
                throw new InvalidOperationException("LoadingFilePath is missing");
            }

            UnityEngine.Debug.Log($"[SaveDataAPI] Start to loading savedata (Body Only): {LoadingFilePath}");

            // バイナリデータを取得
            byte[] bodyRaw;

            // Binary Reader を作成する
            // TODO: Async にする
            {
                using var reader = new BinaryReader(File.Open(LoadingFilePath, FileMode.Open, FileAccess.Read));

                // Body 部分のバイト列を読み込む
                // 先頭位置をHeader 終了地点に移動
                var headersize = Marshal.SizeOf<SaveDataHeaderBinary>();
                reader.BaseStream.Position = headersize;

                // 残りのバイトをすべて読み込む
                bodyRaw = reader.ReadBytes(Header.DataByteCount);
            }

            // 暗号化されたセーブデータであるならば, Decrypto を実行
            // 必ず最初に行うこと
            if (Header.IsCrypted)
            {
                // TODO: Async にする
                Cryptor.Decrypt(ref bodyRaw);
            }

            // 圧縮されたセーブデータであるならば, Decomporess を実行
            if (Header.IsCompressed)
            {
                // TODO: Async にする
                Compressor.Decompress(ref bodyRaw);
            }

            // Binary から復元する
            // TODO: Async にする
            m_body = SaveDataRuntimeBody.FromBinary(in bodyRaw);

            UnityEngine.Debug.Log($"[SaveDataAPI] Success to loaded savedata (Body Only): {LoadingFilePath}");
        }

        /// <summary>
        /// コンストラクタ, 外部には公開しない
        /// CreateEmpty 関数により使用される
        /// </summary>
        private SaveDataRuntime()
        {
            // 新規にHeaderを作成する
            m_header = new SaveDataRuntimeHeader(new SaveDataHeaderBinary());
            // 新規に Body を作成しておく
            m_body = new SaveDataRuntimeBody();
        }

        /// <summary>
        /// コンストラクタ
        /// 事前読み込み済みの Headerを引数に受け取るバージョン
        /// このあと手動でロードを行う必要がある
        /// </summary>
        /// <param name="filepath"></param>
        /// <param name="header"></param>
        private SaveDataRuntime(SaveDataHeaderBinary header, in string filepath)
        {
            m_header = new SaveDataRuntimeHeader(header);
            LoadingFilePath = filepath;
        }

        // 指定されたパスに保存をおこなう
        // TODO: Async化する
        internal void SaveAsync(in string filepath, bool isAutoSave)
        {
            // Body の読み込み前なら
            if (!IsExistedBody)
            {
                // エラーを履く, Body が存在しない場合は保存することができない
                throw new InvalidOperationException("Body部分のデータが存在していないため, 保存することができません");
            }

            // Binary 版の SaveData 本体の作成
            var saveData = new SaveDataBinary();
            {
                // Body を Binary に
                // TODO: Async にする
                Body.ToBinary(out saveData.Data);

                // Body 部分の圧縮 (必ず暗号化より先に行うこと)
                // TODO: Async にする
                Compressor.Compress(ref saveData.Data);

                // Body 部分の暗号化
                // TODO: Async にする
                Cryptor.Encrypt(ref saveData.Data);

                // 新しい Header の作成

                // 保存時点での Application の Versionを取得する
                // x.x.x という形式である
                var versionStr = UnityEngine.Application.version;
                // TODO: x.x.x という保証 とパースの実装する
                uint appVersionMajor = 0;
                uint appVersionMinor = 0;

                // 南: これ, 新しくするのは, CreatedTime の更新のためだけなので考えましょう
                // TODO: Update Time とか 現在のプレイ時間は..?
                var header = new SaveDataHeaderBinary();
                var bodyBinaryCount = saveData.Data.Length;
                header.Init(
                    (uint)bodyBinaryCount,
                    isAutoSave: isAutoSave,
                    isCompressed: true,
                    isCrypted: true,
                    appVersionMajor: appVersionMajor,
                    appVersionMinor: appVersionMinor
                );

                saveData.Header = header;
            }

            // BinaryWriter を使用して書き込みを行う
            // TODO: Async にする
            {
                // Binary Writer を作成する
                using var writer = new BinaryWriter(File.Open(filepath, FileMode.Create));

                // Header の書き込み
                {
                    int headerSize = Marshal.SizeOf<SaveDataHeaderBinary>();
                    var buffer = new byte[headerSize];
                    var ptr = IntPtr.Zero;

                    try
                    {
                        ptr = Marshal.AllocHGlobal(headerSize);
                        Marshal.StructureToPtr(saveData.Header, ptr, false);
                        Marshal.Copy(ptr, buffer, 0, headerSize);
                    }
                    finally
                    {
                        if (ptr != IntPtr.Zero)
                            Marshal.FreeHGlobal(ptr);
                    }

                    writer.Write(buffer);
                }

                // Body の書き込み
                {
                    writer.Write(saveData.Data);
                }
            }
        }
    }
}
