using DG.Tweening;
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(ItemClass))]
[RequireComponent(typeof(Collider2D))]
public class TreeGrowth : MonoBehaviour
{
    [Header("Định danh")]
    public string baseId = "TA";

    [Header("Cấp & XP")]
    [Range(0, 3)] public int level = 0;
    public int currentXP = 0;
    public int xpToLevel = 100;

    [Header("Sprites / Prefabs")]
    public GameObject SeedPrefab;
    public GameObject TreeLv1;
    public GameObject TreeLv2;
    public GameObject TreeLv3;

    [Header("UI Prefabs (Resources)")]
    public string treeInfoResourcePath = "Prefabs/Tree/TreeXPAndRelate/TreeInfo";
    public string claimButtonResourcePath = "Prefabs/Tree/TreeXPAndRelate/ClaimButton";
    public Canvas environmentCanvas;

    private TreeInfoUI infoUI;
    private RectTransform infoRect;
    private Button claimBtn;
    private ItemClass item;
    private SpriteRenderer sr;

    private bool isClaimReady = false; // Ngăn TreeInfo khi cây đạt claim

    private long lastWT = 0, lastFR = 0, lastPS = 0;
    private const int WT_XP = 20, FR_XP = 40, PS_XP = 40;
    private static readonly TimeSpan WT_CD = TimeSpan.FromMinutes(0.2);
    private static readonly TimeSpan FR_CD = TimeSpan.FromHours(0.001);
    private static readonly TimeSpan PS_CD = TimeSpan.FromHours(0.001);


    [Header("Raycast")]
    public LayerMask treeMask = Physics2D.DefaultRaycastLayers;

    private void Awake()
    {
        item = GetComponent<ItemClass>();
        sr = GetComponent<SpriteRenderer>();

        if (!environmentCanvas)
            environmentCanvas = UnityEngine.Object.FindFirstObjectByType<Canvas>();

        level = Mathf.Clamp(level, 0, 3);
        currentXP = Mathf.Clamp(currentXP, 0, xpToLevel);
        UpdateVisualForLevel();
    }

    private void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            Vector2 mouseWorldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            RaycastHit2D[] hits = Physics2D.RaycastAll(mouseWorldPos, Vector2.zero, Mathf.Infinity, treeMask);

            foreach (var hit in hits)
            {
                TreeGrowth tg = hit.collider?.GetComponent<TreeGrowth>();
                if (tg != null)
                {
                    tg.OnTreeClicked();
                    break;
                }
            }
        }

        if (infoRect && environmentCanvas)
        {
            Vector3 worldPos = transform.position + new Vector3(0, 1.5f, 0);
            Vector3 screenPos = Camera.main.WorldToScreenPoint(worldPos);
            Vector2 localPos;

            RectTransform canvasRect = environmentCanvas.transform as RectTransform;
            if (environmentCanvas.renderMode == RenderMode.ScreenSpaceOverlay)
                RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screenPos, null, out localPos);
            else
                RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screenPos, environmentCanvas.worldCamera, out localPos);

            infoRect.anchoredPosition = localPos;
            if (claimBtn)
            {
                RectTransform claimRect = claimBtn.GetComponent<RectTransform>();
                claimRect.anchoredPosition = localPos + new Vector2(0, -80f);
            }
        }

        RefreshTimersOnly();
    }

    public void OnTreeClicked()
    {
        if (isClaimReady)
        {
            transform.DOPunchScale(Vector3.one * 0.05f, 0.15f, 8, 0.8f);
            Debug.Log("[TreeGrowth] Cây đã đạt cấp claim, không mở TreeInfo nữa.");
            return;
        }

        transform.DOPunchScale(Vector3.one * 0.05f, 0.15f, 8, 0.8f);

        if (infoRect == null)
            AttachUI();
        else
        {
            bool visible = infoRect.gameObject.activeSelf;
            infoRect.gameObject.SetActive(!visible);
            if (claimBtn) claimBtn.gameObject.SetActive(!visible);
        }
    }

    public void ApplyTool(string toolId)
    {
        if (level >= 3 && currentXP >= xpToLevel)
        {
            ShowClaim(true);
            return;
        }

        int addXP = 0;
        bool canUse = false;
        long now = DateTimeOffset.UtcNow.Ticks;

        switch (toolId)
        {
            case "WT": canUse = (now - lastWT) >= WT_CD.Ticks; if (canUse) { addXP = WT_XP; lastWT = now; } break;
            case "FR": canUse = (now - lastFR) >= FR_CD.Ticks; if (canUse) { addXP = FR_XP; lastFR = now; } break;
            case "PS": canUse = (now - lastPS) >= PS_CD.Ticks; if (canUse) { addXP = PS_XP; lastPS = now; } break;
        }

        if (!canUse)
        {
            infoRect?.DOShakeScale(0.2f, 0.1f, 10, 90f);
            return;
        }

        GainXP(addXP);
    }

    private void GainXP(int xp)
    {
        int from = currentXP;
        currentXP = Mathf.Clamp(currentXP + xp, 0, xpToLevel);

        infoUI?.TweenXP(from, currentXP, xpToLevel);
        infoRect?.DOScale(1.08f, 0.12f).SetLoops(2, LoopType.Yoyo);

        while (currentXP >= xpToLevel && level < 3)
        {
            currentXP -= xpToLevel;
            LevelUp();
        }

        if (level >= 3 && currentXP >= xpToLevel)
            ShowClaim(true);

        RefreshUI();
    }

    private void LevelUp()
    {
        level = Mathf.Clamp(level + 1, 0, 3);
        StartCoroutine(DoLevelUpTransition());
    }

    private IEnumerator DoLevelUpTransition()
    {
        if (sr)
        {
            sr.DOFade(0f, 0.3f);
            sr.DOColor(Color.yellow, 0.2f).SetLoops(2, LoopType.Yoyo);
        }

        transform.DOScale(0.8f, 0.3f).SetEase(Ease.InOutSine);
        yield return new WaitForSeconds(0.35f);

        DOTween.Kill(sr, false);
        DOTween.Kill(transform, false);

        ReplaceWithPrefabForLevel(level);
    }

    private void ReplaceWithPrefabForLevel(int lv)
    {
        GameObject prefab = lv switch
        {
            0 => SeedPrefab,
            1 => TreeLv1,
            2 => TreeLv2,
            _ => TreeLv3
        };

        if (!prefab)
        {
            Debug.LogWarning($"⚠️ Không tìm thấy prefab cho cấp {lv}");
            return;
        }

        Vector3 pos = transform.position;
        Quaternion rot = transform.rotation;
        var parent = transform.parent;
        var oldXP = currentXP;
        var oldCanvas = environmentCanvas;

        if (infoRect) Destroy(infoRect.gameObject);
        if (claimBtn) Destroy(claimBtn.gameObject);

        DOTween.Kill(sr, false);
        DOTween.Kill(transform, false);

        Destroy(gameObject);

        var newTree = Instantiate(prefab, pos, rot, parent);
        var growth = newTree.GetComponent<TreeGrowth>() ?? newTree.AddComponent<TreeGrowth>();
        growth.level = lv;
        growth.currentXP = oldXP;
        growth.environmentCanvas = oldCanvas;
        growth.baseId = baseId;
        growth.SeedPrefab = SeedPrefab;
        growth.TreeLv1 = TreeLv1;
        growth.TreeLv2 = TreeLv2;
        growth.TreeLv3 = TreeLv3;
        growth.treeInfoResourcePath = treeInfoResourcePath;
        growth.claimButtonResourcePath = claimButtonResourcePath;

        growth.sr = newTree.GetComponent<SpriteRenderer>() ?? newTree.AddComponent<SpriteRenderer>();
        growth.UpdateVisualForLevel();

        var newSR = growth.sr;
        if (newSR)
        {
            newSR.color = new Color(1, 1, 1, 0);
            newSR.DOFade(1f, 0.5f).SetEase(Ease.OutSine);
        }

        Vector3 prefabScale = prefab.transform.localScale;
        newTree.transform.localScale = Vector3.zero;
        newTree.transform
            .DOScale(prefabScale * 1.2f, 0.4f)
            .SetEase(Ease.OutBack)
            .OnComplete(() => newTree.transform.DOScale(prefabScale, 0.2f));
    }

    private void UpdateVisualForLevel()
    {
        if (!sr) sr = GetComponent<SpriteRenderer>();
        GameObject src = level switch
        {
            0 => SeedPrefab,
            1 => TreeLv1,
            2 => TreeLv2,
            _ => TreeLv3
        };
        if (src)
        {
            var s = src.GetComponent<SpriteRenderer>();
            if (s) sr.sprite = s.sprite;
        }
        if (item != null)
        {
            item.itemId = $"{baseId}{level}";
            item.type = (level == 0 ? "Seed" : "Tree");
        }
    }

    private void AttachUI()
    {
        if (!environmentCanvas || environmentCanvas.gameObject.scene.rootCount == 0)
            environmentCanvas = UnityEngine.Object.FindFirstObjectByType<Canvas>();

        if (!environmentCanvas)
        {
            Debug.LogError("[TreeGrowth] Không tìm thấy Canvas trong Scene!");
            return;
        }

        var uiPrefab = Resources.Load<GameObject>(treeInfoResourcePath);
        if (!uiPrefab)
        {
            Debug.LogError($"Không thấy prefab TreeInfo tại Resources/{treeInfoResourcePath}");
            return;
        }

        var go = Instantiate(uiPrefab, environmentCanvas.transform);
        go.transform.localScale = Vector3.one;

        infoRect = go.GetComponent<RectTransform>();
        infoUI = go.GetComponent<TreeInfoUI>() ?? go.AddComponent<TreeInfoUI>();

        infoUI.Bind(this);
        infoUI.SetName(baseId);
        infoUI.SetLevel(level);
        infoUI.SetXP(currentXP, xpToLevel);

        infoRect.localScale = Vector3.zero;
        infoRect.DOScale(1f, 0.25f).SetEase(Ease.OutBack);

        ShowClaim(false);
    }

    private void ShowClaim(bool show)
    {
        if (!environmentCanvas) return;

        if (show)
        {
            isClaimReady = true;

            if (infoRect)
                infoRect.gameObject.SetActive(false);

            if (claimBtn == null)
            {
                var btnPrefab = Resources.Load<GameObject>(claimButtonResourcePath);
                if (!btnPrefab)
                {
                    Debug.LogWarning($"Không thấy prefab ClaimButton tại Resources/{claimButtonResourcePath}");
                    return;
                }

                var btnGO = Instantiate(btnPrefab, environmentCanvas.transform);
                btnGO.transform.localScale = Vector3.one;

                claimBtn = btnGO.GetComponent<Button>();
                claimBtn.onClick.RemoveAllListeners();
                claimBtn.onClick.AddListener(Claim);
            }

            claimBtn.transform.DOPunchScale(Vector3.one * 0.1f, 0.35f, 8, 0.8f).SetLoops(-1, LoopType.Restart);
        }
        else
        {
            isClaimReady = false;

            if (claimBtn)
            {
                claimBtn.transform.DOKill();
                Destroy(claimBtn.gameObject);
                claimBtn = null;
            }
        }
    }

    private void Claim()
    {
        Debug.Log("[TreeGrowth] Claim() called!");

        if (PlantLogTabManage.Instance == null)
        {
            var found = FindAnyObjectByType<PlantLogTabManage>(FindObjectsInactive.Include);
            if (found != null)
            {
                PlantLogTabManage.Instance = found;
                Debug.Log("[TreeGrowth] ✅ Đã tìm thấy PlantLogTabManage (inactive).");
            }
        }

        var data = new ClaimedTreeData
        {
            sprite = sr ? sr.sprite : null,
            name = baseId,
            level = level,
            itemId = item != null ? item.itemId : $"{baseId}{level}"
        };

        DOTween.KillAll();

        if (infoRect) Destroy(infoRect.gameObject);
        if (claimBtn) Destroy(claimBtn.gameObject);

        if (sr != null)
            sr.DOFade(0, 0.25f).OnComplete(() =>
            {
                StartCoroutine(ClaimAfterDelay(data));
            });
        else
            StartCoroutine(ClaimAfterDelay(data));
    }

    private IEnumerator ClaimAfterDelay(ClaimedTreeData data)
    {
        yield return new WaitForEndOfFrame();

        if (PlantLogTabManage.Instance != null)
        {
            PlantLogTabManage.Instance.AddToCollection(data);
            Debug.Log("[TreeGrowth] ✅ Đã gửi data claim cho PlantLogTabManage.");
        }
        else
        {
            Debug.LogWarning("[TreeGrowth] ❌ PlantLogTabManage.Instance vẫn null sau khi chờ!");
        }

        Destroy(gameObject);
    }

    private void RefreshUI()
    {
        infoUI?.SetLevel(level);
        infoUI?.SetXP(currentXP, xpToLevel);
        RefreshTimersOnly();
    }

    private void RefreshTimersOnly()
    {
        if (infoUI == null) return;
        long now = DateTimeOffset.UtcNow.Ticks;
        infoUI.SetCooldowns(
            Remaining(now, lastWT, WT_CD),
            Remaining(now, lastFR, FR_CD),
            Remaining(now, lastPS, PS_CD));
    }

    private static TimeSpan Remaining(long now, long last, TimeSpan cd)
    {
        if (last == 0) return TimeSpan.Zero;
        long left = (last + cd.Ticks) - now;
        if (left <= 0) return TimeSpan.Zero;
        return new TimeSpan(left);
    }
}

[System.Serializable]
public struct ClaimedTreeData
{
    public Sprite sprite;
    public string name;
    public int level;
    public string itemId;
}
