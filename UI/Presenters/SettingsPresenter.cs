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
        private readonly bool _showDescriptions;
        private readonly CompositeDisposable _disposables = new();
        private CompositeDisposable _viewDisposables = new();
        private readonly Subject<Unit> _onShowRequested = new();
        private readonly Subject<Unit> _onHideRequested = new();

        public SettingsPresenter(
            SettingsManager settingsManager,
            IConfirmationDialog confirmationDialog,
            ISettingsInputProvider inputProvider,
            SettingsSystemOptions options = null)
        {
            _settingsManager = settingsManager;
            _confirmationDialog = confirmationDialog;
            _inputProvider = inputProvider;
            _showDescriptions = options?.ShowDescriptions ?? true;
        }

        public void Start()
        {
            // シーンのSettingsViewを取得して接続
            ConnectView(Object.FindFirstObjectByType<SettingsView>());

            // 入力プロバイダーのナビゲーションイベントを購読（グローバル）
            _inputProvider.OnNavigateHorizontal
                .Where(x => Mathf.Abs(x) > 0.1f)
                .Subscribe(x => _settingsView?.NavigateHorizontal(x))
                .AddTo(_disposables);
        }

        /// <summary>
        /// シーンのSettingsViewを接続する。シーン遷移時に呼び出す。
        /// </summary>
        public void ConnectView(SettingsView view)
        {
            // 既存のView購読を破棄して再生成
            _viewDisposables.Dispose();
            _viewDisposables = new CompositeDisposable();
            _settingsView = view;
            if (_settingsView == null) return;
            SubscribeToViewEvents();
        }

        /// <summary>
        /// 設定画面の表示を要求（SettingsManager の初期化完了を待機してから表示する）
        /// </summary>
        public async UniTaskVoid RequestShow()
        {
            await _settingsManager.WaitForInitializationAsync();
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
        /// ViewのイベントをSettingsManagerに接続（_viewDisposables に追加）
        /// </summary>
        private void SubscribeToViewEvents()
        {
            // スライダー変更イベント
            _settingsView.OnSliderChanged
                .Subscribe(data => {
                    var setting = _settingsManager.GetSetting<SliderSetting>(data.settingKey);
                    setting.CurrentValue = data.value;
                })
                .AddTo(_viewDisposables);

            // 列挙型変更イベント
            _settingsView.OnEnumChanged
                .Subscribe(data => {
                    var setting = _settingsManager.GetSetting<EnumSetting>(data.settingKey);
                    setting.CurrentValue = data.value;
                })
                .AddTo(_viewDisposables);

            // bool変更イベント
            _settingsView.OnBoolChanged
                .Subscribe(data => {
                    var setting = _settingsManager.GetSetting<BoolSetting>(data.settingKey);
                    setting.CurrentValue = data.value;
                })
                .AddTo(_viewDisposables);

            // ボタンクリックイベント
            _settingsView.OnButtonClicked
                .Subscribe(settingKey => {
                    var setting = _settingsManager.GetSetting<ButtonSetting>(settingKey);
                    setting.ExecuteAction();
                })
                .AddTo(_viewDisposables);

            // 閉じるボタンイベント
            _settingsView.OnCloseRequested
                .Subscribe(_ => _onHideRequested.OnNext(Unit.Default))
                .AddTo(_viewDisposables);
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
                key = setting.SettingKey,
                displayName = setting.SettingName,
                description = _showDescriptions ? (setting.Description ?? "") : ""
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

                case BoolSetting boolSetting:
                    data.type = SettingsView.SettingType.Bool;
                    data.boolValue = boolSetting.CurrentValue;
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
            _viewDisposables?.Dispose();
            _onShowRequested?.Dispose();
            _onHideRequested?.Dispose();
        }
    }
}
