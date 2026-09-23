using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using R3;

namespace Void2610.SettingsSystem
{
    public interface ISettingsDefinition
    {
        /// <summary>
        /// 設定定義の初期化完了を待機（ローカライズ等）
        /// </summary>
        UniTask WaitForInitializationAsync();

        IEnumerable<SettingsCategory> CreateCategories();
        void BindSettingActions(IReadOnlyList<SettingsCategory> categories, CompositeDisposable disposables);

        /// <summary>
        /// 表示名などが古くなり CreateCategories からやり直すべきときに発行する（ロケール変更等）
        /// </summary>
        Observable<Unit> OnCategoriesInvalidated => Observable.Empty<Unit>();
    }
}
