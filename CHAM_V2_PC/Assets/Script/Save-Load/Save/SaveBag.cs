using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

public class SaveBag : MonoBehaviour
{
    [SerializeField] private BagController bagController;

    private string apiUrl = "https://apigame-e8g0a8cyc2b2hseg.eastasia-01.azurewebsites.net/api/Category/SaveCategory";

    [Header("Auto Save Settings")]
    public bool enableAutoSave = true;
    public float autoSaveInterval = 180f;

    private Coroutine autoSaveCoroutine;

    void Start()
    {
        if (enableAutoSave)
        {
            autoSaveCoroutine = StartCoroutine(AutoSaveRoutine());
        }
    }

    private IEnumerator AutoSaveRoutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(autoSaveInterval);
            if (bagController != null && bagController.IsReady())
            {
                Debug.Log("🕒 Auto-saving bag data...");
                yield return SaveBagCoroutine();
            }
        }
    }

    public void SaveBagToServer()
    {
        StartCoroutine(SaveBagCoroutine());
    }

    private void OnApplicationQuit()
    {
        Debug.Log("💾 OnApplicationQuit → Lưu túi trước khi thoát game...");
        SaveBagOffline();
        StartCoroutine(SaveBagCoroutine());
    }

    private void OnApplicationPause(bool pause)
    {
        if (pause)
        {
            Debug.Log("📴 App bị tạm dừng → lưu túi an toàn...");
            SaveBagOffline();
            StartCoroutine(SaveBagCoroutine());
        }
    }

    private IEnumerator SaveBagCoroutine()
    {
        var user = UserSession.currentUser;
        if (user == null || string.IsNullOrEmpty(user.userId))
        {
            Debug.LogWarning("⚠ Không có userId, không thể lưu lên server.");
            yield break;
        }

        List<LoadBag.BagItemData> currentBag = bagController.GetCurrentBagData();
        if (currentBag == null || currentBag.Count == 0)
        {
            Debug.Log("📦 Túi trống, không cần lưu.");
            yield break;
        }

        List<CateDAO> cateList = new List<CateDAO>();
        foreach (var item in currentBag)
        {
            cateList.Add(new CateDAO
            {
                itemId = item.itemId ?? string.Empty,
                quantity = item.quantity
            });
        }

        foreach (var c in cateList)
        {
            Debug.Log($"🔎 Cate -> itemId: {c.itemId}, quantity: {c.quantity}");
        }

        string jsonBody = JsonHelper.ToJson(cateList.ToArray(), false);
        string fullUrl = $"{apiUrl}?userId={user.userId}";

        Debug.Log($"📤 Sending to {fullUrl}\nBody: {jsonBody}");

        using (UnityWebRequest www = new UnityWebRequest(fullUrl, "PUT"))
        {
            byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonBody);
            www.uploadHandler = new UploadHandlerRaw(bodyRaw);
            www.downloadHandler = new DownloadHandlerBuffer();
            www.SetRequestHeader("Content-Type", "application/json");

            yield return www.SendWebRequest();

#if UNITY_2020_1_OR_NEWER
            bool isNetworkError = www.result == UnityWebRequest.Result.ConnectionError || www.result == UnityWebRequest.Result.ProtocolError;
#else
            bool isNetworkError = www.isNetworkError || www.isHttpError;
#endif

            if (!isNetworkError)
            {
                Debug.Log($"✅ Túi đã được lưu lên server thành công! Status: {www.responseCode}");
                Debug.Log("📨 Server response: " + www.downloadHandler.text);
                PlayerPrefs.SetString($"lastBagBackup_{user.userId}", jsonBody);
                PlayerPrefs.Save();
            }
            else
            {
                Debug.LogError($"❌ Không thể lưu túi lên server: {www.error} (code: {www.responseCode})\n➡ Lưu tạm local để backup.\nServer response: {www.downloadHandler.text}");
                SaveBagOffline();
            }
        }
    }

    // 👉 Lưu file backup ngay trong thư mục build (cùng với .exe)
    private void SaveBagOffline()
    {
        try
        {
            var user = UserSession.currentUser;
            if (user == null || string.IsNullOrEmpty(user.userId))
            {
                Debug.LogWarning("⚠ Không có userId, không thể lưu offline.");
                return;
            }

            List<LoadBag.BagItemData> currentBag = bagController.GetCurrentBagData();
            if (currentBag == null) return;

            string json = JsonUtility.ToJson(new SerializableBagWrapper { items = currentBag });

            // 🔸 Lấy thư mục chứa file .exe
            string exeDirectory = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            // 🔸 Tạo folder userId trong thư mục game
            string userFolder = Path.Combine(exeDirectory, user.userId);
            if (!Directory.Exists(userFolder))
                Directory.CreateDirectory(userFolder);

            // 🔸 File backup
            string filePath = Path.Combine(userFolder, $"bagData_{user.userId}_backup.json");

            File.WriteAllText(filePath, json, Encoding.UTF8);
            Debug.Log($"🪣 Túi của user {user.userId} đã được lưu local tại: {filePath}");
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"❌ Lỗi khi lưu túi local: {ex.Message}");
        }
    }

    // 👉 Load lại từ đúng folder userId trong thư mục game
    public List<LoadBag.BagItemData> LoadBagOffline()
    {
        try
        {
            var user = UserSession.currentUser;
            if (user == null || string.IsNullOrEmpty(user.userId))
            {
                Debug.LogWarning("⚠ Không có userId, không thể load offline.");
                return null;
            }

            string exeDirectory = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            string userFolder = Path.Combine(exeDirectory, user.userId);
            string filePath = Path.Combine(userFolder, $"bagData_{user.userId}_backup.json");

            if (!File.Exists(filePath))
            {
                Debug.Log("📂 Không có file backup local.");
                return null;
            }

            string json = File.ReadAllText(filePath, Encoding.UTF8);
            SerializableBagWrapper wrapper = JsonUtility.FromJson<SerializableBagWrapper>(json);

            Debug.Log($"📦 Đã load {wrapper.items.Count} item từ file backup local: {filePath}");
            return wrapper.items;
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"❌ Lỗi khi đọc file backup local: {ex.Message}");
            return null;
        }
    }

    [System.Serializable]
    public class CateDAO
    {
        public string itemId;
        public int quantity;
    }

    [System.Serializable]
    public class SerializableBagWrapper
    {
        public List<LoadBag.BagItemData> items;
    }
}
