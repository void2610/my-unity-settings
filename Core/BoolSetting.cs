using System;

namespace Void2610.SettingsSystem
{
    /// <summary>
    /// 2値の真偽値を扱う設定項目。
    /// </summary>
    [Serializable]
    public sealed class BoolSetting : SettingBase<bool>
    {
        public BoolSetting(string key, string name, string desc, bool defaultValue)
            : base(key, name, desc, defaultValue)
        {
        }

        public BoolSetting()
        {
            // シリアライゼーション用のデフォルトコンストラクタ
        }

        public void Invert()
        {
            CurrentValue = !CurrentValue;
        }
    }
}
