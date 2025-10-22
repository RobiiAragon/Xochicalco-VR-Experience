using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Video;

// Asegura que los componentes necesarios existan en el GameObject para evitar errores.
[RequireComponent(typeof(VideoPlayer))]
[RequireComponent(typeof(Collider))]
public class Videos : MonoBehaviour
{
    // Usamos [SerializeField] en lugar de 'public' para exponer variables al Inspector.
    // Es una mejor práctica que mantiene el código más encapsulado y seguro.
    [Header("Configuración de Video y Luces")]
    [SerializeField] private VideoPlayer videoPlayer;
    [SerializeField] private List<Light> sceneLights;
    [SerializeField] private Light specialLight;

    [Header("Configuración de la Transición")]
    [Tooltip("Duración en segundos del apagado y encendido de luces.")]
    [SerializeField] private float fadeDuration = 1.0f;

    // Variables privadas para el estado interno del script.
    private float[] _initialSceneIntensities;
    private float _specialLightInitialIntensity;
    private bool _hasPlayed = false;
    private Coroutine _fadeCoroutine;

    // Cacheamos esta instrucción para no generar basura en el bucle de la corrutina.
    // Es una micro-optimización importante en plataformas móviles como Quest.
    private readonly WaitForEndOfFrame _waitForEndOfFrame = new WaitForEndOfFrame();

    // Awake se llama antes que Start. Es ideal para inicializar referencias.
    private void Awake()
    {
        // Obtenemos la referencia al VideoPlayer una sola vez.
        videoPlayer = GetComponent<VideoPlayer>();
        videoPlayer.Stop(); // Aseguramos que el video no se autoinicie.

        // Guardamos las intensidades iniciales para poder restaurarlas después.
        if (sceneLights != null)
        {
            _initialSceneIntensities = new float[sceneLights.Count];
            for (int i = 0; i < sceneLights.Count; i++)
            {
                if (sceneLights[i] != null)
                {
                    _initialSceneIntensities[i] = sceneLights[i].intensity;
                }
            }
        }

        if (specialLight != null)
        {
            _specialLightInitialIntensity = specialLight.intensity;
            specialLight.intensity = 0f; // La luz especial siempre inicia apagada.
        }
    }

    // Es una buena práctica suscribirse a eventos en OnEnable y desuscribirse en OnDisable.
    // Esto evita errores si el objeto se activa y desactiva en tiempo de ejecución.
    private void OnEnable()
    {
        videoPlayer.loopPointReached += OnVideoEnd;
    }

    private void OnDisable()
    {
        videoPlayer.loopPointReached -= OnVideoEnd;
    }

    private void OnTriggerEnter(Collider other)
    {
        // Comprobamos que el video no se haya reproducido ya y que no esté en curso.
        if (!_hasPlayed && !videoPlayer.isPlaying)
        {
            _hasPlayed = true;
            // Inicia la secuencia: apagar luces, encender luz especial y reproducir video.
            FadeLightsAndPlay();
        }
    }

    private void OnVideoEnd(VideoPlayer vp)
    {
        // Al terminar el video, restauramos las luces a su estado original.
        RestoreInitialLights();
    }

    private void FadeLightsAndPlay()
    {
        // Creamos un array con las intensidades objetivo para las luces de la escena (todas a 0).
        float[] targetIntensities = new float[sceneLights.Count];
        for (int i = 0; i < targetIntensities.Length; i++)
        {
            targetIntensities[i] = 0f;
        }

        // Llamamos a nuestra corrutina unificada.
        // El video se reproducirá cuando la transición de luces termine (gracias al callback).
        StartFade(targetIntensities, _specialLightInitialIntensity, () => videoPlayer.Play());
    }

    private void RestoreInitialLights()
    {
        // La intensidad objetivo de la luz especial es 0 para apagarla.
        // Las luces de la escena vuelven a sus intensidades originales.
        StartFade(_initialSceneIntensities, 0f, null);
    }

    private void StartFade(float[] sceneTargetIntensities, float specialTargetIntensity, System.Action onComplete)
    {
        // Si ya hay una transición en curso, la detenemos para empezar la nueva.
        // Esto evita comportamientos extraños si los eventos se disparan muy rápido.
        if (_fadeCoroutine != null)
        {
            StopCoroutine(_fadeCoroutine);
        }
        _fadeCoroutine = StartCoroutine(FadeLightsRoutine(sceneTargetIntensities, specialTargetIntensity, onComplete));
    }

    // Esta es la única corrutina, ahora es más genérica y reutilizable.
    private IEnumerator FadeLightsRoutine(float[] sceneTargetIntensities, float specialTargetIntensity, System.Action onComplete)
    {
        float elapsedTime = 0f;

        // Guardamos las intensidades ACTUALES al inicio de la transición.
        // Esto es clave para un 'Lerp' correcto y lineal.
        float[] startIntensities = new float[sceneLights.Count];
        for (int i = 0; i < sceneLights.Count; i++)
        {
            startIntensities[i] = sceneLights[i] != null ? sceneLights[i].intensity : 0;
        }
        float specialStartIntensity = specialLight != null ? specialLight.intensity : 0;

        while (elapsedTime < fadeDuration)
        {
            // Calculamos el progreso de 0 a 1 de forma lineal.
            float t = elapsedTime / fadeDuration;

            // Actualizamos la intensidad de cada luz de la escena.
            for (int i = 0; i < sceneLights.Count; i++)
            {
                if (sceneLights[i] != null)
                {
                    sceneLights[i].intensity = Mathf.Lerp(startIntensities[i], sceneTargetIntensities[i], t);
                }
            }

            // Actualizamos la luz especial.
            if (specialLight != null)
            {
                specialLight.intensity = Mathf.Lerp(specialStartIntensity, specialTargetIntensity, t);
            }

            elapsedTime += Time.deltaTime;
            yield return _waitForEndOfFrame; // Usamos nuestra variable cacheada para no generar basura.
        }

        // Al final del bucle, nos aseguramos de que todas las luces tengan exactamente su valor final.
        for (int i = 0; i < sceneLights.Count; i++)
        {
            if (sceneLights[i] != null)
            {
                sceneLights[i].intensity = sceneTargetIntensities[i];
            }
        }
        if (specialLight != null)
        {
            specialLight.intensity = specialTargetIntensity;
        }

        // Ejecutamos la acción final (como video.Play()) si existe.
        onComplete?.Invoke();
        _fadeCoroutine = null;
    }
}