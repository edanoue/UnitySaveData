#nullable enable

using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using Edanoue.SaveData;

public class StoreのUnitテスト
{
    [Test]
    public void デフォルトのSaveDataPathがPersistanceDataPathのSaveディレクトリである()
    {
        var basePath = Application.persistentDataPath;
        Assert.That(Store.SavePath, Is.EqualTo($"{basePath}/Save"));
    }

    [Test]
    public void ロードしていない状態だと現在読み込み中のセーブデータがnullで帰ってくる()
    {
        var savedata = Store.InGame;
        Assert.That(savedata, Is.Null);
    }

    [Test]
    public void セーブデータを保存するとファイルが書き出される()
    {
        // テスト用に新規でセーブデータを保存する
        Store.CreateAndLoadEmptyInGame();
        Store.ManualSaveInGameAsync("fortest");

        var savedatapaths = Store.GetAllMetaDataAbsolutePaths();
        // 今回新しく保存しているので, 最低でも一つ以上のセーブデータが保存されている
        Assert.That(savedatapaths.Length, Is.GreaterThan(0));

        // 以下のセーブデータファイルが保存されている
        Assert.That(File.Exists(Store.SaveFilenameToPath("fortest")), Is.True);

        // 後続のためにアンロードしておく
        Store.UnloadInGame();
        // ファイルを削除しておく
        File.Delete(Store.SaveFilenameToPath("fortest"));
    }

    [Test]
    public void 新規にセーブデータを作成する()
    {
        // 新規にセーブを作成する
        Store.CreateAndLoadEmptyInGame();

        // 読込中のセーブデータが存在している
        var savedata = Store.InGame;
        Assert.That(savedata, Is.Not.Null);
        Store.UnloadInGame();
    }

    [Test]
    public void セーブデータをアンロードする()
    {
        // 新規にセーブを作成する
        Store.CreateAndLoadEmptyInGame();
        // Clear する と 読込中のセーブデータが存在していない
        Store.UnloadInGame();
        var savedata = Store.InGame;
        Assert.That(savedata, Is.Null);
    }

    [Test]
    public void セーブデータに要素を追加する()
    {
        // 新規にセーブを作成する
        Store.CreateAndLoadEmptyInGame();
        var savedata = Store.InGame;

        // foo という int型の要素を取り扱う
        // はじめは空なので何も存在していない
        Assert.That(savedata!.TryGetValue<int>("foo", out var notSavedElem), Is.False);
        // int の default が代入されている
        Assert.That(notSavedElem, Is.EqualTo(0));

        // セーブに要素を追加する
        // foo: 100 という int 型のものを追加する
        savedata.Commit("foo", 100);

        // 要素追加後は値の取得ができる
        var bSuccess = savedata.TryGetValue<int>("foo", out var loadElemA);
        Assert.That(bSuccess, Is.True);
        Assert.That(loadElemA, Is.EqualTo(100));

        // 後続のために破棄しておく
        Store.UnloadInGame();
    }

    [Test]
    public void セーブデータの事前読み込みを行う事ができる()
    {
        // 新規にセーブを作成する
        Store.CreateAndLoadEmptyInGame();

        // 後々の検証のためにコミットしておく
        Store.InGame?.Commit("foo", "bar");

        // 2つのファイルに保存しておく
        Store.ManualSaveInGameAsync("fortest1");
        Store.ManualSaveInGameAsync("fortest2");

        // 後続のためにアンロードしておく
        Store.UnloadInGame();

        // 弱いアクセス(Header アクセス) を行った すべてのセーブデータを取得する
        var isSuccess = Store.LoadAllInGamesHeaderOnly(out var headerOnlySaveDatas);
        Assert.That(isSuccess, Is.True);

        // 最新のセーブデータを取得する
        SaveDataRuntime? targetSaveData = null;
        foreach (var hs in headerOnlySaveDatas)
        {
            // 必ず全てのセーブデータのBodyはロード済みではない
            Assert.That(hs.IsExistedBody, Is.False);

            // ファイル名にアクセスして, 今回生成したファイルでなければスキップする
            var filename = hs.FileNameWhenLoading;
            if (filename != "fortest1" && filename != "fortest2")
            {
                continue;
            }

            // Header へのアクセスが可能である
            var header = hs.Header;

            // 保存時刻にアクセスできる
            // これを比較することで最後に保存されたセーブデータにアクセスする
            var created = header.SavedTimeLocal;

            // 先ほど作成したやつなので, 日付が今日と同じことを確認する
            // FIXME: 日付変更のびみょーーーな時間に実行されると落ちるぞ
            var today = System.DateTime.Now.Date;
            Assert.That(created.Date, Is.EqualTo(today));

            // すべて AutoSave ではない
            var isAutosave = header.IsAutoSave;
            Assert.That(isAutosave, Is.False);

            // 後続でロードするために, fortest2 をキャッシュしておく
            if (filename == "fortest2")
            {
                targetSaveData = hs;
            }
        }

        // セーブデータをロードする事ができる
        Store.LoadInGameFromHeaderOnly(targetSaveData!);

        // 今ロードしているセーブデータを取得する
        var currentInGame = Store.InGame!;

        // ファイル名は "fortest2" である
        Assert.That(currentInGame.FileNameWhenLoading, Is.EqualTo("fortest2"));

        // テストケースの先頭で代入した値が取得できる
        {
            currentInGame.TryGetValue<string>("foo", out var e);
            Assert.That(e, Is.EqualTo("bar"));
        }

        // 後続のためにアンロードしておく
        Store.UnloadInGame();

        // ファイルを削除しておく
        File.Delete(Store.SaveFilenameToPath("fortest1"));
        File.Delete(Store.SaveFilenameToPath("fortest2"));
    }

    [Test]
    public void 新規作成したセーブデータの場合ファイル名が空欄である()
    {
        // 新規にセーブを作成する
        Store.CreateAndLoadEmptyInGame();

        // ロードしたセーブデータではないので, ロード時のファイル名が空欄である
        Assert.That(Store.InGame!.FileNameWhenLoading, Is.EqualTo(""));

        // 後続のためにアンロードしておく
        Store.UnloadInGame();
    }

    [Test]
    public void ロードしたセーブデータの場合ファイル名が取得できる()
    {
        // 新規にセーブを作成する
        Store.CreateAndLoadEmptyInGame();
        // fortest という名前で保存する
        Store.ManualSaveInGameAsync("fortest");

        // この時点ではあくまで新規に保存したセーブデータがもととなっているため, ファイル名は空欄である
        Assert.That(Store.InGame!.FileNameWhenLoading, Is.EqualTo(""));

        // 一度アンロードする
        Store.UnloadInGame();

        // すべてのセーブデータを取得する
        Store.LoadAllInGamesHeaderOnly(out var savedatas);

        // 作成した fortest というセーブデータを見つける
        SaveDataRuntime targetSaveData = savedatas.First(s => s.FileNameWhenLoading == "fortest");

        // セーブデータをロードする
        Store.LoadInGameFromHeaderOnly(targetSaveData);

        // ファイル名は "fortest" である
        Assert.That(Store.InGame!.FileNameWhenLoading, Is.EqualTo("fortest"));

        // 後続のためにアンロードしておく
        Store.UnloadInGame();

        // ファイルを削除しておく
        File.Delete(Store.SaveFilenameToPath("fortest"));
    }

}
