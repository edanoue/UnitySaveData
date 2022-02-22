#nullable enable

namespace Edanoue.SaveData
{
    using System;
    using UnityEngine;
    using UnityEngine.Events;
    using Sirenix.OdinInspector;

    /// <summary>
    /// InGame 用のセーブデータに対しての 一つの要素として機能する Component のベースクラス
    /// </summary>
    /// <typeparam name="T">保存する型</typeparam>
    public abstract class EdaSaveElementBase<T> : MonoBehaviour
    {
        #region Key Group

        [SerializeField]
        [BoxGroup("Key")]
        [LabelText("Override key")]
        [HideInPlayMode]
        [ToggleLeft]
        bool m_useCustomKey;

        [SerializeField]
        [BoxGroup("Key")]
        [ShowIf("m_useCustomKey")]
        [LabelText("Key")]
        [DisableInPlayMode]
        [ValidateInput("_ValidateCustomKey")]
        string m_customKey = System.String.Empty;

        // 初回時に自動生成されるGUID
        [SerializeField]
        [HideInInspector]
        string m_guidKey = Convert.ToBase64String(Guid.NewGuid().ToByteArray());

        [ShowInInspector]
        [BoxGroup("Key")]
        [HideIf("m_useCustomKey")]
        public string Key => m_useCustomKey ? m_customKey : m_guidKey;

        #endregion // Key Group

        #region Value Group

        [SerializeField]
        [BoxGroup("Value")]
        [LabelText("デフォルトの値")]
        [Tooltip("初回プレイ時に使用されるデフォルトの値を設定")]
        [DisableInPlayMode]
        T m_defaultValue = default!;

        #endregion // Value Group

        #region Unity Events Group

        [SerializeField]
        [BoxGroup("Unity Events")]
        [LabelText("ChangedValue")]
        UnityEvent<T>? ChangedValueUnityEvent;

        #endregion // UnityEvents Group

        #region 公開イベント
        public event Action<T>? ChangedValue;

        #endregion

        #region MonoBehaviour Messages

        void Start()
        {
            // Store と 同期を行う
            // この時点でイベントが呼ばれる
            // 他のオブジェクトが Awake で準備をしているであろうので, この処理は Start 内で呼ぶ

            var loadedSaveData = Store.InGame;

            // この時点ではまだ InGame 用のセーブデータがロードされていない
            // これに関してはこのスクリプトの責任ではないため早期リターンする
            if (loadedSaveData is null)
            {
                _SetValue(m_defaultValue);
                return;
            }

            // すでに InGame 用のセーブデータが ロードされている場合は 値の取得を試みる
            if (loadedSaveData.TryGetValue<T>(Key, out var element))
            {
                // 値の取得に成功した場合は, 自身の値を更新する
                _SetValue(element!);
            }

            // InGame 用のセーブデータがロードされているが
            // このコンポーネントで指定されている Key が保存されていない
            // デフォルトの値を使用する
            else
            {
                _SetValue(m_defaultValue);
            }
        }

        #endregion

        #region 公開関数

        /// <summary>
        /// 保存している値を取得する. セーブされていない値がある場合はそちらを取得する
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public T GetValue()
        {
            if (m_value is null)
                throw new InvalidProgramException("値が null です");

            return m_value;
        }

        /// <summary>
        /// 値を更新して, 変更をStoreに上げる関数
        /// </summary>
        /// <note>
        /// Store 側でSaveを行わないと, 値の保存は行われません
        /// </note>
        /// <param name="value"></param>
        public void Commit(in T value)
        {
            // 自身の値を更新する
            _SetValue(in value);

            // In-game の方の SaveData を取得する
            // これは現在ロード中の セーブスロット内に相当するセーブデータ
            var loadedSaveDataInGame = Store.InGame;
            if (loadedSaveDataInGame is null)
            {
                // TODO: Store で何もセーブデータがロードされていなかったら
                throw new InvalidOperationException("Failed to access LoadedSaveDataInGame. Create or Load first");
            }

            // Store の ロード済みの値を更新する
            loadedSaveDataInGame.Commit(Key, GetValue());
        }

        #endregion

        #region Unity の Inspector の Developments 機能

#if UNITY_EDITOR

        // 開発用のメモ
        // この要素が何に使用されるかを書くなど
        // 本番環境では利用できない
        [SerializeField]
        [BoxGroup("Editor Only Features")]
        [LabelText("メモ")]
        [TextArea]
        [DisableInPlayMode]
        internal string m_memo = System.String.Empty;

        // 開発用の Commit ボタン
        // ゲームプレイ中にのみ表示される
        [BoxGroup("Editor Only Features")]
        [HideInEditorMode]
        [InlineButton("_Commit", "Commit")]
        [LabelText("更新する値")]
        [ShowInInspector]
        T _commitPendingValue = default!;
        void _Commit()
        {
            if (Application.isPlaying)
            {
                Commit(_commitPendingValue);
            }
        }
#endif

        #endregion

        #region 内部処理用
        T? m_value;

        /// <summary>
        /// 値の書き込み, イベントの通知がなされる
        /// </summary>
        /// <param name="value"></param>
        private void _SetValue(in T value)
        {
            if (value == null)
            {
                throw new System.ArgumentNullException(nameof(value));
            }

            if (m_value == null)
            {
                m_value = value;
                ChangedValue?.Invoke(value);
                ChangedValueUnityEvent?.Invoke(value);
                return;
            }

            // 同じ値のばあい, 早期リターンする
            if (m_value.Equals(value))
            {
                return;
            }

            m_value = value;
            ChangedValue?.Invoke(value);
            ChangedValueUnityEvent?.Invoke(value);
        }

        #endregion

        #region エディタのバリデーション用
#if UNITY_EDITOR
        bool _ValidateCustomKey(string value, ref string errorMessage)
        {
            if (System.String.IsNullOrEmpty(value) || System.String.IsNullOrWhiteSpace(value))
            {
                errorMessage = "空白のキーです";
                return false;
            }

            var trimmedValue = value.Trim();
            if (trimmedValue != value)
            {
                errorMessage = "前後に空白文字が含まれています";
                return false;
            }

            return true;
        }
#endif
        #endregion


    }
}
