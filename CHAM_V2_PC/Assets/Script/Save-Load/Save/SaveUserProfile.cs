using System.Collections;
using System.IO;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;

public class SaveUserProfile : MonoBehaviour
{
    [Header("User Information to save")]
    public TMP_Text currentLevel;
    public TMP_Text expPerLevel;
    public TMP_Text coins;

    private string baseUrl = "https://apigame-e8g0a8cyc2b2hseg.eastasia-01.azurewebsites.net/api/User/UpdateUser";

    [Header("Auto Save Settings")]
    public bool enableAutoSave = true;
    public float autoSaveInterval = 120f; // auto-save mỗi 2 phút

    private Coroutine autoSaveCoroutine;

    void Start()
    {
        if (enableAutoSave)
            autoSaveCoroutine = StartCoroutine(AutoSaveRoutine());
    }

    private IEnumerator AutoSaveRoutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(autoSaveInterval);
            yield return SaveUserProfileCoroutine();
        }
    }

    public void SaveNow()
    {
        StartCoroutine(SaveUserProfileCoroutine());
    }

    private void OnApplicationQuit()
    {
        Debug.Log("💾 OnApplicationQuit → Lưu user profile trước khi thoát...");
        SaveOffline();
        StartCoroutine(SaveUserProfileCoroutine());
    }

    private void OnApplicationPause(bool pause)
    {
        if (pause)
        {
            Debug.Log("📴 App bị tạm dừng → lưu user profile...");
            SaveOffline();
            StartCoroutine(SaveUserProfileCoroutine());
        }
    }

    private IEnumerator SaveUserProfileCoroutine()
    {
        var user = UserSession.currentUser;
        if (user == null || string.IsNullOrEmpty(user.userId))
        {
            Debug.LogWarning("⚠ Không có userId, không thể lưu user profile lên server.");
            yield break;
        }

        // 🔹 Tạo object có đủ 9 field (nhưng chỉ 4 field có giá trị thật)
        UserDAO data = new UserDAO
        {
            userId = user.userId,
            username = "string",
            password = "string",
            email = "string",
            memberTypeId = "string",
            level = int.TryParse(currentLevel.text, out int lvl) ? lvl : 0,
            expPerLevel = int.TryParse(expPerLevel.text, out int exp) ? exp : 0,
            coin = int.TryParse(coins.text, out int coinVal) ? coinVal : 0,
            status = "string"
        };

        string jsonBody = JsonUtility.ToJson(data, true);
        Debug.Log($"📤 Sending to {baseUrl}\nBody: {jsonBody}");

        using (UnityWebRequest www = new UnityWebRequest(baseUrl, "PUT"))
        {
            byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonBody);
            www.uploadHandler = new UploadHandlerRaw(bodyRaw);
            www.downloadHandler = new DownloadHandlerBuffer();
            www.SetRequestHeader("Content-Type", "application/json");

            yield return www.SendWebRequest();

#if UNITY_2020_1_OR_NEWER
            bool isError = www.result == UnityWebRequest.Result.ConnectionError || www.result == UnityWebRequest.Result.ProtocolError;
#else
            bool isError = www.isNetworkError || www.isHttpError;
#endif

            if (!isError)
            {
                Debug.Log($"✅ User profile saved successfully! ({www.responseCode})");
                PlayerPrefs.SetString($"userBackup_{user.userId}", jsonBody);
                PlayerPrefs.Save();
            }
            else
            {
                Debug.LogError($"❌ Save failed: {www.error}\n➡ Backup local.\nResponse: {www.downloadHandler.text}");
                SaveOffline();
            }
        }
    }

    private void SaveOffline()
    {
        try
        {
            var user = UserSession.currentUser;
            if (user == null || string.IsNullOrEmpty(user.userId))
            {
                Debug.LogWarning("⚠ Không có userId, không thể lưu offline.");
                return;
            }

            string exeDir = Application.dataPath;
#if UNITY_STANDALONE_WIN
            exeDir = Path.GetDirectoryName(Application.dataPath);
#endif

            string userFolder = Path.Combine(exeDir, user.userId);
            if (!Directory.Exists(userFolder)) Directory.CreateDirectory(userFolder);

            string path = Path.Combine(userFolder, "userProfile_backup.json");

            string json = JsonUtility.ToJson(new UserDAO
            {
                userId = user.userId,
                username = "string",
                password = "string",
                email = "string",
                memberTypeId = "string",
                level = int.TryParse(currentLevel.text, out int lvl) ? lvl : 0,
                expPerLevel = int.TryParse(expPerLevel.text, out int exp) ? exp : 0,
                coin = int.TryParse(coins.text, out int coinVal) ? coinVal : 0,
                status = "string"
            }, true);

            File.WriteAllText(path, json, Encoding.UTF8);
            Debug.Log($"🪣 User profile đã lưu local tại: {path}");
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"❌ Lỗi khi lưu offline: {ex.Message}");
        }
    }

    [System.Serializable]
    public class UserDAO
    {
        public string userId;
        public string username;
        public string password;
        public string email;
        public string memberTypeId;
        public int level;
        public int expPerLevel;
        public int coin;
        public string status;
    }
}
