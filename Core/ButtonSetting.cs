using System;
using R3;
using UnityEngine;

namespace Void2610.SettingsSystem
{
    /// <summary>
    /// ボタン形式の設定項目（セーブデータ削除など）
    /// </summary>
    [Serializable]
    public sealed class ButtonSetting : SettingBase<Unit>
    {
        [SerializeField] private string buttonText;
        [SerializeField] private bool requiresConfirmation;
        [SerializeField] private string confirmationMessage;

        /// <summary>
        /// ボタンに表示するテキスト
        /// </summary>
        public string ButtonText => buttonText;

        /// <summary>
        /// 確認ダイアログが必要かどうか
        /// </summary>
        public bool RequiresConfirmation => requiresConfirmation;

        public string ConfirmationMessage => confirmationMessage;

        public Observable<Unit> OnButtonClicked => _onButtonClicked;

        private readonly Subject<Unit> _onButtonClicked = new();

        public ButtonSetting(string key, string name, string desc, string btnText, bool needsConfirmation = false, string confirmMsg = "")
            : base(key, name, desc, Unit.Default)
        {
            buttonText = btnText;
            requiresConfirmation = needsConfirmation;
            confirmationMessage = confirmMsg;
        }

        public ButtonSetting()
        {
            // シリアライゼーション用のデフォルトコンストラクタ
            buttonText = "";
        }

        public void ExecuteAction()
        {
            CurrentValue = Unit.Default;
            _onButtonClicked.OnNext(Unit.Default);
        }
    }
}
