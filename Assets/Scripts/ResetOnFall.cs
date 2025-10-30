using UnityEngine;
using UnityEngine.SceneManagement;

public class ResetOnFall : MonoBehaviour
{
    // Evita reinicios múltiples si el botón se presiona rápidamente
    private bool isResetting = false;

    // Método público para asignar al Button -> OnClick()
    public void ResetScene()
    {
        if (isResetting) return;
        isResetting = true;

        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}
