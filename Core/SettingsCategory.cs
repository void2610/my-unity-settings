using System.Collections.Generic;

namespace Void2610.SettingsSystem
{
    public sealed class SettingsCategory
    {
        public string Name { get; }
        public IReadOnlyList<ISettingBase> Settings { get; }

        /// <summary>
        /// タイトル画面でのみ表示するかどうか
        /// </summary>
        public bool TitleOnly { get; }

        public SettingsCategory(string name, IEnumerable<ISettingBase> settings, bool titleOnly = false)
        {
            Name = name;
            Settings = new List<ISettingBase>(settings);
            TitleOnly = titleOnly;
        }
    }
}
