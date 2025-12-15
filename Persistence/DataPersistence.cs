using System;
using System.IO;
using UnityEngine;

namespace Void2610.SettingsSystem
{
    /// <summary>
    /// プラットフォーム別のデータ永続化を抽象化するユーティリティ
    /// </summary>
    public static class DataPersistence
    {
        /// <summary>
        /// データを保存
        /// </summary>
        /// <param name="key">データのキー（ファイル名またはPlayerPrefsキー）</param>
        /// <param name="data">保存するJSONデータ</param>
        /// <returns>保存が成功したかどうか</returns>
        public static bool SaveData(string key, string data)
        {
            try
            {
#if UNITY_WEBGL && !UNITY_EDITOR
                // WebGL環境ではPlayerPrefsに保存
                PlayerPrefs.SetString(key, data);
                PlayerPrefs.Save();
#else
                // その他の環境ではファイルに保存
                var filePath = GetFilePath(key);
                File.WriteAllText(filePath, data);
#endif
                return true;
            }
            catch (Exception e)
            {
                Debug.LogError($"データの保存に失敗しました ({key}): {e.Message}");
                return false;
            }
        }

        /// <summary>
        /// データを読み込み
        /// </summary>
        /// <param name="key">データのキー（ファイル名またはPlayerPrefsキー）</param>
        /// <returns>読み込んだデータ（存在しない場合はnull）</returns>
        public static string LoadData(string key)
        {
            try
            {
#if UNITY_WEBGL && !UNITY_EDITOR
                return PlayerPrefs.GetString(key, null);
#else
                var filePath = GetFilePath(key);
                if (!File.Exists(filePath)) return null;
                return File.ReadAllText(filePath);
#endif
            }
            catch (Exception e)
            {
                Debug.LogError($"データの読み込みに失敗しました ({key}): {e.Message}");
                return null;
            }
        }

        /// <summary>
        /// データが存在するかチェック
        /// </summary>
        /// <param name="key">データのキー</param>
        /// <returns>データの存在有無</returns>
        public static bool DataExists(string key)
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            return PlayerPrefs.HasKey(key) && !string.IsNullOrEmpty(PlayerPrefs.GetString(key, ""));
#else
            var filePath = GetFilePath(key);
            return File.Exists(filePath);
#endif
        }

        /// <summary>
        /// データを削除
        /// </summary>
        /// <param name="key">データのキー</param>
        /// <returns>削除が成功したかどうか</returns>
        public static bool DeleteData(string key)
        {
            try
            {
#if UNITY_WEBGL && !UNITY_EDITOR
                if (PlayerPrefs.HasKey(key))
                {
                    PlayerPrefs.DeleteKey(key);
                    PlayerPrefs.Save();
                    return true;
                }
                return false;
#else
                var filePath = GetFilePath(key);
                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                    return true;
                }
                return false;
#endif
            }
            catch (Exception e)
            {
                Debug.LogError($"データの削除に失敗しました ({key}): {e.Message}");
                return false;
            }
        }

        /// <summary>
        /// ファイルパスを取得（非WebGL環境用）
        /// </summary>
        private static string GetFilePath(string key)
        {
            // キーに拡張子が含まれていない場合は.jsonを追加
            if (!key.EndsWith(".json"))
            {
                key += ".json";
            }
            return Path.Combine(Application.persistentDataPath, key);
        }
    }
}
