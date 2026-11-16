using System.Collections.Generic;
using UnityEngine;

namespace Void2610.SettingsSystem
{
    /// <summary>
    /// 見せ方の既定の実装。行とタブにそれぞれ 1 つのカーソルを重ね、名前の文字の色を変える。動きは付けず、その場で切り替える
    /// カーソルは行のコンテナ・タブのコンテナの最初の子に置き (後から並ぶ行やタブの下に描かれる)、LayoutElement の ignoreLayout を立てる
    /// </summary>
    public sealed class SettingsFocusCursor : SettingsFocusViewBase
    {
        [SerializeField] private RectTransform rowCursor;
        [SerializeField] private Color focusedLabelColor = Color.white;
        [SerializeField] private RectTransform tabCursor;
        [SerializeField] private Color tabLabelColor = new(0.9f, 0.9f, 0.95f, 1f);
        [SerializeField] private Color currentTabLabelColor = Color.white;

        private SettingsUiElement _focusedRow;
        private Color _focusedRowLabelColor;

        public override void ShowFocusedRow(SettingsUiElement row)
        {
            // 行を作り直すときは前の行が破棄済みなので、色を戻す相手がいない
            if (_focusedRow != null && _focusedRow.Label) _focusedRow.Label.color = _focusedRowLabelColor;
            _focusedRow = row;
            rowCursor.gameObject.SetActive(row != null);
            if (row == null) return;

            _focusedRowLabelColor = row.Label.color;
            row.Label.color = focusedLabelColor;
            rowCursor.sizeDelta = row.Rect.rect.size;
            rowCursor.anchoredPosition = AnchoredPositionOver(rowCursor, row.Rect);
        }

        public override void ShowCurrentTab(IReadOnlyList<SettingsUiElement> tabs, int currentIndex)
        {
            for (var i = 0; i < tabs.Count; i++)
                tabs[i].Label.color = i == currentIndex ? currentTabLabelColor : tabLabelColor;
            tabCursor.gameObject.SetActive(currentIndex >= 0 && currentIndex < tabs.Count);
            if (!tabCursor.gameObject.activeSelf) return;

            var tab = tabs[currentIndex].Rect;
            tabCursor.sizeDelta = tab.rect.size;
            tabCursor.anchoredPosition = AnchoredPositionOver(tabCursor, tab);
        }
    }
}
