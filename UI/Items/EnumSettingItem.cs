using System;
using R3;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Void2610.SettingsSystem
{
    /// <summary>
    /// Enum形式の設定項目UI
    /// 左右ナビゲーションで選択肢を切り替え
    /// </summary>
    public class EnumSettingItem : MonoBehaviour, ISettingItemNavigatable
    {
        [SerializeField] private Button prevButton;
        [SerializeField] private Button nextButton;
        [SerializeField] private TextMeshProUGUI valueText;

        public GameObject SelectableGameObject => gameObject;
        public Observable<(string settingName, object value)> OnValueChanged => _onValueChanged;

        private readonly Subject<(string settingName, object value)> _onValueChanged = new();
        private string _settingName;
        private string[] _options;
        private string[] _displayNames;
        private int _currentIndex;

        /// <summary>
        /// 設定項目を初期化
        /// </summary>
        public void Initialize(string settingName, string[] options, string[] displayNames, string currentValue)
        {
            _settingName = settingName;
            _options = options ?? Array.Empty<string>();
            _displayNames = displayNames ?? options ?? Array.Empty<string>();

            // 現在のインデックスを設定
            _currentIndex = Array.IndexOf(_options, currentValue);
            if (_currentIndex < 0) _currentIndex = 0;

            // ボタンのイベント設定
            prevButton.OnClickAsObservable()
                .Subscribe(_ => MovePrevious())
                .AddTo(this);
            nextButton.OnClickAsObservable()
                .Subscribe(_ => MoveNext())
                .AddTo(this);

            // 初期表示更新
            UpdateValueText();
        }

        /// <summary>
        /// 左右ナビゲーション（選択肢の切り替え）
        /// </summary>
        public void OnNavigateHorizontal(float direction)
        {
            if (Mathf.Abs(direction) < 0.1f) return;

            if (direction > 0)
                MoveNext();
            else
                MovePrevious();
        }

        /// <summary>
        /// 決定操作（Enumでは次の選択肢に移動）
        /// </summary>
        public void OnSubmit() => MoveNext();

        /// <summary>
        /// 次の選択肢に移動
        /// </summary>
        private void MoveNext()
        {
            if (_options.Length == 0) return;

            _currentIndex = (_currentIndex + 1) % _options.Length;
            UpdateValueText();
            NotifyValueChanged();
        }

        /// <summary>
        /// 前の選択肢に移動
        /// </summary>
        private void MovePrevious()
        {
            if (_options.Length == 0) return;

            _currentIndex = (_currentIndex - 1 + _options.Length) % _options.Length;
            UpdateValueText();
            NotifyValueChanged();
        }

        /// <summary>
        /// 値テキストの更新
        /// </summary>
        private void UpdateValueText()
        {
            if (_currentIndex >= 0 && _currentIndex < _displayNames.Length)
            {
                valueText.text = _displayNames[_currentIndex];
            }
        }

        /// <summary>
        /// 値変更を通知
        /// </summary>
        private void NotifyValueChanged()
        {
            if (_currentIndex >= 0 && _currentIndex < _options.Length)
            {
                var newValue = _options[_currentIndex];
                _onValueChanged.OnNext((_settingName, newValue));
            }
        }

        private void OnDestroy()
        {
            _onValueChanged?.Dispose();
        }
    }
}
