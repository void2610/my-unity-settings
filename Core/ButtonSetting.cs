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
        [SerializeField] private bool isDestructive;

        /// <summary>
        /// ボタンに表示するテキスト
        /// </summary>
        public string ButtonText => buttonText;

        /// <summary>
        /// 確認ダイアログが必要かどうか
        /// </summary>
        public bool RequiresConfirmation => requiresConfirmation;

        public string ConfirmationMessage => confirmationMessage;

        /// <summary>
        /// 実行でデータが失われるか (確認ダイアログを破壊的操作として出す)
        /// </summary>
        public bool IsDestructive => isDestructive;

        public Observable<Unit> OnButtonClicked => _onButtonClicked;

        private readonly Subject<Unit> _onButtonClicked = new();

        public ButtonSetting(string key, string name, string desc, string btnText, bool needsConfirmation = false, string confirmMsg = "", bool destructive = false)
            : base(key, name, desc, Unit.Default)
        {
            buttonText = btnText;
            requiresConfirmation = needsConfirmation;
            confirmationMessage = confirmMsg;
            isDestructive = destructive;
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
