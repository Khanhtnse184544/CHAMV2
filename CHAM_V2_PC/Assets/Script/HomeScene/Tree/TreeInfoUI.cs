using DG.Tweening;
using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class TreeInfoUI : MonoBehaviour
{
    public Image xpBar;
    public TextMeshProUGUI xpText;
    public TextMeshProUGUI levelText;
    public TextMeshProUGUI treeName;
    public TextMeshProUGUI timeWater;
    public TextMeshProUGUI timeFer;
    public TextMeshProUGUI timePes;

    private TreeGrowth bound;

    public void Bind(TreeGrowth growth)
    {
        bound = growth;

        if (!xpBar) xpBar = transform.Find("XPbar/RealXP")?.GetComponent<Image>();
        if (!xpText) xpText = transform.Find("XPbar/Xp100")?.GetComponent<TextMeshProUGUI>();
        if (!levelText) levelText = transform.Find("XPbar/Start/Level")?.GetComponent<TextMeshProUGUI>();
        if (!treeName) treeName = transform.Find("TreeName")?.GetComponent<TextMeshProUGUI>();
        if (!timeWater) timeWater = transform.Find("Watercan")?.GetComponentInChildren<TextMeshProUGUI>();
        if (!timeFer) timeFer = transform.Find("Fertilizer")?.GetComponentInChildren<TextMeshProUGUI>();
        if (!timePes) timePes = transform.Find("Pesticides")?.GetComponentInChildren<TextMeshProUGUI>();

        // Debug để kiểm tra nếu prefab thiếu gì đó
        if (!xpBar || !xpText || !levelText || !treeName)
            Debug.LogError("[TreeInfoUI] Một hoặc nhiều thành phần UI bị thiếu trong prefab TreeInfo!");
    }


    public void SetName(string name) => treeName.text = name;
    public void SetLevel(int lv) => levelText.text = lv.ToString();

    public void SetXP(int current, int max)
    {
        xpBar.fillAmount = max <= 0 ? 0 : (float)current / max;
        xpText.text = $"{current}/{max}";
    }

    public void TweenXP(int from, int to, int max)
    {
        if (!xpBar) return;
        float start = max <= 0 ? 0 : (float)from / max;
        float end = max <= 0 ? 0 : (float)to / max;

        xpBar.fillAmount = start;
        xpBar.DOFillAmount(end, 0.35f);
        DOTween.To(() => from, v => xpText.text = $"{v}/{max}", to, 0.35f);
    }

    public void SetCooldowns(TimeSpan wtRemain, TimeSpan frRemain, TimeSpan psRemain)
    {
        if (timeWater) timeWater.text = FormatRemain(wtRemain);
        if (timeFer) timeFer.text = FormatRemain(frRemain);
        if (timePes) timePes.text = FormatRemain(psRemain);
    }

    private string FormatRemain(TimeSpan t)
    {
        if (t == TimeSpan.Zero) return "Ready";
        if (t.TotalHours >= 1) return $"{(int)t.TotalHours:D2}:{t.Minutes:D2}h";
        return $"{t.Minutes:D2}:{t.Seconds:D2}m";
    }
}
