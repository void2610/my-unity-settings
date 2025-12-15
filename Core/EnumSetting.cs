using System;
using System.Linq;
using UnityEngine;

namespace Void2610.SettingsSystem
{
    /// <summary>
    /// 列挙型の選択肢から選ぶ設定項目
    /// 解像度切り替えや品質設定などに使用
    /// </summary>
    [Serializable]
    public class EnumSetting : SettingBase<string>
    {
        [SerializeField] private string[] options;
        [SerializeField] private string[] displayNames; // 表示用の名前（オプション）

        /// <summary>
        /// 現在選択されているインデックス
        /// </summary>
        public int CurrentIndex
        {
            get => Array.IndexOf(options ?? new string[0], CurrentValue);
            set
            {
                if (options != null && value >= 0 && value < options.Length)
                {
                    CurrentValue = options[value];
                }
            }
        }

        /// <summary>
        /// 選択肢の配列
        /// </summary>
        public string[] Options => options ?? new string[0];

        /// <summary>
        /// 表示名の配列（未設定の場合はOptionsを使用）
        /// </summary>
        public string[] DisplayNames => displayNames ?? options ?? new string[0];

        /// <summary>
        /// 現在の表示名
        /// </summary>
        public string CurrentDisplayName
        {
            get
            {
                var index = CurrentIndex;
                return index >= 0 && index < DisplayNames.Length ? DisplayNames[index] : CurrentValue;
            }
        }

        public EnumSetting(string name, string desc, string[] opts, string defaultValue = null, string[] displayNames = null)
            : base(name, desc, defaultValue ?? (opts?.FirstOrDefault() ?? ""))
        {
            options = opts ?? new string[] { "Option1" };
            this.displayNames = displayNames;
        }

        public EnumSetting(string name, string desc, string[] opts, int defaultIndex = 0, string[] displayNames = null)
            : base(name, desc, (opts != null && defaultIndex >= 0 && defaultIndex < opts.Length) ? opts[defaultIndex] : (opts?.FirstOrDefault() ?? ""))
        {
            options = opts ?? new string[] { "Option1" };
            this.displayNames = displayNames;
        }

        public EnumSetting()
        {
            // シリアライゼーション用のデフォルトコンストラクタ
            options = new string[] { "Default" };
        }

        /// <summary>
        /// 次の選択肢に移動
        /// </summary>
        public void MoveNext()
        {
            if (options != null && options.Length > 0)
            {
                CurrentIndex = (CurrentIndex + 1) % options.Length;
            }
        }

        /// <summary>
        /// 前の選択肢に移動
        /// </summary>
        public void MovePrevious()
        {
            if (options != null && options.Length > 0)
            {
                var currentIdx = CurrentIndex;
                CurrentIndex = (currentIdx - 1 + options.Length) % options.Length;
            }
        }

        /// <summary>
        /// 指定したenumの値で設定を初期化
        /// </summary>
        public static EnumSetting CreateFromEnum<T>(string name, string desc, T defaultValue, string[] customDisplayNames = null)
            where T : Enum
        {
            var enumValues = Enum.GetValues(typeof(T)).Cast<T>().ToArray();
            var opts = enumValues.Select(e => e.ToString()).ToArray();
            var defaultVal = defaultValue.ToString();

            return new EnumSetting(name, desc, opts, defaultVal, customDisplayNames);
        }

        /// <summary>
        /// 現在の値をenumとして取得
        /// </summary>
        public T GetEnumValue<T>() where T : Enum
        {
            if (Enum.TryParse(typeof(T), CurrentValue, out object result))
            {
                return (T)result;
            }
            return default(T);
        }
    }
}
