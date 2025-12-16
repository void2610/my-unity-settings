using System.Collections.Generic;
using R3;

namespace Void2610.SettingsSystem
{
    public interface ISettingsDefinition
    {
        IEnumerable<SettingsCategory> CreateCategories();
        void BindSettingActions(IReadOnlyList<SettingsCategory> categories, CompositeDisposable disposables);
    }
}
