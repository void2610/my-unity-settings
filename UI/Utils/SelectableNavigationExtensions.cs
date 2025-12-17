using System.Collections.Generic;
using UnityEngine.UI;

namespace Void2610.SettingsSystem
{
    /// <summary>
    /// Selectableのナビゲーション設定用拡張メソッド
    /// </summary>
    internal static class SelectableNavigationExtensions
    {
        /// <summary>
        /// UI Selectableのリストにナビゲーションを設定
        /// </summary>
        public static void SetNavigation(this List<Selectable> selectables, bool isHorizontal = true, bool wrapAround = false)
        {
            if (selectables.Count == 0) return;

            for (int i = 0; i < selectables.Count; i++)
            {
                var selectable = selectables[i];
                if (!selectable) continue;

                var navigation = selectable.navigation;
                navigation.mode = UnityEngine.UI.Navigation.Mode.Explicit;

                if (isHorizontal)
                {
                    // 水平ナビゲーション
                    if (i > 0)
                        navigation.selectOnLeft = selectables[i - 1];
                    else if (wrapAround && selectables.Count > 1)
                        navigation.selectOnLeft = selectables[selectables.Count - 1];

                    if (i < selectables.Count - 1)
                        navigation.selectOnRight = selectables[i + 1];
                    else if (wrapAround && selectables.Count > 1)
                        navigation.selectOnRight = selectables[0];
                }
                else
                {
                    // 垂直ナビゲーション
                    if (i > 0)
                        navigation.selectOnUp = selectables[i - 1];
                    else if (wrapAround && selectables.Count > 1)
                        navigation.selectOnUp = selectables[selectables.Count - 1];

                    if (i < selectables.Count - 1)
                        navigation.selectOnDown = selectables[i + 1];
                    else if (wrapAround && selectables.Count > 1)
                        navigation.selectOnDown = selectables[0];
                }

                selectable.navigation = navigation;
            }
        }
    }
}
