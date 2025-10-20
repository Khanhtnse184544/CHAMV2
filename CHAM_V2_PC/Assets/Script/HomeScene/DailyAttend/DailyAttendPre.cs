using System;
using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DailyAttendPreSpace
{
    public class DailyAttendPreManager : MonoBehaviour
    {
        [Header("UI References")]
        public Transform contentParent;
        public GameObject slotPrefab;
        public TMP_Text coins;

        [Header("Data")]
        public List<RewardPreData> rewardList;
        private Dictionary<string, GameObject> resourcePrefabs;

        [Header("References")]
        public BagController bagController;
        public string currentUserId = "U111"; //UserSession.currentUser.userId;

        private string saveFilePath;
        private AttendancePreData attendanceData;

        void Start()
        {
            string exeFolder = AppDomain.CurrentDomain.BaseDirectory;
            string userFolder = Path.Combine(exeFolder, currentUserId);
            if (!Directory.Exists(userFolder))
                Directory.CreateDirectory(userFolder);

            saveFilePath = Path.Combine(userFolder, "attendance_pre.json");

            LoadAllPrefabsFromResources();
            LoadRewardList();
            LoadAttendanceData();
            CreateRewardSlots();
        }

        void LoadAllPrefabsFromResources()
        {
            resourcePrefabs = new Dictionary<string, GameObject>();
            GameObject[] allPrefabs = Resources.LoadAll<GameObject>("");

            foreach (var prefab in allPrefabs)
            {
                ItemClass item = prefab.GetComponent<ItemClass>();
                if (item != null && !resourcePrefabs.ContainsKey(item.itemId))
                    resourcePrefabs[item.itemId] = prefab;
            }
        }

        void LoadRewardList()
        {
            rewardList = new List<RewardPreData>
            {
                new RewardPreData { ItemId = "DG001", quantity = 2 },
                new RewardPreData { ItemId = "DG002", quantity = 3 },
                new RewardPreData { ItemId = "Coins", quantity = 100 },
                new RewardPreData { ItemId = "DF001", quantity = 2 },
                new RewardPreData { ItemId = "Coins", quantity = 300 },
                new RewardPreData { ItemId = "DC001", quantity = 3 },
                new RewardPreData { ItemId = "DFR01", quantity = 5 }
            };
        }

        void LoadAttendanceData()
        {
            if (File.Exists(saveFilePath))
            {
                try
                {
                    string json = File.ReadAllText(saveFilePath);
                    attendanceData = JsonUtility.FromJson<AttendancePreData>(json);
                }
                catch
                {
                    attendanceData = new AttendancePreData();
                }
            }
            else
            {
                attendanceData = new AttendancePreData();
            }

            DateTime today = DateTime.Now.Date;
            if (!string.IsNullOrEmpty(attendanceData.lastClaimDate))
            {
                DateTime lastDate = DateTime.Parse(attendanceData.lastClaimDate);
                if ((today - lastDate).Days >= 2)
                {
                    Debug.Log("⚠️ Bỏ qua 1 ngày -> reset chuỗi điểm danh Premium!");
                    attendanceData.claimedDays.Clear();
                    attendanceData.lastClaimDate = "";
                    SaveAttendanceData();
                }
            }
        }

        void CreateRewardSlots()
        {
            foreach (Transform child in contentParent)
                Destroy(child.gameObject);

            bool isPremium = UserSession.currentUser != null &&
                             UserSession.currentUser.memberTypeId == "M2";

            for (int i = 0; i < rewardList.Count; i++)
            {
                RewardPreData reward = rewardList[i];
                GameObject slot = Instantiate(slotPrefab, contentParent);

                Image icon = slot.transform.Find("ItemIcon")?.GetComponent<Image>();
                TextMeshProUGUI qtyText = slot.transform.Find("ItemIcon/Quantity")?.GetComponent<TextMeshProUGUI>();
                TextMeshProUGUI dayNo = slot.transform.Find("Day/No")?.GetComponent<TextMeshProUGUI>();

                if (resourcePrefabs.TryGetValue(reward.ItemId, out GameObject prefab))
                {
                    Sprite iconSprite = prefab.GetComponent<SpriteRenderer>()?.sprite
                        ?? prefab.GetComponent<Image>()?.sprite;

                    if (icon != null)
                    {
                        icon.sprite = iconSprite;
                        icon.color = Color.white;
                    }
                }
                else if (icon != null)
                {
                    icon.sprite = null;
                    icon.color = Color.clear;
                }

                if (qtyText != null)
                    qtyText.text = "x" + reward.quantity;

                if (dayNo != null)
                    dayNo.text = (i + 1).ToString();

                CanvasGroup group = slot.GetComponent<CanvasGroup>() ?? slot.AddComponent<CanvasGroup>();
                bool isClaimed = attendanceData.claimedDays.Contains(i + 1);
                group.alpha = isClaimed ? 0.5f : 1f;

                Button btn = slot.GetComponent<Button>() ?? slot.AddComponent<Button>();
                int dayIndex = i + 1;
                btn.interactable = isPremium && !isClaimed && CanClaimThisDay(dayIndex);

                btn.onClick.RemoveAllListeners();
                btn.onClick.AddListener(() => OnRewardClicked(reward, slot, dayIndex, isPremium));
            }
        }

        bool CanClaimThisDay(int dayIndex)
        {
            int nextDay = attendanceData.claimedDays.Count + 1;
            if (dayIndex != nextDay)
                return false;

            if (!string.IsNullOrEmpty(attendanceData.lastClaimDate))
            {
                if (DateTime.Parse(attendanceData.lastClaimDate) == DateTime.Now.Date)
                    return false;
            }

            return true;
        }

        void OnRewardClicked(RewardPreData reward, GameObject slot, int dayIndex, bool isPremium)
        {
            if (!isPremium)
            {
                Debug.Log("❌ Chỉ thành viên Premium mới có thể nhận quà này!");
                return;
            }

            if (!CanClaimThisDay(dayIndex))
            {
                Debug.Log("⚠️ Hôm nay bạn chỉ được nhận phần thưởng kế tiếp một lần!");
                return;
            }

            Debug.Log($"🎯 Nhận reward Premium: {reward.ItemId} x{reward.quantity}");

            if (reward.ItemId == "Coins" && coins != null && int.TryParse(coins.text, out int current))
            {
                coins.text = (current + reward.quantity).ToString();
            }
            else if (bagController != null)
            {
                bagController.AddItemToBag(reward.ItemId, reward.quantity);
            }

            attendanceData.claimedDays.Add(dayIndex);
            attendanceData.lastClaimDate = DateTime.Now.ToString("yyyy-MM-dd");
            SaveAttendanceData();

            slot.GetComponent<CanvasGroup>().alpha = 0.5f;
            slot.GetComponent<Button>().interactable = false;

            Debug.Log($"✅ Đã nhận phần thưởng Premium ngày {dayIndex}");
        }

        void SaveAttendanceData()
        {
            File.WriteAllText(saveFilePath, JsonUtility.ToJson(attendanceData, true));
        }
    }

    [Serializable]
    public class RewardPreData
    {
        public string ItemId;
        public int quantity;
    }

    [Serializable]
    public class AttendancePreData
    {
        public List<int> claimedDays = new();
        public string lastClaimDate = "";
    }
}
