using System.Collections.Generic;
using UnityEngine;

namespace Void2610.SettingsSystem
{
    /// <summary>
    /// 設定画面で、フォーカスのある行と今のカテゴリのタブをどう見せるか
    /// いつ変わったかは <see cref="SettingsView"/> が伝え、見せ方は派生が決める。既定の実装は <see cref="SettingsFocusCursor"/>
    /// </summary>
    public abstract class SettingsFocusViewBase : MonoBehaviour
    {
        /// <summary>
        /// フォーカスのある行が変わったとき。行の中のどの要素 (スライダー・列挙の前後ボタン等) が選ばれても同じ行を渡す
        /// タブや閉じるボタン、設定画面の外へ選択が移ったとき、選択が外れたとき、行を作り直すときは null
        /// </summary>
        public abstract void ShowFocusedRow(SettingsUiElement row);

        /// <summary>今のカテゴリが変わったとき。タブを作り直したときも全タブを渡し直す。タブが無い (カテゴリが 1 つ) ときは空と -1</summary>
        public abstract void ShowCurrentTab(IReadOnlyList<SettingsUiElement> tabs, int currentIndex);

        /// <summary>
        /// フォーカスのあるタブが変わったとき。index は <see cref="ShowCurrentTab"/> で渡したタブの番号で、タブ以外へ移ったときは -1
        /// 既定では何もしない (タブのボタンの Transition で示せる)。今のタブかどうかと合わせて見せ方を決めるときに使う
        /// </summary>
        public virtual void ShowFocusedTab(int index) { }

        /// <summary>
        /// target の中心に重なるときの cursor の anchoredPosition。cursor のアンカーは 1 点 (anchorMin == anchorMax) とする
        /// cursor を target と同じスクロールの中に置けば、スクロールしても重なったままになる
        /// </summary>
        protected static Vector2 AnchoredPositionOver(RectTransform cursor, RectTransform target)
        {
            var parent = (RectTransform)cursor.parent;
            Vector2 center = parent.InverseTransformPoint(target.TransformPoint(target.rect.center));
            var anchorPoint = parent.rect.min + Vector2.Scale(parent.rect.size, cursor.anchorMin);
            return center - anchorPoint + (cursor.pivot - new Vector2(0.5f, 0.5f)) * cursor.rect.size;
        }
    }
}
