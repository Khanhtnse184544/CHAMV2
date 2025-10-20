using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;

public class LoginManager : MonoBehaviour
{
    [Header("UI References")]
    public TMP_InputField usernameInput;
    public TMP_InputField passwordInput;
    //private Text messageText; // Text hiển thị thông báo login


    private string apiBaseUrl = "https://apigame-e8g0a8cyc2b2hseg.eastasia-01.azurewebsites.net/api/User/Login"; // đổi thành API thật


    private string hardcodeUser = "admin";
    private string hardcodePass = "123456";

    /// <summary>
    /// Hàm được gọi khi bấm nút Login
    /// </summary>
    public void OnLoginButton()
    {
        string user = usernameInput.text.Trim();
        string pass = passwordInput.text.Trim();

        // Nếu để test offline (Hardcode)
        if (string.IsNullOrEmpty(apiBaseUrl))
        {
            CheckLoginHardcode(user, pass);
            Debug.Log("HomeScene");
        }
        else
        {
            StartCoroutine(CheckLoginAPI(user, pass));
        }
    }

    /// <summary>
    /// Login hardcode cho test nhanh
    /// </summary>
    private void CheckLoginHardcode(string user, string pass)
    {
        if (user == hardcodeUser && pass == hardcodePass)
        {
            //messageText.text = "✅ Login success (Hardcode)";
            Debug.Log("Login success (Hardcode)");
            // TODO: Load scene / mở UI mới
            SceneManager.LoadScene("HomeScene");
        }
        else
        {
            //messageText.text = "❌ Invalid username or password";
        }
    }

    /// <summary>
    /// Gọi API check login (sử dụng UnityWebRequest)
    /// </summary>
    private IEnumerator CheckLoginAPI(string user, string pass)
    {
        // ⚙️ Tạo body JSON đúng với UserDAO
        string jsonData = $"{{\"email\":\"{user}\",\"password\":\"{pass}\"}}";
        byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonData);

        using (UnityWebRequest www = new UnityWebRequest(apiBaseUrl, "POST"))
        {
            www.uploadHandler = new UploadHandlerRaw(bodyRaw);
            www.downloadHandler = new DownloadHandlerBuffer();
            www.SetRequestHeader("Content-Type", "application/json");
            www.timeout = 10;

            Debug.Log("🔹 Sending login request...");

            yield return www.SendWebRequest();

            if (www.result == UnityWebRequest.Result.Success)
            {
                string response = www.downloadHandler.text;
                Debug.Log("Response: " + response);

                try
                {
                    // Parse JSON trả về sang UserProfile
                    UserProfile userProfile = JsonUtility.FromJson<UserProfile>(response);

                    if (userProfile != null && !string.IsNullOrEmpty(userProfile.userId))
                    {
                        Debug.Log($"✅ Login success! User: {userProfile.userName}, Coin: {userProfile.coin}");
                        UserSession.currentUser = userProfile;
                        SceneManager.LoadScene("HomeScene");
                    }
                    else
                    {
                        Debug.LogWarning("⚠️ Login failed: Sai thông tin hoặc parse lỗi.");
                    }
                }
                catch (System.Exception ex)
                {
                    Debug.LogError("❌ JSON parse error: " + ex.Message);
                }
            }
            else
            {
                Debug.LogError($"⚠️ Network/API error: {www.error}");
            }
        }
    }

}
