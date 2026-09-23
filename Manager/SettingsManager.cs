using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using R3;
using UnityEngine;
using VContainer.Unity;

namespace Void2610.SettingsSystem
{
    public sealed class SettingsManager : IStartable, IDisposable
    {
        public IReadOnlyList<SettingsCategory> Categories => _categories;
        public bool IsInitialized { get; private set; }

        private const string SETTINGS_KEY = "game_settings";

        private SettingsCategory[] _categories;
        private readonly Subject<string> _onSettingChanged = new();
        private readonly CompositeDisposable _disposables = new();
        private CompositeDisposable _categoryDisposables = new();
        private readonly ISettingsDefinition _settingsDefinition;

        public SettingsManager(ISettingsDefinition settingsDefinition)
        {
            _settingsDefinition = settingsDefinition;
        }

        /// <summary>
        /// 初期化処理（全てのMonoBehaviourのAwake完了後に実行）
        /// </summary>
        public void Start()
        {
            InitializeAsync().Forget();
        }

        private async UniTaskVoid InitializeAsync()
        {
            // 設定定義の初期化待機（ローカライズ等）
            await _settingsDefinition.WaitForInitializationAsync();

            // 設定定義から設定項目を作成
            InitializeSettings();

            // 各設定の値変更イベントを監視
            SubscribeToSettingChanges();

            // セーブデータから設定を読み込む
            LoadSettings();
            ApplyCurrentValues();

            _settingsDefinition.OnCategoriesInvalidated
                .Subscribe(_ => RebuildCategories())
                .AddTo(_disposables);

            IsInitialized = true;
        }

        /// <summary>
        /// 設定定義から設定項目を作り直す。現在の値は引き継ぐ
        /// </summary>
        public void RebuildCategories()
        {
            var currentValues = _categories
                .SelectMany(c => c.Settings)
                .ToDictionary(s => s.SettingKey, s => s.SerializeValue());

            _categoryDisposables.Dispose();
            _categoryDisposables = new CompositeDisposable();

            // 購読前に値を戻し、引き継ぎを変更イベントとして扱わせない
            _categories = _settingsDefinition.CreateCategories().ToArray();
            foreach (var setting in _categories.SelectMany(c => c.Settings))
            {
                if (currentValues.TryGetValue(setting.SettingKey, out var value))
                {
                    setting.DeserializeValue(value);
                }
            }

            _settingsDefinition.BindSettingActions(_categories, _categoryDisposables);
            SubscribeToSettingChanges();
            ApplyCurrentValues();
        }

        public async UniTask WaitForInitializationAsync()
        {
            await UniTask.WaitUntil(() => IsInitialized);
        }

        /// <summary>
        /// 保存済みの設定値を SettingsManager の初期化を待たずに読む。
        /// 起動時ロケール選択のように、設定項目が生成される前に保存値が要る箇所から使う
        /// </summary>
        /// <returns>保存されていない / 壊れている場合は false</returns>
        public static bool TryLoadSavedValue<T>(string settingKey, out T value)
        {
            value = default;
            var json = DataPersistence.LoadData(SETTINGS_KEY);
            if (string.IsNullOrEmpty(json)) return false;

            try
            {
                var settingsData = JsonUtility.FromJson<SettingsData>(json);
                if (settingsData == null || !settingsData.TryGetValue(settingKey, out var serialized)) return false;
                if (string.IsNullOrEmpty(serialized)) return false;

                var data = JsonUtility.FromJson<SerializableValue<T>>(serialized);
                if (data == null) return false;

                value = data.value;
                return true;
            }
            catch (ArgumentException e)
            {
                Debug.LogError($"保存済み設定の解析に失敗しました ({settingKey}): {e.Message}");
                return false;
            }
        }

        private void InitializeSettings()
        {
            _categories = _settingsDefinition.CreateCategories().ToArray();
            _settingsDefinition.BindSettingActions(_categories, _categoryDisposables);
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
                            _onSettingChanged.OnNext(setting.SettingKey);
                            SaveSettings();
                        })
                        .AddTo(_categoryDisposables);
                }
            }
        }

        public T GetSetting<T>(string settingKey) where T : class, ISettingBase
        {
            return _categories
                .SelectMany(c => c.Settings)
                .FirstOrDefault(s => s.SettingKey == settingKey) as T;
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

        private void SaveSettings()
        {
            var settingsData = new SettingsData();

            foreach (var category in _categories)
            {
                foreach (var setting in category.Settings)
                {
                    settingsData.SetValue(setting.SettingKey, setting.SerializeValue());
                }
            }

            var json = JsonUtility.ToJson(settingsData, true);
            DataPersistence.SaveData(SETTINGS_KEY, json);
        }

        private void LoadSettings()
        {
            var json = DataPersistence.LoadData(SETTINGS_KEY);
            if (string.IsNullOrEmpty(json)) return;

            var settingsData = JsonUtility.FromJson<SettingsData>(json);

            foreach (var category in _categories)
            {
                foreach (var setting in category.Settings)
                {
                    if (settingsData.TryGetValue(setting.SettingKey, out var value))
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
            _categoryDisposables?.Dispose();
            _disposables?.Dispose();
        }
    }

    /// <summary>
    /// 設定データのシリアライゼーション用クラス
    /// </summary>
    [Serializable]
    internal sealed class SettingsData
    {
        public List<SettingEntry> entries = new();

        public string GetValue(string key) => Find(key)?.value;

        public void SetValue(string key, string value)
        {
            // 壊れた JSON から復元すると entries が null になりうる
            entries ??= new List<SettingEntry>();

            var entry = Find(key);
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
            var entry = Find(key);
            if (entry != null)
            {
                value = entry.value;
                return true;
            }
            value = null;
            return false;
        }

        private SettingEntry Find(string key) => entries?.Find(e => e != null && e.key == key);
    }

    /// <summary>
    /// 設定エントリのシリアライゼーション用クラス
    /// </summary>
    [Serializable]
    internal sealed class SettingEntry
    {
        public string key;
        public string value;
    }
}
