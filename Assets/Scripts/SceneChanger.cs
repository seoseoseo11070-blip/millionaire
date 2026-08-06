using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneChanger : MonoBehaviour
{

    public void OnStartButtonClicked()
    {
        SceneManager.LoadScene(1);
    }
}