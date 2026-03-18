namespace Void2610.SettingsSystem
{
    /// <summary>
    /// 設定システムの表示オプション。RootLifetimeScope でインスタンス登録して使用する。
    /// </summary>
    public sealed class SettingsSystemOptions
    {
        /// <summary>
        /// 設定項目の説明文を表示するかどうか。
        /// </summary>
        public bool ShowDescriptions { get; set; } = true;
    }
}
