using System.Collections.Generic;
using R3;
using UnityEngine;

namespace Void2610.SettingsSystem
{
    /// <summary>
    /// 設定項目のナビゲーション操作を定義するインターフェース
    /// </summary>
    internal interface ISettingItemNavigatable
    {
        /// <summary>
        /// EventSystemで選択可能なGameObject（ナビゲーション設定用）
        /// </summary>
        GameObject SelectableGameObject { get; }

        /// <summary>
        /// 全ての選択可能なGameObject（説明文表示用）
        /// </summary>
        IEnumerable<GameObject> AllSelectableGameObjects { get; }

        void OnNavigateHorizontal(float direction);
        void OnSubmit();
        Observable<(string settingName, object value)> OnValueChanged { get; }
    }
}
