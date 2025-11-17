using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneLoader : MonoBehaviour
{
    // Cargar escena por nombre (uso desde Button OnClick -> LoadScene (String))
    public void LoadScene(string sceneName)
    {
        if (string.IsNullOrEmpty(sceneName))
        {
            Debug.LogWarning("SceneLoader: sceneName está vacío.");
            return;
        }
        // Asegúrate de añadir la escena a Build Settings
        SceneManager.LoadScene(sceneName);
    }

    // Método opcional sin parámetros si prefieres asignar funciones individuales
    public void LoadSceneByIndex(int sceneIndex)
    {
        SceneManager.LoadScene(sceneIndex);
    }

    // Método opcional para salir del juego (útil en builds)
    public void QuitGame()
    {
        Application.Quit();
    }

    // Nuevos métodos: asignables directamente desde Button -> SceneLoader -> LoadMuseo / LoadAventura
    public void LoadMuseo()
    {
        SceneManager.LoadScene("Museo");
    }

    public void LoadAventura()
    {
        SceneManager.LoadScene("Aventura");
    }

    public void LoadMainMenu()
    {
        SceneManager.LoadScene("MainMenu");
    }
}
