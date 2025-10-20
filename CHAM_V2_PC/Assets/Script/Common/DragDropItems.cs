using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class DragDropItems : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [Header("Canvas cần ẩn khi kéo")]
    public List<Canvas> allCanvasToHide;

    [Header("Slot đang kéo")]
    public BagSlotUI slotUI;

    private GameObject ghostItem;
    private GameObject highlight;
    private SpriteRenderer highlightSR;
    private Camera mainCam;

    private void Start()
    {
        mainCam = Camera.main;
        allCanvasToHide = new List<Canvas>(FindObjectsByType<Canvas>(FindObjectsSortMode.None));

        // ✅ Tạo highlight trong suốt
        highlight = new GameObject("Highlight");
        highlightSR = highlight.AddComponent<SpriteRenderer>();
        highlightSR.sortingOrder = 1000;
        highlightSR.color = new Color(1, 1, 1, 0);
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (slotUI == null || string.IsNullOrEmpty(slotUI.itemId)) return;

        foreach (var canvas in allCanvasToHide)
            if (canvas != null)
                canvas.enabled = false;

        GameObject prefab = FindPrefabByItemId(slotUI.itemId);
        if (prefab == null)
        {
            Debug.LogWarning($"❌ Không tìm thấy prefab cho itemId: {slotUI.itemId}");
            ShowAllCanvas();
            return;
        }

        ghostItem = Instantiate(prefab);
        var col = ghostItem.GetComponent<Collider2D>();
        if (col != null) col.enabled = false;

        var sr = ghostItem.GetComponent<SpriteRenderer>();
        if (sr != null)
            sr.color = new Color(1, 1, 1, 0.5f);

        highlightSR.sprite = sr != null ? sr.sprite : null;
        highlightSR.color = new Color(1, 1, 1, 0);

        UpdateGhostAndHighlight();
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (ghostItem != null)
            UpdateGhostAndHighlight();
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (ghostItem == null)
        {
            ShowAllCanvas();
            highlightSR.color = new Color(1, 1, 1, 0);
            return;
        }

        Vector3 worldPos = mainCam.ScreenToWorldPoint(Input.mousePosition);
        worldPos.z = 0;

        bool canPlace = CheckCanPlace(worldPos, slotUI.itemType);

        if (canPlace)
        {
            PlaceItem(worldPos);
            Debug.Log($"✅ Đặt {slotUI.itemType} tại {worldPos}");
        }
        else
        {
            Debug.Log("⚠️ Không thể đặt vật phẩm ở đây.");
        }

        Destroy(ghostItem);
        highlightSR.color = new Color(1, 1, 1, 0);
        ShowAllCanvas();
    }

    // ====================== HÀM PHỤ ======================

    private void UpdateGhostAndHighlight()
    {
        Vector3 worldPos = mainCam.ScreenToWorldPoint(Input.mousePosition);
        worldPos.z = 0;

        // ✅ Nếu là Seed → snap ghost + highlight về tâm Soil gần nhất
        if (slotUI.itemType == "Seed")
        {
            Collider2D[] nearby = Physics2D.OverlapCircleAll(worldPos, 0.5f);
            float minDist = float.MaxValue;
            Vector3 closestCenter = worldPos;

            foreach (var hit in nearby)
            {
                if (hit != null && hit.CompareTag("Soil"))
                {
                    float dist = Vector2.Distance(worldPos, hit.bounds.center);
                    if (dist < minDist)
                    {
                        minDist = dist;
                        closestCenter = hit.bounds.center;
                    }
                }
            }

            // Nếu có Soil gần, căn giữa
            worldPos = closestCenter;
        }

        ghostItem.transform.position = worldPos;
        highlight.transform.position = worldPos;

        bool canPlace = CheckCanPlace(worldPos, slotUI.itemType);
        highlightSR.color = canPlace ? new Color(0, 1, 0, 0.35f) : new Color(1, 0, 0, 0.35f);
    }

    private bool CheckCanPlace(Vector3 pos, string itemType)
    {
        Collider2D[] hits = Physics2D.OverlapPointAll(pos);
        int colliderCount = hits.Length;

        if (itemType == "Seed")
        {
            if (colliderCount > 2) return false;
            foreach (var hit in hits)
                if (hit != null && hit.CompareTag("Soil"))
                    return true;
            return false;
        }
        else if (itemType == "Decor")
        {
            if (colliderCount >= 2) return false;
            bool hasSoil = false, hasBackground = false;
            foreach (var hit in hits)
            {
                if (hit == null) continue;
                if (hit.CompareTag("Soil")) hasSoil = true;
                if (hit.CompareTag("Background")) hasBackground = true;
            }
            return hasBackground && !hasSoil;
        }
        return false;
    }

    private void PlaceItem(Vector3 pos)
    {
        GameObject prefab = FindPrefabByItemId(slotUI.itemId);
        if (prefab == null) return;

        // ✅ Khi thả Seed, đặt đúng tâm Soil gần nhất
        if (slotUI.itemType == "Seed")
        {
            Collider2D[] nearby = Physics2D.OverlapCircleAll(pos, 0.5f);
            float minDist = float.MaxValue;
            Vector3 closestCenter = pos;

            foreach (var hit in nearby)
            {
                if (hit != null && hit.CompareTag("Soil"))
                {
                    float dist = Vector2.Distance(pos, hit.bounds.center);
                    if (dist < minDist)
                    {
                        minDist = dist;
                        closestCenter = hit.bounds.center;
                    }
                }
            }
            pos = closestCenter;
        }

        GameObject placed = Instantiate(prefab, pos, Quaternion.identity);
        var sr = placed.GetComponent<SpriteRenderer>();
        if (sr != null)
            sr.color = Color.white;

        // ✅ Gán layer nếu có "PlacedItem"
        int placedItemLayer = LayerMask.NameToLayer("PlacedItem");
        if (placedItemLayer != -1)
            placed.layer = placedItemLayer;

        // ✅ Nếu là Decor thì gắn collider + script kéo thả lại
        if (slotUI.itemType == "Decor")
        {
            Collider2D col = placed.GetComponent<Collider2D>();
            if (col == null)
            {
                col = placed.AddComponent<BoxCollider2D>();
                if (sr != null && sr.sprite != null)
                    ((BoxCollider2D)col).size = sr.sprite.bounds.size;
            }
            col.isTrigger = false;
            col.enabled = true;

            if (placed.GetComponent<DragDropPlacedDecor>() == null)
                placed.AddComponent<DragDropPlacedDecor>();
        }

        slotUI.ReduceQuantity(1);
        Debug.Log($"🌱 Đặt vật phẩm: {slotUI.itemId} ({slotUI.itemType}) tại {pos}");
    }

    private void ShowAllCanvas()
    {
        foreach (var canvas in allCanvasToHide)
            if (canvas != null)
                canvas.enabled = true;
    }

    private GameObject FindPrefabByItemId(string id)
    {
        GameObject[] allPrefabs = Resources.LoadAll<GameObject>("");
        foreach (var prefab in allPrefabs)
        {
            var item = prefab.GetComponent<ItemClass>();
            if (item != null && item.itemId == id)
                return prefab;
        }
        return null;
    }
}
