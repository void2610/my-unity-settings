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

        [Header("タブUI")]
        [SerializeField] private Transform tabContainer;
        [SerializeField] private GameObject tabButtonPrefab;

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
        /// 現在のカテゴリの最初の設定項目のGameObject（ナビゲーション用）
        /// </summary>
        public GameObject FirstSettingItem
        {
            get
            {
                if (string.IsNullOrEmpty(_currentCategory) ||
                    !_categorySettingItems.TryGetValue(_currentCategory, out var items) ||
                    items.Count == 0)
                    return null;
                return items[0].SelectableGameObject;
            }
        }

        private readonly Subject<(string settingName, float value)> _onSliderChanged = new();
        private readonly Subject<(string settingName, string value)> _onEnumChanged = new();
        private readonly Subject<string> _onButtonClicked = new();
        private readonly Subject<Unit> _onCloseRequested = new();
        private readonly List<GameObject> _settingUIObjects = new();
        private readonly List<ISettingItemNavigatable> _settingItems = new();
        private readonly List<Button> _tabButtons = new();
        private readonly Dictionary<string, List<GameObject>> _categoryContainers = new();
        private readonly Dictionary<string, List<ISettingItemNavigatable>> _categorySettingItems = new();
        private readonly CompositeDisposable _disposables = new();
        private readonly CompositeDisposable _itemSubscriptions = new();
        private readonly CompositeDisposable _tabSubscriptions = new();
        private IConfirmationDialog _confirmationDialog;
        private string _currentCategory;
        private string[] _categories;

        [Serializable]
        public struct CategoryDisplayData
        {
            public string name;
            public SettingDisplayData[] settings;
        }

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

        public void SetCategories(CategoryDisplayData[] categoriesData, IConfirmationDialog confirmationDialog)
        {
            _confirmationDialog = confirmationDialog;

            // 現在のカテゴリを保存（再表示時に復元するため）
            var previousCategory = _currentCategory;
            _currentCategory = null;

            ClearSettingsUI();

            _categories = categoriesData.Select(c => c.name).ToArray();

            // タブUIを生成（複数カテゴリがある場合のみ）
            if (_categories.Length > 1 && tabContainer && tabButtonPrefab)
            {
                CreateTabs();
            }

            // カテゴリごとに設定項目を生成
            foreach (var categoryData in categoriesData)
            {
                _categoryContainers[categoryData.name] = new List<GameObject>();
                _categorySettingItems[categoryData.name] = new List<ISettingItemNavigatable>();

                foreach (var settingData in categoryData.settings)
                {
                    CreateSettingUI(settingData, categoryData.name);
                }
            }

            // 前回のカテゴリを復元、存在しなければ最初のカテゴリを表示
            if (_categories.Length > 0)
            {
                var targetCategory = _categories.Contains(previousCategory) ? previousCategory : _categories[0];
                SwitchCategory(targetCategory);
            }

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
        /// タブUIを生成
        /// </summary>
        private void CreateTabs()
        {
            for (var i = 0; i < _categories.Length; i++)
            {
                var category = _categories[i];
                var tabObject = Instantiate(tabButtonPrefab, tabContainer);
                var tabButton = tabObject.GetComponent<Button>();
                var tabText = tabObject.GetComponentInChildren<TextMeshProUGUI>();

                if (tabText)
                {
                    tabText.text = category;
                }

                var capturedCategory = category;
                tabButton.OnClickAsObservable()
                    .Subscribe(_ => SwitchCategory(capturedCategory))
                    .AddTo(_tabSubscriptions);

                _tabButtons.Add(tabButton);
            }

            // タブボタンの横ナビゲーションを設定
            SetupTabNavigation();
        }

        /// <summary>
        /// タブボタンのナビゲーションを設定
        /// </summary>
        private void SetupTabNavigation()
        {
            if (_tabButtons.Count == 0) return;

            var selectables = _tabButtons.Select(b => (Selectable)b).ToList();
            selectables.SetNavigation(isHorizontal: true, wrapAround: true);
        }

        /// <summary>
        /// カテゴリを切り替え
        /// </summary>
        private void SwitchCategory(string category)
        {
            if (_currentCategory == category) return;

            _currentCategory = category;

            // 全カテゴリの設定項目を非表示
            foreach (var containers in _categoryContainers.Values)
            {
                foreach (var container in containers)
                {
                    if (container) container.SetActive(false);
                }
            }

            // 選択カテゴリの設定項目を表示
            if (_categoryContainers.TryGetValue(category, out var visibleContainers))
            {
                foreach (var container in visibleContainers)
                {
                    if (container) container.SetActive(true);
                }
            }

            // タブボタンの見た目を更新
            UpdateTabButtonVisuals();

            // ナビゲーションを再設定
            SetupNavigation();
        }

        /// <summary>
        /// タブボタンの見た目を更新（選択状態の表示）
        /// </summary>
        private void UpdateTabButtonVisuals()
        {
            for (var i = 0; i < _tabButtons.Count && i < _categories.Length; i++)
            {
                var isSelected = _categories[i] == _currentCategory;
                var colors = _tabButtons[i].colors;
                colors.normalColor = isSelected ? new Color(0.8f, 0.8f, 0.8f, 1f) : Color.white;
                _tabButtons[i].colors = colors;
            }
        }

        /// <summary>
        /// 設定項目のUIを生成
        /// </summary>
        private void CreateSettingUI(SettingDisplayData settingData, string category)
        {
            // 設定項目のコンテナを作成（横並び用）
            var containerObject = Instantiate(settingsContentContainerPrefab, settingsContainer);
            // タイトルテキストを作成（左側）
            CreateTitleText(containerObject.transform, settingData.displayName);

            // 設定固有のUIを作成（右側）
            ISettingItemNavigatable settingItem = settingData.type switch
            {
                SettingType.Slider => CreateSliderUI(settingData, containerObject.transform),
                SettingType.Button => CreateButtonUI(settingData, containerObject.transform),
                SettingType.Enum => CreateEnumUI(settingData, containerObject.transform),
                _ => null
            };

            if (settingItem != null)
            {
                _settingItems.Add(settingItem);
                _categorySettingItems[category].Add(settingItem);
            }

            _settingUIObjects.Add(containerObject);
            _categoryContainers[category].Add(containerObject);
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
        private ISettingItemNavigatable CreateSliderUI(SettingDisplayData settingData, Transform parent)
        {
            var uiObject = Instantiate(sliderSettingPrefab, parent);
            var settingItem = uiObject.GetComponent<SliderSettingItem>();

            settingItem.Initialize(settingData.name, settingData.minValue, settingData.maxValue, settingData.floatValue);
            settingItem.OnValueChanged
                .Subscribe(data => _onSliderChanged.OnNext((data.settingName, (float)data.value)))
                .AddTo(_itemSubscriptions);

            return settingItem;
        }

        /// <summary>
        /// ボタン設定のUIを生成
        /// </summary>
        private ISettingItemNavigatable CreateButtonUI(SettingDisplayData settingData, Transform parent)
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

            return settingItem;
        }

        /// <summary>
        /// Enum設定のUIを生成
        /// </summary>
        private ISettingItemNavigatable CreateEnumUI(SettingDisplayData settingData, Transform parent)
        {
            var uiObject = Instantiate(enumSettingPrefab, parent);
            var settingItem = uiObject.GetComponent<EnumSettingItem>();

            settingItem.Initialize(settingData.name, settingData.options, settingData.displayNames, settingData.stringValue);
            settingItem.OnValueChanged
                .Subscribe(data => _onEnumChanged.OnNext((data.settingName, (string)data.value)))
                .AddTo(_itemSubscriptions);

            return settingItem;
        }

        /// <summary>
        /// Selectableのナビゲーションを設定
        /// </summary>
        private void SetupNavigation()
        {
            // 現在のカテゴリの設定項目のみを対象にする（activeInHierarchyではなくカテゴリで判定）
            if (string.IsNullOrEmpty(_currentCategory) ||
                !_categorySettingItems.TryGetValue(_currentCategory, out var categoryItems))
                return;

            var visibleSelectables = categoryItems
                .Where(item => item.SelectableGameObject)
                .Select(item => item.SelectableGameObject.GetComponent<Selectable>())
                .Where(s => s)
                .ToList();

            if (visibleSelectables.Count == 0) return;

            // 垂直ナビゲーションを設定（isHorizontal=false, wrapAround=false）
            visibleSelectables.SetNavigation(isHorizontal: false, wrapAround: false);

            // タブがある場合はタブからのナビゲーションも設定
            if (_tabButtons.Count > 0)
            {
                // タブボタンの下ナビゲーション → 最初の設定項目
                foreach (var tabButton in _tabButtons)
                {
                    var tabNav = tabButton.navigation;
                    tabNav.selectOnDown = visibleSelectables[0];
                    tabButton.navigation = tabNav;
                }

                // 最初の設定項目の上ナビゲーション → 現在選択中のタブ
                var currentTabIndex = Array.IndexOf(_categories, _currentCategory);
                if (currentTabIndex >= 0 && currentTabIndex < _tabButtons.Count)
                {
                    var firstItemNav = visibleSelectables[0].navigation;
                    firstItemNav.selectOnUp = _tabButtons[currentTabIndex];
                    visibleSelectables[0].navigation = firstItemNav;
                }
            }
            else
            {
                // タブがない場合はcloseButtonとのナビゲーション
                var closeButtonNav = closeButton.navigation;
                closeButtonNav.mode = UINavigation.Mode.Explicit;
                closeButtonNav.selectOnDown = visibleSelectables[0];
                closeButtonNav.selectOnUp = visibleSelectables[^1];
                closeButton.navigation = closeButtonNav;

                // 最初の設定項目の上ナビゲーション → closeButton
                var firstItemNav = visibleSelectables[0].navigation;
                firstItemNav.selectOnUp = closeButton;
                visibleSelectables[0].navigation = firstItemNav;
            }

            // 最後の設定項目の下ナビゲーション → closeButton
            var lastItemNav = visibleSelectables[^1].navigation;
            lastItemNav.selectOnDown = closeButton;
            visibleSelectables[^1].navigation = lastItemNav;

            // closeButtonの上ナビゲーション → 最後の設定項目
            var closeNav = closeButton.navigation;
            closeNav.mode = UINavigation.Mode.Explicit;
            closeNav.selectOnUp = visibleSelectables[^1];
            closeButton.navigation = closeNav;
        }

        /// <summary>
        /// 設定UIをクリア
        /// </summary>
        private void ClearSettingsUI()
        {
            _settingItems.Clear();

            // Subscriptionをクリア
            _itemSubscriptions.Clear();
            _tabSubscriptions.Clear();

            // タブボタンをDestroy
            foreach (var tabButton in _tabButtons.Where(t => t))
                DestroyImmediate(tabButton.gameObject);
            _tabButtons.Clear();

            // カテゴリコンテナをクリア
            _categoryContainers.Clear();
            _categorySettingItems.Clear();

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
            _tabSubscriptions.Dispose();
            _onSliderChanged?.Dispose();
            _onEnumChanged?.Dispose();
            _onButtonClicked?.Dispose();
            _onCloseRequested?.Dispose();
        }
    }
}
