using TMPro;
using UnityEngine;

namespace Void2610.SettingsSystem
{
    /// <summary>
    /// カテゴリのタブ。フォーカスの有無と別に、今表示しているカテゴリのタブであることを常に示す
    /// タブの Prefab に付けると <see cref="SettingsView"/> が使う (付けなければボタンの通常色で示す)
    /// </summary>
    public sealed class SettingsTabView : MonoBehaviour
    {
        [SerializeField] private CanvasGroup currentIndicator;
        [SerializeField] private TextMeshProUGUI label;
        [SerializeField] private Color labelColor = new(0.9f, 0.9f, 0.95f, 1f);
        [SerializeField] private Color currentLabelColor = Color.white;

        public void SetCurrent(bool current)
        {
            currentIndicator.alpha = current ? 1f : 0f;
            label.color = current ? currentLabelColor : labelColor;
        }
    }
}
