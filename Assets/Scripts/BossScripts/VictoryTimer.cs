using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

// Script auxiliar que sobrevive entre escenas para redirigir al menú
public class VictoryTimer : MonoBehaviour
{
    public void Init(float duration, string menuScene)
    {
        StartCoroutine(GoToMenu(duration, menuScene));
    }

    IEnumerator GoToMenu(float duration, string menuScene)
    {
        yield return new WaitForSeconds(duration);
        Destroy(gameObject);
        SceneManager.LoadScene(menuScene);
    }
}