using System.Collections.Generic;
using R3;

namespace Void2610.SettingsSystem
{
    /// <summary>
    /// 設定定義のインターフェース
    /// 各プロジェクトで実装して、プロジェクト固有の設定を定義する
    /// </summary>
    public interface ISettingsDefinition
    {
        /// <summary>
        /// 設定項目を作成して返す
        /// </summary>
        /// <returns>設定項目のリスト</returns>
        IEnumerable<ISettingBase> CreateSettings();

        /// <summary>
        /// 設定値の変更をシステムに反映するバインディングを設定
        /// BGM音量やSE音量など、プロジェクト固有のシステムへの反映処理をここで行う
        /// </summary>
        /// <param name="settings">作成された設定項目のリスト</param>
        /// <param name="disposables">購読のDisposable管理用</param>
        void BindSettingActions(IReadOnlyList<ISettingBase> settings, CompositeDisposable disposables);
    }
}
