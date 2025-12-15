using R3;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Void2610.SettingsSystem
{
    /// <summary>
    /// ボタン形式の設定項目UI
    /// Submit操作でボタンクリックを実行
    /// </summary>
    [RequireComponent(typeof(Button))]
    public class ButtonSettingItem : MonoBehaviour, ISettingItemNavigatable
    {
        public GameObject SelectableGameObject => _button.gameObject;
        public Observable<(string settingName, object value)> OnValueChanged => _onValueChanged;

        private readonly Subject<(string settingName, object value)> _onValueChanged = new();
        private string _settingName;
        private Button _button;

        /// <summary>
        /// 設定項目を初期化
        /// </summary>
        public void Initialize(string settingName, string buttonText)
        {
            _settingName = settingName;
            _button = GetComponent<Button>();
            _button.GetComponentInChildren<TextMeshProUGUI>().text = buttonText;

            // ボタンのイベント設定
            _button.OnClickAsObservable()
                .Subscribe(_ => _onValueChanged.OnNext((_settingName, Unit.Default)))
                .AddTo(this);
        }

        public void OnNavigateHorizontal(float direction) { }
        public void OnSubmit() => _onValueChanged.OnNext((_settingName, Unit.Default));

        private void OnDestroy()
        {
            _onValueChanged?.Dispose();
        }
    }
}
