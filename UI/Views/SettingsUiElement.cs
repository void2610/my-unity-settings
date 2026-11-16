using TMPro;
using UnityEngine;

namespace Void2610.SettingsSystem
{
    /// <summary>
    /// 設定画面の行やタブ 1 つ分。見せ方の実装 (<see cref="SettingsFocusViewBase"/>) が位置と名前の文字を使う
    /// </summary>
    public sealed class SettingsUiElement
    {
        public RectTransform Rect { get; }
        public TextMeshProUGUI Label { get; }

        public SettingsUiElement(RectTransform rect, TextMeshProUGUI label)
        {
            Rect = rect;
            Label = label;
        }
    }
}
