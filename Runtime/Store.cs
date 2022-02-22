#nullable enable

namespace Edanoue.SaveData
{
    using System;
    using System.IO;
    using System.Runtime.InteropServices;
    using UnityEngine;

    public static class Store
    {
        #region 公開プロパティ

        /// <summary>
        /// セーブデータが保存されているディレクトリを取得
        /// </summary>
        /// <value></value>
        public static string SavePath
        {
            get
            {
                // Unity が提供してくれる, セーブデータ用に使えるディレクトリを取得する
                // see: https://docs.unity3d.com/ScriptReference/Application-persistentDataPath.html
                // Windows だと 以下のパスとなる
                // %userprofile%\AppData\LocalLow\<companyname>\<productname>
                var basePath = Application.persistentDataPath;

                // Save ディレクトリを結合して返す
                var path = Path.Combine(basePath, "Save");
                // Windows-style -> Unix-stlye
                path = path.Replace("\\", "/");
                return path;
            }
        }

        /// <summary>
        /// 現在読込中の In-Game Savedata
        /// </summary>
        /// <value></value>
        public static SaveDataRuntime? InGame
        {
            get
            {
                lock (s_loadedSaveDataInGameLock)
                    return s_loadedSaveDataInGame;
            }
            private set
            {
                lock (s_loadedSaveDataInGameLock)
                {
                    s_loadedSaveDataInGame = value;
                }
            }
        }

        #endregion

        #region 公開関数

        /// <summary>
        /// ファイル名を指定するとパスに変換してくれる関数
        /// </summary>
        /// <param name="filename"></param>
        /// <returns></returns>
        public static string SaveFilenameToPath(in string filename) => Path.Combine(SavePath, $"{filename}{SaveDataExtension}");

        /// <summary>
        /// 保存されているすべての SaveDataMeta のフルパスを取得する
        /// </summary>
        public static string[] GetAllMetaDataAbsolutePaths()
        {
            // 事前にセーブデータが保存されているディレクトリが存在するかの確認を行う
            if (!Directory.Exists(SavePath))
                // ディレクトリが存在しない場合は空の配列を返す
                return new string[0];

            // セーブデータが保存されているディレクトリ以下にある .sav 拡張子のファイルの一覧を取得
            return Directory.GetFiles(SavePath, $"*{SaveDataExtension}");
        }

        /// <summary>
        /// 新規にゲーム本編のセーブデータを作成する
        /// 既存のロード中のデータは破棄される
        /// </summary>
        public static bool CreateAndLoadEmptyInGame(bool isForceLoad = false)
        {
            // すでに InGame用の セーブデータを読込中である
            if (IsLoadingInGameSaveData)
            {
                // 強制破棄のフラグが立っているならば破棄して進む
                if (isForceLoad)
                {
                    UnloadInGame();
                }
                // 立っていない場合は早期リターンする
                else
                {
                    Debug.LogWarning("[SaveDataAPI] すでに InGameSaveDaga が読み込まれているため, 新規作成に失敗しました");
                    return false;
                }
            }

            // 新規でセーブデータを作成する
            var newSaveData = SaveDataRuntime.CreateEmpty();

            // 現在読込中のゲーム本編のセーブデータとする
            InGame = newSaveData;

            Debug.Log("[SaveDataAPI] Created and load new ingame savedata");
            return true;
        }

        public static bool IsLoadingInGameSaveData => InGame is not null;


        /// <summary>
        /// 現在読込中のセーブデータを破棄する
        /// プレイ画面からタイトル画面に戻る際などにこの関数を呼ぶ必要がある
        /// </summary>
        public static void UnloadInGame()
        {
            if (IsLoadingInGameSaveData)
            {
                // 現在読込中のセーブデータを破棄する
                InGame = null;
            }
        }

        /// <summary>
        /// 現在ロード中のゲーム本編のセーブデータを自動保存する
        /// </summary>
        public static void AutoSaveInGameAsync()
        {
            // TODO: オートセーブ数の 最大保存数を設定する
            // TODO: オートセーブの名前を設定する
            SaveInGameInternalAsync("autosave00", isAutoSave: true);
        }

        /// <summary>
        /// 現在ロード中のゲーム本編のセーブデータを手動で保存する
        /// </summary>
        /// <param name="slotname"></param>
        public static void ManualSaveInGameAsync(in string slotname)
        {
            SaveInGameInternalAsync(slotname, isAutoSave: false);
        }

        /// <summary>
        /// 現在読込中のデータを保存
        /// </summary>
        /// <param name="filename"></param>
        // TODO: Asyncにする
        private static void SaveInGameInternalAsync(in string filename, bool isAutoSave = false)
        {
            if (System.String.IsNullOrEmpty(filename) || System.String.IsNullOrWhiteSpace(filename))
            {
                throw new ArgumentNullException(nameof(filename));
            }

            // まだ何もロード済みではない場合
            if (InGame is null)
            {
                throw new InvalidOperationException("現在読込中のセーブデータがないため保存することができません.");
            }

            // 初回保存時の場合はSavePathディレクトリを作成してあげる必要がある
            if (!Directory.Exists(SavePath))
            {
                // 存在しないばあいディレクトリを全て(Recursiveに)作成する
                Directory.CreateDirectory(SavePath);
                Debug.Log($"[SaveDataAPI] Created directory: {SavePath}");
            }

            // 保存予定のファイル名を取得する
            var filepath = SaveFilenameToPath(filename);

            // TODO: セーブデータのファイル名, 必ずぶつからないようにしてしまって良い
            //       ケアが面倒くさくないので
            //       とはいえ, 重複の確認は必要
            //       20210605100512.sav auto2021060510512.sav とかでいい

            // TODO: 既に書き込み先のファイルが存在しているときの処理を行う

            // TODO: AutoSave の場合は, 古いファイルを削除する必要がある

            InGame.SaveAsync(filepath, isAutoSave);
        }

        // TODO: Async にする
        public static void LoadInGameAsync(in string filepath)
        {
            // 引数の Validation Check
            {
                // file path が空白文字である
                if (System.String.IsNullOrEmpty(filepath) || System.String.IsNullOrWhiteSpace(filepath))
                {
                    throw new ArgumentNullException(nameof(filepath));
                }
                // file path に指定された ファイルが存在していない
                if (!File.Exists(filepath))
                {
                    throw new FileNotFoundException($"指定されたファイルが存在していません: {filepath}", nameof(filepath));
                }
            }

            // Header 部分のみ事前読み込みを行う
            var loadingSaveData = LoadSaveDataHeaderOnlyAsync(filepath);

            // Body 部分の読み込みを行う
            LoadInGameFromHeaderOnly(loadingSaveData);
        }

        public static bool LoadAllInGamesHeaderOnly(out SaveDataRuntime[] savedatas)
        {
            // アクセスできるすべてのセーブデータを取得
            var savedataPaths = GetAllMetaDataAbsolutePaths();
            int saveDataCount = savedataPaths.Length;

            // 空だったら早期リターンする
            if (saveDataCount < 1)
            {
                savedatas = new SaveDataRuntime[0];
                return false;
            }

            savedatas = new SaveDataRuntime[savedataPaths.Length];
            for (int i = 0; i < saveDataCount; i++)
            {
                // Header 部分だけのロードを行って結果に代入していく
                var path = savedataPaths[i];
                var savedataHeaderOnly = LoadSaveDataHeaderOnlyAsync(path);
                savedatas[i] = savedataHeaderOnly;
            }

            return true;
        }

        // TODO: Async にする
        /// <summary>
        /// Header 部分だけ事前読み越された SaveData の Body部分のロードを行い, In-Game Savedata とする処理
        /// </summary>
        /// <param name="filepath"></param>
        public static void LoadInGameFromHeaderOnly(SaveDataRuntime loadingSaveData)
        {
            // 引数に指定された SaveDataRuntime ですでに Body が読み込まれていたらエラー
            if (loadingSaveData.IsExistedBody)
            {
                throw new ArgumentException("すでにBody部分が読み込まれています", nameof(loadingSaveData));
            }

            // Body 部分の読み込みをおこなう
            loadingSaveData.LoadBodyAsync();

            // TODO: 最終確定前のValidation Check やること

            // In-Game savedata としてマークする
            InGame = loadingSaveData;
        }

        #endregion

        #region 内部処理用

        private static object s_loadedSaveDataInGameLock = new object();
        private static SaveDataRuntime? s_loadedSaveDataInGame;

        /// <summary>
        /// SaveData の拡張子を取得する
        /// </summary>
        internal static string SaveDataExtension => ".sav";

        /// <summary>
        /// セーブデータをRead済みのBinaryReader から, Header 部分のみ読み込み
        /// </summary>
        /// <param name="reader"></param>
        /// <returns></returns>
        private static SaveDataHeaderBinary ReadHeader(BinaryReader reader)
        {
            var size = Marshal.SizeOf<SaveDataHeaderBinary>();
            var ptr = IntPtr.Zero;

            try
            {
                reader.BaseStream.Position = 0;
                ptr = Marshal.AllocHGlobal(size);
                Marshal.Copy(reader.ReadBytes(size), 0, ptr, size);
                return (SaveDataHeaderBinary)Marshal.PtrToStructure(ptr, typeof(SaveDataHeaderBinary));
            }
            finally
            {
                if (ptr != IntPtr.Zero)
                    Marshal.FreeHGlobal(ptr);
            }
        }

        // TODO: Async にする
        private static SaveDataRuntime LoadSaveDataHeaderOnlyAsync(in string filepath)
        {
            // 引数の Validation Check
            {
                // file path が空白文字である
                if (System.String.IsNullOrEmpty(filepath) || System.String.IsNullOrWhiteSpace(filepath))
                {
                    throw new ArgumentNullException(nameof(filepath));
                }
                // file path に指定された ファイルが存在していない
                if (!File.Exists(filepath))
                {
                    throw new FileNotFoundException($"指定されたファイルが存在していません: {filepath}", nameof(filepath));
                }
            }

            Debug.Log($"[SaveDataAPI] Start to loading savedata (Header Only): {filepath}");

            // Header 部分の読み込みを行う
            SaveDataHeaderBinary header;
            {
                try
                {
                    // Binary Reader を作成する
                    using var reader = new BinaryReader(File.Open(filepath, FileMode.Open, FileAccess.Read));

                    // Header 部分だけ読み込む
                    // TODO: Async にする
                    header = ReadHeader(reader);
                }
                // 指定されたパスにファイルが見つからなかった場合
                catch (FileNotFoundException e)
                {
                    throw e;
                }
            }

            // TODO: Validation Check する

            Debug.Log($"[SaveDataAPI] Success to loaded savedata (Header Only): {filepath}");

            // 実行中のセーブデータを作成する
            return SaveDataRuntime.CreateFromHeader(header, filepath);
        }

        #endregion

    }
}
