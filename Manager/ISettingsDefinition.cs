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
    }
}
