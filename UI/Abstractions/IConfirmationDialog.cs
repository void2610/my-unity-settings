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
        /// <param name="destructive">確定でデータが失われる操作か。実装は誤って押しても安全なキャンセルを押されやすい位置に置く等で区別する</param>
        /// <returns>ユーザーが確認した場合はtrue、キャンセルした場合はfalse</returns>
        UniTask<bool> ShowDialog(string message, string confirmText = null, string cancelText = null, bool destructive = false);
    }
}
