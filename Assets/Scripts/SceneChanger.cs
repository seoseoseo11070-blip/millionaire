using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneChanger : MonoBehaviour
{
    public void OnStartButtonClicked()
    {
        Debug.Log("ゲームシーンへ");
        SceneManager.LoadScene(0);
    }
}