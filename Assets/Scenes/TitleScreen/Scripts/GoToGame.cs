using UnityEngine;
using UnityEngine.SceneManagement;

public class GoToGame : MonoBehaviour
{
    [SerializeField] string nameOfFirstScene;

    public void Go()
    {
        SceneManager.LoadSceneAsync(nameOfFirstScene);
    }
}
