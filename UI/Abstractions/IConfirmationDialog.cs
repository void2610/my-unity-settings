using Cysharp.Threading.Tasks;

namespace Void2610.SettingsSystem
{
    /// <summary>
    /// 確認ダイアログのインターフェース
    /// </summary>
    public interface IConfirmationDialog
    {
        /// <summary>
        /// 確認ダイアログを表示し、ユーザーの選択を待つ
        /// </summary>
        /// <param name="message">表示するメッセージ</param>
        /// <param name="confirmText">確認ボタンのテキスト</param>
        /// <param name="cancelText">キャンセルボタンのテキスト</param>
        /// <returns>ユーザーが確認した場合はtrue、キャンセルした場合はfalse</returns>
        UniTask<bool> ShowDialog(string message, string confirmText = "OK", string cancelText = "キャンセル");
    }
}
