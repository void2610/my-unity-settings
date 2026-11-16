using TMPro;
using UnityEngine;

namespace Void2610.SettingsSystem
{
    /// <summary>
    /// 設定項目の 1 行。行の中のどの要素が選ばれても、行全体をフォーカス中として強調する
    /// 行のコンテナ Prefab に付けると <see cref="SettingsView"/> が使う (付けなければ強調しない)
    /// </summary>
    public sealed class SettingRowView : MonoBehaviour
    {
        [SerializeField] private CanvasGroup focusIndicator;
        [SerializeField] private Color labelColor = new(0.9f, 0.9f, 0.95f, 1f);
        [SerializeField] private Color focusedLabelColor = Color.white;

        private TextMeshProUGUI _label;

        public void SetLabel(TextMeshProUGUI label)
        {
            _label = label;
            SetFocused(false);
        }

        public void SetFocused(bool focused)
        {
            focusIndicator.alpha = focused ? 1f : 0f;
            if (_label) _label.color = focused ? focusedLabelColor : labelColor;
        }
    }
}
