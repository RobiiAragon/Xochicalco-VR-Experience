using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

/// <summary>
/// Singleton que gestiona un fullscreen fade (Image) y permite recargar la escena con una transición.
/// Si no hay un Image asignado en el inspector, crea un Canvas + Image en tiempo de ejecución.
/// El objeto se marca DontDestroyOnLoad para sobrevivir a la recarga de escena.
/// </summary>
public class FadeController : MonoBehaviour
{
    public static FadeController Instance { get; private set; }

    [Tooltip("Image usada para el fade; si está vacía, se creará una automáticamente en Awake.")]
    public Image fadeImage;

    [Tooltip("Duración total del efecto (segundos). Se divide en fade-out y fade-in).")]
    public float defaultDuration = 0.8f;

    bool isFading = false;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        if (fadeImage == null)
        {
            CreateFullscreenCanvasAndImage();
        }

        // Ensure start transparent
        if (fadeImage != null)
        {
            var c = fadeImage.color;
            c.a = 0f;
            fadeImage.color = c;
            fadeImage.raycastTarget = false;
        }
    }

    void CreateFullscreenCanvasAndImage()
    {
        var canvasGO = new GameObject("FadeCanvas");
        canvasGO.transform.SetParent(transform);
        var canvas = canvasGO.AddComponent<Canvas>();

        // Intentar usar la cámara principal (útil para VR/OpenXR). Si no está disponible, usar Overlay y reintentar más tarde.
        Camera mainCam = Camera.main;
        if (mainCam != null)
        {
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = mainCam;
            canvas.planeDistance = Mathf.Max(0.01f, mainCam.nearClipPlane + 0.01f);
        }
        else
        {
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        }

        canvas.sortingOrder = 1000; // encima de todo

        canvasGO.AddComponent<CanvasScaler>();
        canvasGO.AddComponent<GraphicRaycaster>();

        var imageGO = new GameObject("FadeImage");
        imageGO.transform.SetParent(canvasGO.transform, false);
        var image = imageGO.AddComponent<Image>();
        image.color = Color.black;

        var rt = image.rectTransform;
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        fadeImage = image;

        // Si no había cámara en Awake, reintentar unos frames para enlazar al empezar la escena (útil en setups VR donde la cámara se crea después)
        if (mainCam == null)
        {
            StartCoroutine(TryAttachToCameraCoroutine(canvasGO));
        }
    }

    System.Collections.IEnumerator TryAttachToCameraCoroutine(GameObject canvasGO)
    {
        // Reintenta unos pocos frames para encontrar la cámara principal y convertir el canvas a ScreenSpaceCamera.
        int attempts = 0;
        while (attempts < 30)
        {
            attempts++;
            yield return null;
            var cam = Camera.main;
            if (cam != null)
            {
                var canvas = canvasGO.GetComponent<Canvas>();
                if (canvas != null)
                {
                    canvas.renderMode = RenderMode.ScreenSpaceCamera;
                    canvas.worldCamera = cam;
                    canvas.planeDistance = Mathf.Max(0.01f, cam.nearClipPlane + 0.01f);
                }
                // Parentear al transform de la cámara para que siga cualquier ajuste de XR rig
                try
                {
                    canvasGO.transform.SetParent(cam.transform, false);
                }
                catch { }
                yield break;
            }
        }
    }

    /// <summary>
    /// Recarga la escena actual con un fade-out, recarga asincrónica y fade-in.
    /// </summary>
    public void ReloadScene(float duration = -1f)
    {
        if (duration <= 0f) duration = defaultDuration;
        if (isFading) return;
        StartCoroutine(FadeAndReloadCoroutine(duration));
    }

    /// <summary>
    /// Garantiza que exista una instancia y lanza la recarga con fade.
    /// Crea un GameObject con este componente si hace falta (útil para uso desde código donde no se haya añadido el controller en la escena).
    /// </summary>
    public static void ReloadSceneStatic(float duration = -1f)
    {
        if (Instance == null)
        {
            var go = new GameObject("FadeController");
            // Añadir componente inicializará Instance en Awake
            Instance = go.AddComponent<FadeController>();
        }

        Instance.ReloadScene(duration);
    }

    IEnumerator FadeAndReloadCoroutine(float duration)
    {
        isFading = true;

        // mitad para fade out, mitad para fade in
        float half = Mathf.Max(0.01f, duration * 0.5f);

        yield return StartCoroutine(Fade(0f, 1f, half));

        // Empieza la carga asincrónica
        var op = SceneManager.LoadSceneAsync(SceneManager.GetActiveScene().buildIndex);
        // allowSceneActivation por defecto true; esperar a que termine
        while (!op.isDone)
        {
            yield return null;
        }


        // pequeña espera para asegurar que la nueva escena esté lista (opcional)
        yield return null;

        // Re-attach canvas to the new main camera (si existe). En algunos rigs VR la cámara se crea al cargar la escena,
        // así que esperamos unos frames y volvemos a enlazar el Canvas para que el fade-in sea visible en el nuevo visor.
        int attempts = 0;
        while (attempts < 30 && Camera.main == null)
        {
            attempts++;
            yield return null;
        }

        AttachCanvasToMainCamera();

        yield return StartCoroutine(Fade(1f, 0f, half));

        isFading = false;
    }

    void AttachCanvasToMainCamera()
    {
        if (fadeImage == null) return;

        var canvas = fadeImage.GetComponentInParent<Canvas>();
        if (canvas == null) return;

        var cam = Camera.main;
        if (cam != null)
        {
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = cam;
            canvas.planeDistance = Mathf.Max(0.01f, cam.nearClipPlane + 0.01f);
            // Opcional: parentear al transform de la cámara para seguir movimientos del rig
            try { canvas.transform.SetParent(cam.transform, false); } catch { }
        }
    }

    IEnumerator Fade(float from, float to, float duration)
    {
        if (fadeImage == null)
        {
            yield break;
        }

        fadeImage.raycastTarget = true; // bloquear input durante la transición

        float elapsed = 0f;
        Color c = fadeImage.color;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime; // usar tiempo no escalado para UI
            float t = Mathf.Clamp01(elapsed / duration);
            float a = Mathf.Lerp(from, to, t);
            c.a = a;
            fadeImage.color = c;
            yield return null;
        }

        c.a = to;
        fadeImage.color = c;
        fadeImage.raycastTarget = (to > 0.01f);
    }
}
