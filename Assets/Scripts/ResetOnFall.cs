using UnityEngine;
using UnityEngine.SceneManagement;
// Necesario para aceptar el parámetro que envía HurricaneVR
using HurricaneVR.Framework.Components;

public class ResetOnFall : MonoBehaviour
{
    // Evita reinicios múltiples si el botón se presiona rápidamente
    private bool isResetting = false;

    // Método público sin parámetros (útil para UI Button -> OnClick)
    public void ResetScene()
    {
        if (isResetting) return;
        isResetting = true;

        // Usamos el FadeController para hacer fade out -> recargar -> fade in
        // Usamos la versión estática que crea el controller si no existe.
        FadeController.ReloadSceneStatic();
    }

    // Sobrecarga compatible con HurricaneVR's HVRButtonEvent (UnityEvent<HVRPhysicsButton>)
    // Esto permite asignar el método directamente al "Button Down" del HVRPhysicsButton desde el Inspector.
    public void ResetScene(HVRPhysicsButton sender)
    {
        // simplemente delegamos a la versión sin parámetros
        ResetScene();
    }
}
