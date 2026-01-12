using System;
using System.Linq;
using Cysharp.Threading.Tasks;
using R3;
using UnityEngine;
using VContainer.Unity;
using Object = UnityEngine.Object;

namespace Void2610.SettingsSystem
{
    /// <summary>
    /// SettingsManagerとSettingsViewの橋渡しを行うPresenterクラス
    /// MVPパターンに基づいてViewとModelを分離
    /// ウィンドウの表示/非表示はプロジェクト側で管理
    /// </summary>
    public sealed class SettingsPresenter : IStartable, IDisposable
    {
        /// <summary>
        /// 設定画面の表示要求イベント
        /// </summary>
        public Observable<Unit> OnShowRequested => _onShowRequested;

        /// <summary>
        /// 設定画面の非表示要求イベント
        /// </summary>
        public Observable<Unit> OnHideRequested => _onHideRequested;

        /// <summary>
        /// SettingsViewインスタンス（プロジェクト側でのナビゲーション用）
        /// </summary>
        public SettingsView View => _settingsView;

        private SettingsView _settingsView;
        private readonly SettingsManager _settingsManager;
        private readonly IConfirmationDialog _confirmationDialog;
        private readonly ISettingsInputProvider _inputProvider;
        private readonly CompositeDisposable _disposables = new();
        private readonly Subject<Unit> _onShowRequested = new();
        private readonly Subject<Unit> _onHideRequested = new();

        public SettingsPresenter(
            SettingsManager settingsManager,
            IConfirmationDialog confirmationDialog,
            ISettingsInputProvider inputProvider)
        {
            _settingsManager = settingsManager;
            _confirmationDialog = confirmationDialog;
            _inputProvider = inputProvider;
        }

        public void Start()
        {
            _settingsView = Object.FindFirstObjectByType<SettingsView>();

            SubscribeInputEvents();
            SubscribeToViewEvents();
            InitializeAsync().Forget();
        }

        private async UniTaskVoid InitializeAsync()
        {
            await _settingsManager.WaitForInitializationAsync();
            RefreshSettingsView();
        }

        /// <summary>
        /// 設定画面の表示を要求
        /// </summary>
        public void RequestShow()
        {
            RefreshSettingsView();
            _onShowRequested.OnNext(Unit.Default);
        }

        /// <summary>
        /// 設定画面の非表示を要求
        /// </summary>
        public void RequestHide()
        {
            _onHideRequested.OnNext(Unit.Default);
        }

        /// <summary>
        /// 入力イベントの購読
        /// </summary>
        private void SubscribeInputEvents()
        {
            // 水平ナビゲーションの購読
            _inputProvider.OnNavigateHorizontal
                .Where(x => Mathf.Abs(x) > 0.1f)
                .Subscribe(x => _settingsView.NavigateHorizontal(x))
                .AddTo(_disposables);
        }

        /// <summary>
        /// ViewのイベントをSettingsManagerに接続
        /// </summary>
        private void SubscribeToViewEvents()
        {
            // スライダー変更イベント
            _settingsView.OnSliderChanged
                .Subscribe(data => {
                    var setting = _settingsManager.GetSetting<SliderSetting>(data.settingName);
                    setting.CurrentValue = data.value;
                })
                .AddTo(_disposables);

            // 列挙型変更イベント
            _settingsView.OnEnumChanged
                .Subscribe(data => {
                    var setting = _settingsManager.GetSetting<EnumSetting>(data.settingName);
                    setting.CurrentValue = data.value;
                })
                .AddTo(_disposables);

            // ボタンクリックイベント
            _settingsView.OnButtonClicked
                .Subscribe(settingName => {
                    var setting = _settingsManager.GetSetting<ButtonSetting>(settingName);
                    setting.ExecuteAction();
                })
                .AddTo(_disposables);

            // 閉じるボタンイベント
            _settingsView.OnCloseRequested
                .Subscribe(_ => _onHideRequested.OnNext(Unit.Default))
                .AddTo(_disposables);
        }

        private void RefreshSettingsView()
        {
            var categoriesData = _settingsManager.Categories
                .Select(ConvertToCategoryDisplayData)
                .ToArray();
            _settingsView.SetCategories(categoriesData, _confirmationDialog);
        }

        private SettingsView.CategoryDisplayData ConvertToCategoryDisplayData(SettingsCategory category)
        {
            return new SettingsView.CategoryDisplayData
            {
                name = category.Name,
                settings = category.Settings.Select(ConvertToSettingDisplayData).ToArray()
            };
        }

        private SettingsView.SettingDisplayData ConvertToSettingDisplayData(ISettingBase setting)
        {
            var data = new SettingsView.SettingDisplayData
            {
                name = setting.SettingName,
                displayName = setting.SettingName,
                description = setting.Description ?? ""
            };

            switch (setting)
            {
                case SliderSetting sliderSetting:
                    data.type = SettingsView.SettingType.Slider;
                    data.floatValue = sliderSetting.CurrentValue;
                    data.minValue = sliderSetting.MinValue;
                    data.maxValue = sliderSetting.MaxValue;
                    break;

                case EnumSetting enumSetting:
                    data.type = SettingsView.SettingType.Enum;
                    data.stringValue = enumSetting.CurrentValue;
                    data.options = enumSetting.Options;
                    data.displayNames = enumSetting.DisplayNames;
                    break;

                case ButtonSetting buttonSetting:
                    data.type = SettingsView.SettingType.Button;
                    data.buttonText = buttonSetting.ButtonText;
                    data.requiresConfirmation = buttonSetting.RequiresConfirmation;
                    data.confirmationMessage = buttonSetting.ConfirmationMessage;
                    break;
            }

            return data;
        }

        public void Dispose()
        {
            _disposables?.Dispose();
            _onShowRequested?.Dispose();
            _onHideRequested?.Dispose();
        }
    }
}
