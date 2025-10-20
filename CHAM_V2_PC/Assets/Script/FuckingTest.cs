using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class FuckingTest : MonoBehaviour
{

    [System.Serializable]
    public class ItemData
    {
        public string itemId;
        public string itemName;
        public string price;
        public string type;
    }

    [System.Serializable]
    public class ItemList
    {
        public List<ItemData> items = new List<ItemData>();
    }

    void Start()
    {
        // Load tất cả prefab trong thư mục Resources (kể cả subfolder)
        GameObject[] allPrefabs = Resources.LoadAll<GameObject>("");

        ItemList list = new ItemList();

        foreach (var prefab in allPrefabs)
        {
            ItemClass item = prefab.GetComponent<ItemClass>();
            if (item != null)
            {
                ItemData data = new ItemData
                {
                    itemId = item.itemId,
                    itemName = item.itemName,
                    price = item.price,
                    type = item.type
                };
                list.items.Add(data);
            }
        }

        // Tạo JSON đẹp (pretty print)
        string json = JsonUtility.ToJson(list, true);

        // Xuất ra Desktop
        string desktopPath = System.Environment.GetFolderPath(System.Environment.SpecialFolder.Desktop);
        string filePath = Path.Combine(desktopPath, "AllItemsFromResources.json");

        File.WriteAllText(filePath, json);

        Debug.Log($"✅ Đã xuất {list.items.Count} prefab ItemClass ra file: {filePath}");
    }
}
