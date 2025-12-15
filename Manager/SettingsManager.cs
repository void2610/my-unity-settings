using System;
using System.Collections.Generic;
using System.Linq;
using R3;
using UnityEngine;
using VContainer.Unity;

namespace Void2610.SettingsSystem
{
    /// <summary>
    /// ゲーム設定の管理を行うサービスクラス
    /// VContainerでシングルトンとして注入される
    /// </summary>
    public class SettingsManager : IStartable, IDisposable
    {
        /// <summary>
        /// 全ての設定項目の読み取り専用リスト
        /// </summary>
        public IReadOnlyList<ISettingBase> Settings => _settings.AsReadOnly();

        /// <summary>
        /// 設定が変更された時のイベント
        /// </summary>
        public Observable<string> OnSettingChanged => _onSettingChanged;

        private const string SETTINGS_KEY = "game_settings";

        private readonly List<ISettingBase> _settings = new();
        private readonly Subject<string> _onSettingChanged = new();
        private readonly CompositeDisposable _disposables = new();
        private readonly ISettingsDefinition _settingsDefinition;

        public SettingsManager(ISettingsDefinition settingsDefinition)
        {
            _settingsDefinition = settingsDefinition;

            // 設定定義から設定項目を作成
            InitializeSettings();

            // 各設定の値変更イベントを監視
            SubscribeToSettingChanges();
        }

        /// <summary>
        /// 初期化処理（全てのMonoBehaviourのAwake完了後に実行）
        /// </summary>
        public void Start()
        {
            // セーブデータから設定を読み込む
            LoadSettings();
            ApplyCurrentValues();
        }

        /// <summary>
        /// 設定定義から設定を初期化
        /// </summary>
        private void InitializeSettings()
        {
            // 設定定義から設定項目を作成
            var settings = _settingsDefinition.CreateSettings();
            foreach (var setting in settings)
            {
                _settings.Add(setting);
            }

            // 設定値の変更をシステムに反映するバインディングを設定
            _settingsDefinition.BindSettingActions(_settings, _disposables);
        }

        /// <summary>
        /// 各設定の値変更イベントを監視（通知とセーブ処理）
        /// </summary>
        private void SubscribeToSettingChanges()
        {
            foreach (var setting in _settings)
            {
                setting.OnSettingChanged
                    .Subscribe(_ =>
                    {
                        _onSettingChanged.OnNext(setting.SettingName);
                        SaveSettings(); // 設定変更時に自動保存
                    })
                    .AddTo(_disposables);
            }
        }

        /// <summary>
        /// 設定名で設定項目を取得
        /// </summary>
        public T GetSetting<T>(string settingName) where T : class, ISettingBase
        {
            return _settings.FirstOrDefault(s => s.SettingName == settingName) as T;
        }

        /// <summary>
        /// すべての設定をデフォルト値にリセット
        /// </summary>
        public void ResetAllSettings()
        {
            foreach (var setting in _settings)
            {
                setting.ResetToDefault();
            }
        }

        /// <summary>
        /// 設定データをファイルに保存
        /// </summary>
        public void SaveSettings()
        {
            var settingsData = new SettingsData();

            foreach (var setting in _settings)
            {
                settingsData.SetValue(setting.SettingName, setting.SerializeValue());
            }

            var json = JsonUtility.ToJson(settingsData, true);
            DataPersistence.SaveData(SETTINGS_KEY, json);
        }

        /// <summary>
        /// ファイルから設定データを読み込み
        /// </summary>
        public void LoadSettings()
        {
            var json = DataPersistence.LoadData(SETTINGS_KEY);
            if (string.IsNullOrEmpty(json)) return;

            var settingsData = JsonUtility.FromJson<SettingsData>(json);

            foreach (var setting in _settings)
            {
                if (settingsData.TryGetValue(setting.SettingName, out var value))
                {
                    setting.DeserializeValue(value);
                }
            }
        }

        /// <summary>
        /// 現在の設定値を適用
        /// </summary>
        private void ApplyCurrentValues()
        {
            foreach (var setting in _settings)
            {
                setting.ApplyCurrentValue();
            }
        }

        /// <summary>
        /// リソース解放
        /// </summary>
        public void Dispose()
        {
            _disposables?.Dispose();
        }
    }

    /// <summary>
    /// 設定データのシリアライゼーション用クラス
    /// </summary>
    [Serializable]
    public class SettingsData
    {
        public List<SettingEntry> entries = new();

        public string GetValue(string key)
        {
            var entry = entries.Find(e => e.key == key);
            return entry?.value;
        }

        public void SetValue(string key, string value)
        {
            var entry = entries.Find(e => e.key == key);
            if (entry != null)
            {
                entry.value = value;
            }
            else
            {
                entries.Add(new SettingEntry { key = key, value = value });
            }
        }

        public bool TryGetValue(string key, out string value)
        {
            var entry = entries.Find(e => e.key == key);
            if (entry != null)
            {
                value = entry.value;
                return true;
            }
            value = null;
            return false;
        }
    }

    /// <summary>
    /// 設定エントリのシリアライゼーション用クラス
    /// </summary>
    [Serializable]
    public class SettingEntry
    {
        public string key;
        public string value;
    }
}
