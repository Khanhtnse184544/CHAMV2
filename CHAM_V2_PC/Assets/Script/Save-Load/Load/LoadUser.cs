using TMPro;
using UnityEngine;

public class LoadUser : MonoBehaviour
{
    private UserProfile user;
    public TMP_Text userLevel;
    public TMP_Text userCoin;
    //public TMP_Text userName;
    public TMP_Text expPerLevel;
    public TMP_Text level;
    public RectTransform realXP;
    private RectTransform parentXpBar;
    private void Start()
    {
        parentXpBar = realXP.parent.GetComponent<RectTransform>();
        Vector2 sizeXpParent = parentXpBar.rect.size;
        Debug.Log(sizeXpParent.x);
        user = UserSession.currentUser;
        userCoin.text = user.coin.ToString();
        //userName.text = user.userName.ToString();
        expPerLevel.text = user.expPerLevel.ToString();
        level.text = user.level.ToString();
        Vector2 size = realXP.sizeDelta;
        size.x = ((float)user.expPerLevel / 100) * sizeXpParent.x;
        realXP.sizeDelta = size;
    }
}
