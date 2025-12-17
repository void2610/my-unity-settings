using R3;
using UnityEngine;

namespace Void2610.SettingsSystem
{
    /// <summary>
    /// 設定項目のナビゲーション操作を定義するインターフェース
    /// キーボード/ゲームパッドでの上下左右・決定操作を統一的に扱う
    /// </summary>
    internal interface ISettingItemNavigatable
    {
        /// <summary>
        /// EventSystemで選択可能なGameObject
        /// </summary>
        GameObject SelectableGameObject { get; }

        /// <summary>
        /// 左右のナビゲーション操作（スライダー値変更・enum切り替え）
        /// </summary>
        /// <param name="direction">方向（負: 左/減少、正: 右/増加）</param>
        void OnNavigateHorizontal(float direction);

        /// <summary>
        /// 決定操作（主にボタン実行用）
        /// </summary>
        void OnSubmit();

        /// <summary>
        /// 値が変更された時のイベント
        /// </summary>
        Observable<(string settingName, object value)> OnValueChanged { get; }
    }
}
