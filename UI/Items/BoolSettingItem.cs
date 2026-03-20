using System.Collections.Generic;
using R3;
using UnityEngine;
using UnityEngine.UI;

namespace Void2610.SettingsSystem
{
    /// <summary>
    /// ボタン画像を切り替える2値 bool 設定UI。
    /// </summary>
    [RequireComponent(typeof(Button))]
    public sealed class BoolSettingItem : MonoBehaviour, ISettingItemNavigatable
    {
        [SerializeField] private Image targetImage;
        [SerializeField] private Sprite onSprite;
        [SerializeField] private Sprite offSprite;
        [SerializeField] private bool useNativeSize;

        public GameObject SelectableGameObject => _button.gameObject;
        public IEnumerable<GameObject> AllSelectableGameObjects => new[] { _button.gameObject };
        public Observable<(string settingName, object value)> OnValueChanged => _onValueChanged;

        private readonly Subject<(string settingName, object value)> _onValueChanged = new();
        private string _settingName;
        private bool _currentValue;
        private Button _button;

        public void Initialize(string settingName, bool currentValue)
        {
            _settingName = settingName;
            _currentValue = currentValue;
            _button = GetComponent<Button>();

            _button.OnClickAsObservable()
                .Subscribe(_ => Invert())
                .AddTo(this);

            RefreshVisual();
        }

        public void OnNavigateHorizontal(float direction)
        {
            if (Mathf.Abs(direction) >= 0.1f)
            {
                Invert();
            }
        }

        public void OnSubmit()
        {
            Invert();
        }

        private void Invert()
        {
            _currentValue = !_currentValue;
            RefreshVisual();
            _onValueChanged.OnNext((_settingName, _currentValue));
        }

        private void RefreshVisual()
        {
            if (!targetImage) return;

            targetImage.sprite = _currentValue ? onSprite : offSprite;
            if (useNativeSize && targetImage.sprite)
            {
                targetImage.SetNativeSize();
            }
        }

        private void OnDestroy()
        {
            _onValueChanged?.Dispose();
        }
    }
}
