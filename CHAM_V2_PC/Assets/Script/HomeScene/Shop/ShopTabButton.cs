using UnityEngine;
using UnityEngine.UI;

public class ShopTabButton : MonoBehaviour
{
    public ShopManager shopManager;                // Tham chiếu tới ShopManager
    public ShopManager.ShopTab tab;                // Tab nào sẽ mở khi click nút này

    void Start()
    {
        // Bắt sự kiện click của nút
        GetComponent<Button>().onClick.AddListener(() => shopManager.OpenTab(tab));
    }
}
