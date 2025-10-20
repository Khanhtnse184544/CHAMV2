using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

public class PlantLogTabManage : MonoBehaviour
{
    [Header("Prefab slot hiển thị cây / hạt giống")]
    public GameObject SlotPlantedLog;

    [Header("Parent chứa các slot")]
    public Transform slotParent;

    [Header("Nút tab trái / phải")]
    public Button leftTabButton;   // Planting Trees
    public Button rightTabButton;  // Collections

    private readonly List<GameObject> listPlantedTree = new List<GameObject>();
    private readonly List<ClaimedTreeData> listClaimedTree = new List<ClaimedTreeData>();

    public static PlantLogTabManage Instance;

    private enum TabType { Planting, Collection }
    private TabType currentTab = TabType.Planting;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            Debug.Log("[PlantLogTabManage] ✅ Instance initialized!");
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        if (leftTabButton != null)
            leftTabButton.onClick.AddListener(() => SwitchTab(TabType.Planting));

        if (rightTabButton != null)
            rightTabButton.onClick.AddListener(() => SwitchTab(TabType.Collection));

        GetAllPlantingTrees();
    }

    #region --- Tab Switching ---
    private void SwitchTab(TabType type)
    {
        currentTab = type;

        foreach (Transform child in slotParent)
            Destroy(child.gameObject);

        if (type == TabType.Planting)
        {
            Debug.Log("[PlantLogTabManage] 🪴 Hiển thị cây đang trồng");
            GetAllPlantingTrees();
        }
        else
        {
            Debug.Log("[PlantLogTabManage] 🌳 Hiển thị Collection");
            ShowCollection();
        }
    }
    #endregion

    #region --- Lấy cây đang trồng ---
    private void GetAllPlantingTrees()
    {
        listPlantedTree.Clear();

        ItemClass[] allItems = FindObjectsByType<ItemClass>(FindObjectsSortMode.None);

        foreach (ItemClass item in allItems)
        {
            // chỉ lấy cây chưa claim (lv0–2)
            if ((item.type == "Seed" || item.type == "Tree") && !item.itemId.Contains("3"))
            {
                listPlantedTree.Add(item.gameObject);
                CreateSlot(item);
            }
        }

        Debug.Log($"[PlantLogTabManage] ✅ Đã load {listPlantedTree.Count} cây đang trồng.");
    }
    #endregion

    #region --- Hiển thị Collection ---
    private void ShowCollection()
    {
        foreach (var tree in listClaimedTree)
        {
            CreateSlot(tree);
        }

        Debug.Log($"[PlantLogTabManage] ✅ Đã load {listClaimedTree.Count} cây trong Collection.");
    }
    #endregion

    #region --- Khi cây được Claim ---
    public void AddToCollection(ClaimedTreeData data)
    {
        StartCoroutine(AddToCollectionRoutine(data));
    }

    private IEnumerator AddToCollectionRoutine(ClaimedTreeData data)
    {
        yield return new WaitForEndOfFrame();

        listClaimedTree.Add(data);
        Debug.Log($"[PlantLogTabManage] 🌿 Cây {data.name} (Lv {data.level}, ID {data.itemId}) đã được thêm vào Collection.");

        if (currentTab == TabType.Collection)
        {
            CreateSlot(data);
        }

        if (currentTab == TabType.Planting)
        {
            foreach (Transform child in slotParent)
                Destroy(child.gameObject);
            GetAllPlantingTrees();
        }

        // 🔹 Gửi danh sách collection mới lên API
        SendCollectionToAPI();
    }
    #endregion

    #region --- Tạo Slot ---
    private void CreateSlot(ItemClass item)
    {
        if (SlotPlantedLog == null || slotParent == null)
        {
            Debug.LogWarning("[PlantLogTabManage] ⚠️ Slot prefab hoặc slotParent chưa được gán!");
            return;
        }

        GameObject slot = Instantiate(SlotPlantedLog, slotParent);
        var icon = slot.transform.Find("PlantedIcon")?.GetComponent<Image>();
        var levelText = slot.transform.Find("StartIcon/Level")?.GetComponent<TextMeshProUGUI>();

        SpriteRenderer itemImg = item.GetComponent<SpriteRenderer>();
        if (icon != null && itemImg != null)
            icon.sprite = itemImg.sprite;

        SaveItemData data = item.GetComponent<SaveItemData>();
        if (levelText != null && data != null)
            levelText.text = data.level.ToString();
    }

    private void CreateSlot(ClaimedTreeData data)
    {
        if (SlotPlantedLog == null || slotParent == null)
        {
            Debug.LogWarning("[PlantLogTabManage] ⚠️ Slot prefab hoặc slotParent chưa được gán!");
            return;
        }

        GameObject slot = Instantiate(SlotPlantedLog, slotParent);
        var icon = slot.transform.Find("PlantedIcon")?.GetComponent<Image>();
        var levelText = slot.transform.Find("StartIcon/Level")?.GetComponent<TextMeshProUGUI>();
        var idText = slot.transform.Find("ItemIdText")?.GetComponent<TextMeshProUGUI>(); // optional text for ID

        if (icon != null && data.sprite != null)
            icon.sprite = data.sprite;

        if (levelText != null)
            levelText.text = data.level.ToString();

        if (idText != null)
            idText.text = data.itemId;
    }
    #endregion

    #region --- Gửi dữ liệu Collection lên API ---
    public void SendCollectionToAPI()
    {
        StartCoroutine(SendCollectionRoutine());
    }

    private IEnumerator SendCollectionRoutine()
    {
        // 🧩 Lấy danh sách ItemId
        List<string> ids = new List<string>();
        foreach (var tree in listClaimedTree)
            ids.Add(tree.itemId);

        if (ids.Count == 0)
        {
            Debug.LogWarning("[PlantLogTabManage] ⚠️ Không có itemId nào để gửi lên server.");
            yield break;
        }

        // 🧩 Lấy UserId từ UserSession
        string userId = (UserSession.currentUser != null && !string.IsNullOrEmpty(UserSession.currentUser.userId))
            ? UserSession.currentUser.userId
            : "unknown";

        // 🧩 Endpoint API
        string url = $"https://apigame-e8g0a8cyc2b2hseg.eastasia-01.azurewebsites.net/api/User/SavePlantedLog?userId={userId}";
        Debug.Log($"[PlantLogTabManage] 🌐 PUT {url}");

        // 🧩 Convert danh sách sang JSON array chuẩn (["A","B","C"])
        string jsonArray = JsonHelper.ToJson(ids.ToArray());
        Debug.Log("[PlantLogTabManage] 📦 Body JSON: " + jsonArray);

        // 🧩 Tạo request PUT
        using (UnityWebRequest req = new UnityWebRequest(url, "PUT"))
        {
            byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonArray);
            req.uploadHandler = new UploadHandlerRaw(bodyRaw);
            req.downloadHandler = new DownloadHandlerBuffer();
            req.SetRequestHeader("Content-Type", "application/json");

            yield return req.SendWebRequest();

            if (req.result == UnityWebRequest.Result.Success)
            {
                Debug.Log("[PlantLogTabManage] ✅ Gửi danh sách cây thành công!");
            }
            else
            {
                Debug.LogWarning($"[PlantLogTabManage] ❌ Lỗi gửi API: {req.error}");
            }
        }
    }
    #endregion

    #region --- Tự động lưu khi game tạm dừng hoặc thoát ---
    private void OnApplicationPause(bool pause)
    {
        if (pause)
        {
            Debug.Log("[PlantLogTabManage] 💾 Game bị tạm dừng — tự động lưu Collection lên server...");
            SendCollectionToAPI();
        }
    }

    private void OnApplicationQuit()
    {
        Debug.Log("[PlantLogTabManage] 💾 Game thoát — tự động lưu Collection lên server...");
        StartCoroutine(SendAndWaitBeforeQuit());
    }

    private IEnumerator SendAndWaitBeforeQuit()
    {
        yield return SendCollectionRoutine();
        yield return new WaitForSeconds(0.3f); // chờ gửi xong
    }
    #endregion
}
