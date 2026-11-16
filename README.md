# SettingsSystem

Unity向けの汎用設定画面システム。R3リアクティブプログラミングとVContainerを活用した、拡張性の高い設定管理パッケージ。

## 特徴

- **リアクティブ設計**: R3を使用した設定値の変更監視と自動反映
- **型安全な設定項目**: スライダー、ボタン、列挙型の3種類をサポート
- **永続化対応**: JSON形式でのデータ保存・読み込み
- **カスタマイズ可能**: プロジェクト固有の設定を`ISettingsDefinition`で定義
- **UIナビゲーション**: ゲームパッド対応の設定画面ナビゲーション

## 依存パッケージ

- [R3](https://github.com/Cysharp/R3) - リアクティブプログラミング
- [UniTask](https://github.com/Cysharp/UniTask) - 非同期処理
- [VContainer](https://github.com/hadashiA/VContainer) - 依存性注入
- TextMeshPro - UI表示

## インストール

### git submoduleとして追加

```bash
# リポジトリルートに追加
git submodule add https://github.com/void2610/my-unity-settings.git my-unity-settings

# Assets/Scripts/にシンボリックリンク作成
ln -s ../../my-unity-settings Assets/Scripts/SettingsSystem
```

## 使い方

### 1. 設定定義クラスの作成

プロジェクト固有の設定を`ISettingsDefinition`を実装して定義:

```csharp
public class GameSettingsDefinition : ISettingsDefinition
{
    public IEnumerable<ISettingBase> CreateSettings()
    {
        yield return new SliderSetting(
            name: "BGM音量",
            desc: "BGMの音量を設定します",
            defaultVal: 0.5f,
            min: 0f,
            max: 1f
        );

        yield return new EnumSetting(
            name: "画質",
            desc: "グラフィック品質を設定",
            opts: new[] { "low", "medium", "high" },
            defaultValue: "medium",
            displayNames: new[] { "低", "中", "高" }
        );

        yield return new ButtonSetting(
            name: "データ削除",
            desc: "セーブデータを削除",
            btnText: "削除",
            needsConfirmation: true,
            confirmMsg: "本当に削除しますか？"
        );
    }

    public void BindSettingActions(IReadOnlyList<ISettingBase> settings, CompositeDisposable disposables)
    {
        foreach (var setting in settings)
        {
            switch (setting)
            {
                case SliderSetting { SettingName: "BGM音量" } s:
                    s.OnSettingChanged
                        .Subscribe(_ => AudioManager.Instance.BgmVolume = s.CurrentValue)
                        .AddTo(disposables);
                    break;
            }
        }
    }
}
```

### 2. VContainerでの登録

```csharp
public class RootLifetimeScope : LifetimeScope
{
    protected override void Configure(IContainerBuilder builder)
    {
        // 設定システムの登録
        builder.Register<SettingsManager>(Lifetime.Singleton);
        builder.Register<GameSettingsDefinition>(Lifetime.Singleton).As<ISettingsDefinition>();
        builder.Register<DataPersistence>(Lifetime.Singleton);
    }
}
```

### 3. 確認ダイアログの実装

ボタン設定で確認が必要な場合、`IConfirmationDialog`を実装:

```csharp
public class ConfirmationDialogService : IConfirmationDialog
{
    public async UniTask<bool> ShowDialog(string message, string confirmText = null, string cancelText = null, bool destructive = false)
    {
        // ダイアログ表示ロジック
        return await ShowConfirmationUI(message, confirmText, cancelText, destructive);
    }
}
```

`destructive` は確定でデータが失われる確認 (`ButtonSetting` の `isDestructive` を立てたボタン等) で true になる。
誤って押しても安全なキャンセルを押されやすい位置に置く等、通常の確認と区別して出す。

### 4. フォーカスと今のカテゴリの見せ方

パッドやキーボードで動かしたとき、どこを選んでいるかが分かるように、`SettingsView` の `focusView` に見せ方の実装 (`SettingsFocusViewBase` の派生) を割り当てる。必須で、空だと最初の選択で例外になる。

いつ変わったかは `SettingsView` が決めて伝える。どう見せるかは実装が決める。

- **`ShowFocusedRow(row)`**: フォーカスのある行が変わったとき。行の中のどの要素 (スライダー・列挙の前後ボタン等) が選ばれても同じ行を渡す。タブや閉じるボタン、設定画面の外へ選択が移ったとき、選択が外れたとき、行を作り直すときは null
- **`ShowCurrentTab(tabs, currentIndex)`**: 今のカテゴリが変わったとき。全タブを渡す。フォーカスが他にあっても今のカテゴリは示し続ける
- 渡す行とタブは位置が確定している (並べ直してから渡す)。カーソルを重ねる位置は `AnchoredPositionOver` で求められる

既定の実装は **`SettingsFocusCursor`**。行とタブにそれぞれ 1 つのカーソルを重ね、名前の文字の色を変える。動きは付けず、その場で切り替える。カーソルは行のコンテナ・タブのコンテナの最初の子に置き、LayoutElement の ignoreLayout を立てる。

動き (カーソルを滑らせる・フェードさせる) や、名前以外の色を変える見せ方は、プロジェクトで `SettingsFocusViewBase` を派生させて書く。基盤はアニメーションのライブラリに依存しない。

## フォルダ構成

```
SettingsSystem/
├── Core/                    # 設定タイプ定義
│   ├── SettingBase.cs       # 基底クラス
│   ├── SliderSetting.cs     # スライダー設定
│   ├── ButtonSetting.cs     # ボタン設定
│   └── EnumSetting.cs       # 列挙型設定
├── Manager/
│   ├── SettingsManager.cs   # 設定管理
│   └── ISettingsDefinition.cs
├── Persistence/
│   └── DataPersistence.cs   # データ永続化
├── UI/
│   ├── Abstractions/        # インターフェース
│   ├── Items/               # 設定項目UI
│   ├── Presenters/          # MVP Presenter
│   ├── Utils/               # ユーティリティ
│   └── Views/               # MVP View
└── SettingsSystem.asmdef
```

## 設定タイプ

| タイプ | 用途 | 値の型 |
|--------|------|--------|
| `SliderSetting` | 数値調整（音量、感度等） | `float` |
| `EnumSetting` | 選択肢から選択 | `string` |
| `ButtonSetting` | アクション実行 | - |

## ライセンス

MIT License
