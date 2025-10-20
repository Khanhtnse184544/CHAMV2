using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class PanelManager : MonoBehaviour
{
    [System.Serializable]
    public class PanelItem
    {
        public string panelName;       // Tên panel (Inventory, Settings, Shop, ...)
        public GameObject panelObject; // GameObject chứa panel
    }

    public List<PanelItem> panels;       // Kéo thả các panel vào đây trong Inspector
    public float fadeDuration = 0.25f;   // Thời gian fade in/out

    private Dictionary<string, CanvasGroup> panelDict = new Dictionary<string, CanvasGroup>();
    private string currentPanel = null;

    void Awake()
    {
        // Lưu panel vào dictionary và ẩn đi
        foreach (var p in panels)
        {
            if (p.panelObject != null)
            {
                CanvasGroup cg = p.panelObject.GetComponent<CanvasGroup>();
                if (cg == null) cg = p.panelObject.AddComponent<CanvasGroup>();
                panelDict[p.panelName] = cg;
                SetPanelState(cg, false, true); // ẩn mặc định
            }
        }
    }

    /// <summary>
    /// Hiện panel và ẩn panel khác
    /// </summary>
    public void ShowPanel(string name)
    {
        foreach (var kv in panelDict)
        {
            if (kv.Key == name) StartCoroutine(FadePanel(kv.Value, true));
            else StartCoroutine(FadePanel(kv.Value, false));
        }
        currentPanel = name;
    }

    /// <summary>
    /// Ẩn panel nếu nó đang mở
    /// </summary>
    public void HidePanel(string name)
    {
        if (panelDict.ContainsKey(name))
        {
            StartCoroutine(FadePanel(panelDict[name], false));
            if (currentPanel == name) currentPanel = null;
        }
    }

    /// <summary>
    /// Toggle panel (bật/tắt, đảm bảo chỉ 1 panel mở cùng lúc)
    /// </summary>
    public void TogglePanel(string name)
    {
        if (currentPanel == name)
            HidePanel(name);
        else
            ShowPanel(name);
    }

    // Hàm fade in/out
    private IEnumerator FadePanel(CanvasGroup cg, bool show)
    {
        float startAlpha = cg.alpha;
        float targetAlpha = show ? 1f : 0f;
        float time = 0f;

        // Nếu show thì bật interactable ngay từ đầu
        if (show)
        {
            cg.gameObject.SetActive(true);
            cg.interactable = true;
            cg.blocksRaycasts = true;
        }

        while (time < fadeDuration)
        {
            time += Time.deltaTime;
            cg.alpha = Mathf.Lerp(startAlpha, targetAlpha, time / fadeDuration);
            yield return null;
        }

        cg.alpha = targetAlpha;

        // Nếu hide thì tắt tương tác và disable GameObject
        if (!show)
        {
            cg.interactable = false;
            cg.blocksRaycasts = false;
            cg.gameObject.SetActive(false);
        }
    }

    // Hàm thiết lập trạng thái panel ngay lập tức (không fade)
    private void SetPanelState(CanvasGroup cg, bool show, bool instant = false)
    {
        if (instant)
        {
            cg.alpha = show ? 1f : 0f;
            cg.interactable = show;
            cg.blocksRaycasts = show;
            cg.gameObject.SetActive(show);
        }
    }
    void Update()
    {
        // Nếu có panel đang mở
        if (currentPanel != null)
        {
            // Nếu click chuột trái hoặc chạm
            if (Input.GetMouseButtonDown(0))
            {
                // Kiểm tra xem click có nằm trên UI không
                if (!IsPointerOverUIObject(panelDict[currentPanel].gameObject))
                {
                    HidePanel(currentPanel);
                }
            }
        }
    }

    private bool IsPointerOverUIObject(GameObject panelObj)
    {
        // Lấy vị trí chuột / touch
        PointerEventData eventData = new PointerEventData(EventSystem.current);
        eventData.position = Input.mousePosition;

        // Raycast UI
        var results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(eventData, results);

        // Nếu không click trúng UI nào → return false
        if (results.Count == 0) return false;

        // Nếu click trúng UI nhưng không phải con của panel → return false
        foreach (var r in results)
        {
            if (r.gameObject == panelObj || r.gameObject.transform.IsChildOf(panelObj.transform))
                return true;
        }
        return false;
    }

    public void ShowPanelOnly(string name)
    {
        List<string> subPanels = new List<string> { "RemoveAds", "CoinsPanel" };
        foreach (string panelName in subPanels)
        {
            if (panelDict.ContainsKey(panelName))
            {
                bool shouldShow = panelName == name;
                StartCoroutine(FadePanel(panelDict[panelName], shouldShow));
            }
        }
    }

}
