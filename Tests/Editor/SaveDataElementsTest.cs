#nullable enable

using System.IO;
using NUnit.Framework;
using UnityEngine;
using Edanoue.SaveData;

public class SaveDataElementsのRuntime時点でのテスト
{
    // 各テスト実行前に実行される
    [SetUp]
    public void SetUp()
    {
        // 新規にセーブを作成する
        Store.CreateAndLoadEmptyInGame();
    }

    // 各テスト実行後に実行される
    [TearDown]
    public void TearDown()
    {
        // 後続のために破棄しておく
        Store.UnloadInGame();
    }

    [Test]
    public void セーブデータにBool型の要素を追加する()
    {
        // foo: true という bool 型のものを追加する
        var savedata = Store.InGame;
        savedata!.Commit("foo0", true);
        savedata!.Commit("foo1", false);

        // 要素追加後は値の取得ができる
        {
            savedata.TryGetValue<bool>("foo0", out var e);
            Assert.That(e, Is.True);
        }
        {
            savedata.TryGetValue<bool>("foo1", out var e);
            Assert.That(e, Is.False);
        }
    }

    [Test]
    public void セーブデータにInt型の要素を追加する()
    {
        // foo: 100 という int 型のものを追加する
        var savedata = Store.InGame;
        savedata!.Commit("foo", 100);

        // 要素追加後は値の取得ができる
        savedata.TryGetValue<int>("foo", out var e);
        Assert.That(e, Is.EqualTo(100));
    }

    [Test]
    public void セーブデータにFloat型の要素を追加する()
    {
        // foo: 3.14 という float 型のものを追加する
        var savedata = Store.InGame;
        savedata!.Commit("foo", 3.14f);

        // 要素追加後は値の取得ができる
        savedata.TryGetValue<float>("foo", out var e);
        Assert.That(e, Is.EqualTo(3.14f));
    }

    [Test]
    public void セーブデータにVector3型の要素を追加する()
    {
        // {1, 2, 3} の UnityEngine.Vector3 型を追加する
        var savedata = Store.InGame;
        savedata!.Commit("foo", new Vector3(1, 2, 3));

        // 要素追加後は値の取得ができる
        savedata.TryGetValue<Vector3>("foo", out var e);
        Assert.That(e, Is.EqualTo(new Vector3(1, 2, 3)));
    }

    [Test]
    public void セーブデータにQuaternion型の要素を追加する()
    {
        // {1, 2, 3, 4} の UnityEngine.Quaternion 型を追加する
        var savedata = Store.InGame;
        savedata!.Commit("foo", new Quaternion(1, 2, 3, 4));

        // 要素追加後は値の取得ができる
        savedata.TryGetValue<Quaternion>("foo", out var e);
        Assert.That(e, Is.EqualTo(new Quaternion(1, 2, 3, 4)));
    }

    [Test]
    public void セーブデータにString型の要素を追加する()
    {
        // foo: bar という string 型のものを追加する
        var savedata = Store.InGame;
        savedata!.Commit("foo", "bar");

        // 要素追加後は値の取得ができる
        savedata.TryGetValue("foo", out string? e);
        Assert.That(e, Is.EqualTo("bar"));
    }

    [Test]
    public void 保存した型と違う型を指定して取得しようとすると取得に失敗する()
    {
        // foo: true という bool 型のものを追加する
        var savedata = Store.InGame;
        savedata!.Commit("foo", true);

        // foo というキーだが int 型を指定して取得する
        var bSuccess = savedata.TryGetValue<int>("foo", out var e);
        // 取得に失敗する
        Assert.That(bSuccess, Is.False);
        // element には デフォルトの値が入っている
        Assert.That(e, Is.EqualTo(0));
    }

    [Test]
    public void 未保存の値を取得しようとすると取得に失敗する()
    {
        var savedata = Store.InGame;
        var bSuccess = savedata!.TryGetValue("foo", out int _);
        Assert.That(bSuccess, Is.False);
    }

    [Test]
    public void すでに存在している値と同じ型の値を追加すると上書きされる()
    {
        // foo: 3 という int 型のものを追加する
        var savedata = Store.InGame;
        savedata!.Commit("foo", 3);

        // foo: 10 という int 型のものを追加する
        savedata!.Commit("foo", 10);

        // 値を取得すると 10 に上書きされている
        savedata.TryGetValue("foo", out int e);
        Assert.That(e, Is.EqualTo(10));
    }

    [Test]
    public void すでに存在している値と違う型のものを追加しようとするとエラーが出る()
    {
        // foo: 3 という int 型のものを追加する
        var savedata = Store.InGame;
        savedata!.Commit("foo", 3);

        // foo: 5.6 という float 型のものを追加する
        // この時点でエラーが発生する
        Assert.Throws<System.ArgumentException>(() =>
        {
            savedata!.Commit("foo", 5.6f);
        });
    }

    [Test]
    public void 空文字のKeyをCommitしようとするとエラーがでる()
    {
        // Key が空白文字のコミットを行うとエラーが発生する
        var savedata = Store.InGame;
        Assert.Throws<System.ArgumentNullException>(() =>
        {
            savedata!.Commit("", 3);
        });

        // Key がnullのコミットを行うとエラーが出る
        savedata = Store.InGame;
        Assert.Throws<System.ArgumentNullException>(() =>
        {
            savedata!.Commit(null!, true);
        });
    }

    [Test]
    public void NotSerializableな型をコミットするとエラーが出る()
    {
        // NonSerializable な 型を生成する
        var notSer = new _TestClassNotSerializable();
        var savedata = Store.InGame;

        // Commitしようとするとエラーが発生する
        Assert.Throws<System.ArgumentException>(() =>
        {
            savedata!.Commit("foo", notSer);
        });
    }

    [Test]
    public void Serializableな型であればコミットできる()
    {
        // NonSerializable な 型を生成する
        var ser = new _TestClassSerializable();
        {
            ser.Foo = 3.14f;
            ser.Bar = 20;
        }
        var savedata = Store.InGame;

        // Commitできる
        savedata!.Commit("foo", ser);

        // 値の取得もできる
        bool bSuccess = savedata.TryGetValue("foo", out _TestClassSerializable? e);
        Assert.That(bSuccess, Is.True);
        Assert.That(e!.Foo, Is.EqualTo(3.14f));
        Assert.That(e!.Bar, Is.EqualTo(20));
    }

    /// <summary>
    /// テストケースで使用するクラス
    /// このクラスは Serializable ではない ため保存不可能
    /// </summary>
    public class _TestClassNotSerializable
    {
    }

    /// <summary>
    /// テストケースで使用するクラス
    /// このクラスは Serializable なので保存ができる
    /// </summary>
    [System.Serializable]
    public class _TestClassSerializable
    {
        public float Foo;
        public int Bar;
    }
}

public class SaveDataElementsの保存を絡めたテスト
{
    // このテストケースで使用される共通のセーブデータのパス
    static string SaveDataPath => Store.SaveFilenameToPath("fortesting");

    // 各テスト実行前に実行される
    [SetUp]
    public void SetUp()
    {
        // 新規にセーブを作成する
        Store.CreateAndLoadEmptyInGame();
    }

    // 各テスト実行後に実行される
    [TearDown]
    public void TearDown()
    {
        // 後続のために破棄しておく
        Store.UnloadInGame();
        // ファイルを削除しておく
        File.Delete(Store.SaveFilenameToPath("fortesting"));
    }

    [Test]
    public void Bool型のデータがロードできる()
    {
        // foo: true という bool 型のものを追加する
        var savedata = Store.InGame;
        savedata!.Commit("foo", true);

        // これを保存する
        Store.ManualSaveInGameAsync("fortesting");

        // 一度現在ロード中のセーブデータを破棄しておく
        Store.UnloadInGame();

        // 新規でセーブデータをロードする
        Store.LoadInGameAsync(SaveDataPath);

        // ロード後に先程保存した値がきちんと取得できる様になっている
        savedata = Store.InGame;
        var bSuccess = savedata!.TryGetValue("foo", out bool e);
        Assert.That(bSuccess, Is.True);
        Assert.That(e, Is.True);
    }

    [Test]
    public void int型のデータがロードできる()
    {
        // foo: 123 という int 型のものを追加する
        var savedata = Store.InGame;
        savedata!.Commit("foo", 123);

        // これを保存する
        Store.ManualSaveInGameAsync("fortesting");

        // 一度現在ロード中のセーブデータを破棄しておく
        Store.UnloadInGame();

        // 新規でセーブデータをロードする
        Store.LoadInGameAsync(SaveDataPath);

        // ロード後に先程保存した値がきちんと取得できる様になっている
        savedata = Store.InGame;
        var bSuccess = savedata!.TryGetValue("foo", out int e);
        Assert.That(bSuccess, Is.True);
        Assert.That(e, Is.EqualTo(123));
    }

    [Test]
    public void float型のデータがロードできる()
    {
        // foo: 3.14 という float 型のものを追加する
        var savedata = Store.InGame;
        savedata!.Commit("foo", 3.14f);

        // これを保存する
        Store.ManualSaveInGameAsync("fortesting");

        // 一度現在ロード中のセーブデータを破棄しておく
        Store.UnloadInGame();

        // 新規でセーブデータをロードする
        Store.LoadInGameAsync(SaveDataPath);

        // ロード後に先程保存した値がきちんと取得できる様になっている
        savedata = Store.InGame;
        var bSuccess = savedata!.TryGetValue("foo", out float e);
        Assert.That(bSuccess, Is.True);
        Assert.That(e, Is.EqualTo(3.14f));

    }

    [Test]
    public void string型のデータがロードできる()
    {
        // foo: bar という string 型のものを追加する
        var savedata = Store.InGame;
        savedata!.Commit("foo", "bar");

        // これを保存する
        Store.ManualSaveInGameAsync("fortesting");

        // 一度現在ロード中のセーブデータを破棄しておく
        Store.UnloadInGame();

        // 新規でセーブデータをロードする
        Store.LoadInGameAsync(SaveDataPath);

        // ロード後に先程保存した値がきちんと取得できる様になっている
        savedata = Store.InGame;
        var bSuccess = savedata!.TryGetValue("foo", out string? e);
        Assert.That(bSuccess, Is.True);
        Assert.That(e, Is.EqualTo("bar"));
    }

    [Test]
    public void Vector3型のデータがロードできる()
    {
        // foo: {0, 1, 2} という UnityEngine.Vector3 型のものを追加する
        var savedata = Store.InGame;
        savedata!.Commit("foo", new Vector3(0, 1, 2));

        // これを保存する
        Store.ManualSaveInGameAsync("fortesting");

        // 一度現在ロード中のセーブデータを破棄しておく
        Store.UnloadInGame();

        // 新規でセーブデータをロードする
        Store.LoadInGameAsync(SaveDataPath);

        // ロード後に先程保存した値がきちんと取得できる様になっている
        savedata = Store.InGame;
        var bSuccess = savedata!.TryGetValue("foo", out Vector3 e);
        Assert.That(bSuccess, Is.True);
        Assert.That(e, Is.EqualTo(new Vector3(0, 1, 2)));
    }

    [Test]
    public void ユーザー定義のSerializableなデータ型がロードできる()
    {
        // ユーザー定義型のクラスをCommit する
        var savedata = Store.InGame;
        var userData = new _UserData();
        {
            userData.Foo = 200f;
            userData.Bar = 145;
        }
        savedata!.Commit("foo", userData);

        // これを保存する
        Store.ManualSaveInGameAsync("fortesting");

        // 一度現在ロード中のセーブデータを破棄しておく
        Store.UnloadInGame();

        // 新規でセーブデータをロードする
        Store.LoadInGameAsync(SaveDataPath);

        // ロード後に先程保存した値がきちんと取得できる様になっている
        savedata = Store.InGame;
        var bSuccess = savedata!.TryGetValue("foo", out _UserData? e);
        Assert.That(bSuccess, Is.True);
        Assert.That(e!.Foo, Is.EqualTo(200f));
        Assert.That(e.Bar, Is.EqualTo(145));
    }

    /// <summary>
    /// テストケースで使用するクラス
    /// このクラスは Serializable なので保存ができる
    /// </summary>
    [System.Serializable]
    public class _UserData
    {
        public float Foo;
        public int Bar;
    }
}
