using System;
using System.Collections.Generic;
using System.Linq;
using R3;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Cysharp.Threading.Tasks;
using UnityEngine.EventSystems;
using UINavigation = UnityEngine.UI.Navigation;

namespace Void2610.SettingsSystem
{
    /// <summary>
    /// 設定画面のUI表示を担当するViewクラス
    /// 設定項目の生成とイベント通知のみを担当
    /// ウィンドウの表示/非表示はプロジェクト側で管理
    /// </summary>
    public class SettingsView : MonoBehaviour
    {
        [Header("UI設定")]
        [SerializeField] private Transform settingsContainer;
        [SerializeField] private Button closeButton;

        [Header("設定項目プレハブ")]
        [SerializeField] private GameObject settingsContentContainerPrefab;
        [SerializeField] private GameObject titleTextPrefab;
        [SerializeField] private GameObject sliderSettingPrefab;
        [SerializeField] private GameObject buttonSettingPrefab;
        [SerializeField] private GameObject enumSettingPrefab;

        public Observable<(string settingName, float value)> OnSliderChanged => _onSliderChanged;
        public Observable<(string settingName, string value)> OnEnumChanged => _onEnumChanged;
        public Observable<string> OnButtonClicked => _onButtonClicked;
        public Observable<Unit> OnCloseRequested => _onCloseRequested;

        /// <summary>
        /// 最初の設定項目のGameObject（ナビゲーション用）
        /// </summary>
        public GameObject FirstSettingItem => _settingItems.Count > 0 ? _settingItems[0].SelectableGameObject : null;

        private readonly Subject<(string settingName, float value)> _onSliderChanged = new();
        private readonly Subject<(string settingName, string value)> _onEnumChanged = new();
        private readonly Subject<string> _onButtonClicked = new();
        private readonly Subject<Unit> _onCloseRequested = new();
        private readonly List<GameObject> _settingUIObjects = new();
        private readonly List<ISettingItemNavigatable> _settingItems = new();
        private readonly CompositeDisposable _disposables = new();
        private readonly CompositeDisposable _itemSubscriptions = new();
        private IConfirmationDialog _confirmationDialog;

        /// <summary>
        /// 設定データ構造体
        /// </summary>
        [Serializable]
        public struct SettingDisplayData
        {
            public string name;
            public string displayName;
            public SettingType type;
            public float floatValue;
            public string stringValue;
            public float minValue;
            public float maxValue;
            public string[] options;
            public string[] displayNames;
            public string buttonText;
            public bool requiresConfirmation;
            public string confirmationMessage;
        }

        public enum SettingType
        {
            Slider,
            Enum,
            Button
        }

        /// <summary>
        /// 外部から設定データを注入してUIを更新
        /// </summary>
        public void SetSettings(SettingDisplayData[] settingsData, IConfirmationDialog confirmationDialog)
        {
            _confirmationDialog = confirmationDialog;

            ClearSettingsUI();

            // 各設定項目のUIを生成
            foreach (var settingData in settingsData)
                CreateSettingUI(settingData);

            // Selectableのナビゲーションを設定
            SetupNavigation();
        }

        /// <summary>
        /// 左右ナビゲーション（現在フォーカス項目の操作）
        /// </summary>
        public void NavigateHorizontal(float direction)
        {
            var currentSelected = EventSystem.current.currentSelectedGameObject;
            var settingItem = currentSelected?.GetComponent<ISettingItemNavigatable>();
            settingItem?.OnNavigateHorizontal(direction);
        }

        /// <summary>
        /// 設定項目のUIを生成
        /// </summary>
        private void CreateSettingUI(SettingDisplayData settingData)
        {
            // 設定項目のコンテナを作成（横並び用）
            var containerObject = Instantiate(settingsContentContainerPrefab, settingsContainer);
            // タイトルテキストを作成（左側）
            CreateTitleText(containerObject.transform, settingData.displayName);

            // 設定固有のUIを作成（右側）
            switch (settingData.type)
            {
                case SettingType.Slider:
                    CreateSliderUI(settingData, containerObject.transform);
                    break;
                case SettingType.Button:
                    CreateButtonUI(settingData, containerObject.transform);
                    break;
                case SettingType.Enum:
                    CreateEnumUI(settingData, containerObject.transform);
                    break;
            }

            _settingUIObjects.Add(containerObject);
        }

        /// <summary>
        /// 確認ダイアログを表示
        /// </summary>
        private async UniTaskVoid ShowConfirmationDialog(SettingDisplayData settingData)
        {
            var result = await _confirmationDialog.ShowDialog(
                settingData.confirmationMessage,
                "実行"
            );

            if (result) _onButtonClicked.OnNext(settingData.name);
        }

        /// <summary>
        /// タイトルテキストを作成
        /// </summary>
        private void CreateTitleText(Transform parent, string titleText)
        {
            var titleObject = Instantiate(titleTextPrefab, parent);

            // プレハブからTextコンポーネントを取得してテキストを設定
            var textComponent = titleObject.GetComponentInChildren<TextMeshProUGUI>();
            textComponent.text = titleText;
            // レイアウト要素を追加してタイトル幅を固定
            if (!titleObject.TryGetComponent<LayoutElement>(out var layoutElement))
                layoutElement = titleObject.AddComponent<LayoutElement>();
            layoutElement.preferredWidth = 150f; // タイトルの固定幅
            layoutElement.flexibleWidth = 0f;    // 伸縮しない
        }

        /// <summary>
        /// スライダー設定のUIを生成
        /// </summary>
        private void CreateSliderUI(SettingDisplayData settingData, Transform parent)
        {
            var uiObject = Instantiate(sliderSettingPrefab, parent);
            var settingItem = uiObject.GetComponent<SliderSettingItem>();

            settingItem.Initialize(settingData.name, settingData.minValue, settingData.maxValue, settingData.floatValue);
            settingItem.OnValueChanged
                .Subscribe(data => _onSliderChanged.OnNext((data.settingName, (float)data.value)))
                .AddTo(_itemSubscriptions);

            _settingItems.Add(settingItem);
        }

        /// <summary>
        /// ボタン設定のUIを生成
        /// </summary>
        private void CreateButtonUI(SettingDisplayData settingData, Transform parent)
        {
            var uiObject = Instantiate(buttonSettingPrefab, parent);
            var settingItem = uiObject.GetComponent<ButtonSettingItem>();

            settingItem.Initialize(settingData.name, settingData.buttonText);
            settingItem.OnValueChanged
                .Subscribe(data => {
                    if (settingData.requiresConfirmation)
                        ShowConfirmationDialog(settingData).Forget();
                    else
                        _onButtonClicked.OnNext(data.settingName);
                })
                .AddTo(_itemSubscriptions);

            _settingItems.Add(settingItem);
        }

        /// <summary>
        /// Enum設定のUIを生成
        /// </summary>
        private void CreateEnumUI(SettingDisplayData settingData, Transform parent)
        {
            var uiObject = Instantiate(enumSettingPrefab, parent);
            var settingItem = uiObject.GetComponent<EnumSettingItem>();

            settingItem.Initialize(settingData.name, settingData.options, settingData.displayNames, settingData.stringValue);
            settingItem.OnValueChanged
                .Subscribe(data => _onEnumChanged.OnNext((data.settingName, (string)data.value)))
                .AddTo(_itemSubscriptions);

            _settingItems.Add(settingItem);
        }

        /// <summary>
        /// Selectableのナビゲーションを設定
        /// </summary>
        private void SetupNavigation()
        {
            var selectables = _settingItems
                .Select(item => item.SelectableGameObject.GetComponent<Selectable>())
                .Where(s => s)
                .ToList();

            if (selectables.Count == 0) return;

            // 垂直ナビゲーションを設定（isHorizontal=false, wrapAround=false）
            selectables.SetNavigation(isHorizontal: false, wrapAround: false);

            // closeButtonの下ナビゲーション → 最初の設定項目
            // closeButtonの上ナビゲーション → 最後の設定項目
            var closeButtonNav = closeButton.navigation;
            closeButtonNav.mode = UINavigation.Mode.Explicit;
            closeButtonNav.selectOnDown = selectables[0];
            closeButtonNav.selectOnUp = selectables[^1];
            closeButton.navigation = closeButtonNav;

            // 最初の設定項目の上ナビゲーション → closeButton
            var firstItemNav = selectables[0].navigation;
            firstItemNav.selectOnUp = closeButton;
            selectables[0].navigation = firstItemNav;

            // 最後の設定項目の下ナビゲーション → closeButton
            var lastItemNav = selectables[^1].navigation;
            lastItemNav.selectOnDown = closeButton;
            selectables[^1].navigation = lastItemNav;
        }

        /// <summary>
        /// 設定UIをクリア
        /// </summary>
        private void ClearSettingsUI()
        {
            _settingItems.Clear();

            // Subscriptionをクリア
            _itemSubscriptions.Clear();

            // UI要素をDestroy
            foreach (var uiObject in _settingUIObjects.Where(uiObject => uiObject))
                DestroyImmediate(uiObject);
            _settingUIObjects.Clear();
        }

        private void Awake()
        {
            closeButton.OnClickAsObservable()
                .Subscribe(_ => _onCloseRequested.OnNext(Unit.Default))
                .AddTo(_disposables);
        }

        private void OnDestroy()
        {
            _disposables.Dispose();
            _itemSubscriptions.Dispose();
            _onSliderChanged?.Dispose();
            _onEnumChanged?.Dispose();
            _onButtonClicked?.Dispose();
            _onCloseRequested?.Dispose();
        }
    }
}
