using System;
using System.Collections.Generic;
using System.Linq;
using R3;
using UnityEngine;
using VContainer.Unity;

namespace Void2610.SettingsSystem
{
    public class SettingsManager : IStartable, IDisposable
    {
        public IReadOnlyList<SettingsCategory> Categories => _categories;
        public Observable<string> OnSettingChanged => _onSettingChanged;

        private const string SETTINGS_KEY = "game_settings";

        private SettingsCategory[] _categories;
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

        private void InitializeSettings()
        {
            _categories = _settingsDefinition.CreateCategories().ToArray();
            _settingsDefinition.BindSettingActions(_categories, _disposables);
        }

        private void SubscribeToSettingChanges()
        {
            foreach (var category in _categories)
            {
                foreach (var setting in category.Settings)
                {
                    setting.OnSettingChanged
                        .Subscribe(_ =>
                        {
                            _onSettingChanged.OnNext(setting.SettingName);
                            SaveSettings();
                        })
                        .AddTo(_disposables);
                }
            }
        }

        public T GetSetting<T>(string settingName) where T : class, ISettingBase
        {
            return _categories
                .SelectMany(c => c.Settings)
                .FirstOrDefault(s => s.SettingName == settingName) as T;
        }

        public void ResetAllSettings()
        {
            foreach (var category in _categories)
            {
                foreach (var setting in category.Settings)
                {
                    setting.ResetToDefault();
                }
            }
        }

        public void SaveSettings()
        {
            var settingsData = new SettingsData();

            foreach (var category in _categories)
            {
                foreach (var setting in category.Settings)
                {
                    settingsData.SetValue(setting.SettingName, setting.SerializeValue());
                }
            }

            var json = JsonUtility.ToJson(settingsData, true);
            DataPersistence.SaveData(SETTINGS_KEY, json);
        }

        public void LoadSettings()
        {
            var json = DataPersistence.LoadData(SETTINGS_KEY);
            if (string.IsNullOrEmpty(json)) return;

            var settingsData = JsonUtility.FromJson<SettingsData>(json);

            foreach (var category in _categories)
            {
                foreach (var setting in category.Settings)
                {
                    if (settingsData.TryGetValue(setting.SettingName, out var value))
                    {
                        setting.DeserializeValue(value);
                    }
                }
            }
        }

        private void ApplyCurrentValues()
        {
            foreach (var category in _categories)
            {
                foreach (var setting in category.Settings)
                {
                    setting.ApplyCurrentValue();
                }
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
