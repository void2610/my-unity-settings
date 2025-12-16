using System.Collections.Generic;

namespace Void2610.SettingsSystem
{
    public class SettingsCategory
    {
        public string Name { get; }
        public IReadOnlyList<ISettingBase> Settings { get; }

        public SettingsCategory(string name, IEnumerable<ISettingBase> settings)
        {
            Name = name;
            Settings = new List<ISettingBase>(settings);
        }
    }
}
