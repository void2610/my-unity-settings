using R3;
using UnityEngine;

namespace Void2610.SettingsSystem
{
    /// <summary>
    /// 設定画面の入力操作を抽象化するインターフェース
    /// </summary>
    public interface ISettingsInputProvider
    {
        /// <summary>
        /// 設定画面のトグル操作（Pauseボタンなど）
        /// </summary>
        Observable<Unit> OnToggleSettings { get; }

        /// <summary>
        /// 水平方向のナビゲーション入力
        /// </summary>
        Observable<float> OnNavigateHorizontal { get; }
    }
}
