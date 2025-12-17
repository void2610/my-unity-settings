using System.Collections.Generic;
using R3;
using UnityEngine;
using UnityEngine.UI;

namespace Void2610.SettingsSystem
{
    /// <summary>
    /// スライダー形式の設定項目UI
    /// 左右ナビゲーションでスライダー値を変更
    /// </summary>
    [RequireComponent(typeof(Slider))]
    public sealed class SliderSettingItem : MonoBehaviour, ISettingItemNavigatable
    {
        public GameObject SelectableGameObject => _slider.gameObject;
        public IEnumerable<GameObject> AllSelectableGameObjects => new[] { _slider.gameObject };
        public Observable<(string settingName, object value)> OnValueChanged => _onValueChanged;

        private const float NAVIGATION_STEP = 0.05f;

        private readonly Subject<(string settingName, object value)> _onValueChanged = new();
        private Slider _slider;
        private string _settingName;
        private float _minValue;
        private float _maxValue;

        /// <summary>
        /// 設定項目を初期化
        /// </summary>
        public void Initialize(string settingName, float minValue, float maxValue, float currentValue)
        {
            _settingName = settingName;
            _minValue = minValue;
            _maxValue = maxValue;

            _slider = GetComponentInChildren<Slider>();

            // スライダーの設定
            _slider.minValue = minValue;
            _slider.maxValue = maxValue;
            _slider.value = currentValue;

            // スライダー変更イベントのリスニング
            _slider.OnValueChangedAsObservable()
                .Subscribe(v => _onValueChanged.OnNext((_settingName, v)))
                .AddTo(this);
        }

        /// <summary>
        /// 左右ナビゲーション（スライダー値の増減）
        /// </summary>
        public void OnNavigateHorizontal(float direction)
        {
            if (Mathf.Abs(direction) < 0.1f) return;

            var newValue = _slider.value + (direction > 0 ? NAVIGATION_STEP : -NAVIGATION_STEP);
            _slider.value = Mathf.Clamp(newValue, _minValue, _maxValue);
        }

        public void OnSubmit() { }

        private void OnDestroy()
        {
            _onValueChanged?.Dispose();
        }
    }
}
