using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneChanger : MonoBehaviour
{
    public void OnStartButtonClicked()
    {
        if (SfxPlayer.Instance != null)
            SfxPlayer.Instance.PlayUiClick();

        SceneManager.LoadScene("GameScene");
    }
}