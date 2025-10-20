using UnityEngine;
using UnityEngine.EventSystems;

public class DragDropPlacedDecor : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    private Camera mainCam;
    private Vector3 originalPos;
    private SpriteRenderer sr;
    private GameObject highlight;
    private SpriteRenderer highlightSR;

    private void Start()
    {
        mainCam = Camera.main;
        sr = GetComponent<SpriteRenderer>();

        // ✅ highlight
        highlight = new GameObject("Highlight_Move");
        highlightSR = highlight.AddComponent<SpriteRenderer>();
        highlightSR.sprite = sr.sprite;
        highlightSR.sortingOrder = sr.sortingOrder + 1;
        highlightSR.color = new Color(1, 1, 1, 0);
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        Debug.Log("🎯 Bắt đầu kéo Decor đã đặt.");
        originalPos = transform.position;
        sr.color = new Color(1, 1, 1, 0.5f);
    }

    public void OnDrag(PointerEventData eventData)
    {
        Vector3 worldPos = mainCam.ScreenToWorldPoint(Input.mousePosition);
        worldPos.z = 0;

        transform.position = worldPos;
        highlight.transform.position = worldPos;

        bool canMove = CheckCanPlace(worldPos);
        highlightSR.color = canMove ? new Color(0, 1, 0, 0.35f) : new Color(1, 0, 0, 0.35f);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        Vector3 worldPos = mainCam.ScreenToWorldPoint(Input.mousePosition);
        worldPos.z = 0;
        bool canMove = CheckCanPlace(worldPos);

        if (!canMove)
        {
            transform.position = originalPos;
            Debug.Log("⚠️ Không thể di chuyển Decor tới vị trí này.");
        }
        else
        {
            Debug.Log("✅ Di chuyển Decor thành công.");
        }

        sr.color = Color.white;
        highlightSR.color = new Color(1, 1, 1, 0);
    }

    private bool CheckCanPlace(Vector3 pos)
    {
        Collider2D[] hits = Physics2D.OverlapPointAll(pos);
        int colliderCount = hits.Length;

        if (colliderCount >= 3) return false;

        bool hasSoil = false, hasBackground = false;
        foreach (var hit in hits)
        {
            if (hit == null || hit.gameObject == gameObject) continue;
            if (hit.CompareTag("Soil")) hasSoil = true;
            if (hit.CompareTag("Background")) hasBackground = true;
        }

        return hasBackground && !hasSoil;
    }
}
