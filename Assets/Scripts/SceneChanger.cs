using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneChanger : MonoBehaviour
{
    public void OnStartButtonClicked()
    {
        if (!TitleDifficultySelector.IsCurrentDifficultyPlayable())
        {
            Debug.Log("この強さではまだ遊べません");
            return;
        }

        SceneManager.LoadScene(0);
    }
}