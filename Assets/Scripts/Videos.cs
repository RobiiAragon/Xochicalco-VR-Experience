using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Video;

public class Videos : MonoBehaviour
{
    public VideoPlayer video;
    public List<Light> lights; // Lista de luces en la escena
    public float fadeDuration = 1.0f; // Duración del cambio gradual de intensidad
    private Dictionary<Light, float> initialIntensities = new Dictionary<Light, float>(); // Estado inicial de las luces
    public Light specialLight; // Foco que inicia apagado
    private float specialLightInitialIntensity; // Intensidad inicial del foco especial

    // Se ejecuta al iniciar el script
    void Start()
    {
        // Obtiene el componente VideoPlayer de este objeto
        video = GetComponent<VideoPlayer>();
        // Detiene el video al iniciar
        video.Stop();

        // Guardar la intensidad inicial de cada luz
        foreach (Light light in lights)
        {
            initialIntensities[light] = light.intensity;
        }

        // Guardar la intensidad inicial del foco especial
        if (specialLight != null)
        {
            specialLightInitialIntensity = specialLight.intensity;
            specialLight.intensity = 0; // Asegurarse de que esté apagado al inicio
        }

        // Suscribirse al evento que se dispara cuando el video termina
        video.loopPointReached += OnVideoEnd;
    }

    // Se ejecuta cuando otro objeto con un Collider entra en el trigger
    private void OnTriggerEnter(Collider other)
    {
        // Reproduce el video solo si no se está reproduciendo
        if (!video.isPlaying)
        {
            video.Play();
            StartCoroutine(FadeLights(0, true)); // Apagar las luces gradualmente y encender el foco especial
        }
    }

    // Se ejecuta cuando otro objeto con un Collider sale del trigger
    private void OnTriggerExit(Collider other)
    {
        // No hacer nada al salir del trigger, esperar a que el video termine
    }

    // Se ejecuta cuando el video termina
    private void OnVideoEnd(VideoPlayer vp)
    {
        StartCoroutine(FadeLightsBackToOriginal()); // Restaurar las luces gradualmente
    }

    // Corrutina para restaurar las luces a su intensidad inicial
    private IEnumerator FadeLightsBackToOriginal()
    {
        float startTime = Time.time;

        // Intensidad actual de las luces
        Dictionary<Light, float> currentIntensities = new Dictionary<Light, float>();
        foreach (Light light in lights)
        {
            currentIntensities[light] = light.intensity;
        }

        float specialLightCurrentIntensity = specialLight != null ? specialLight.intensity : 0;

        while (Time.time - startTime < fadeDuration)
        {
            float t = (Time.time - startTime) / fadeDuration;
            foreach (Light light in lights)
            {
                light.intensity = Mathf.Lerp(currentIntensities[light], initialIntensities[light], t);
            }

            if (specialLight != null)
            {
                specialLight.intensity = Mathf.Lerp(specialLightCurrentIntensity, 0, t);
            }

            yield return null;
        }

        // Asegurarse de que la intensidad final sea la deseada
        foreach (Light light in lights)
        {
            light.intensity = initialIntensities[light];
        }

        if (specialLight != null)
        {
            specialLight.intensity = 0; // Apagar el foco especial
        }
    }

    // Corrutina para cambiar la intensidad de las luces gradualmente
    private IEnumerator FadeLights(float targetIntensity, bool activateSpecialLight)
    {
        float startTime = Time.time;

        // Intensidad inicial de las luces
        Dictionary<Light, float> currentIntensities = new Dictionary<Light, float>();
        foreach (Light light in lights)
        {
            currentIntensities[light] = light.intensity;
        }

        float specialLightCurrentIntensity = specialLight != null ? specialLight.intensity : 0;

        while (Time.time - startTime < fadeDuration)
        {
            float t = (Time.time - startTime) / fadeDuration;
            foreach (Light light in lights)
            {
                light.intensity = Mathf.Lerp(currentIntensities[light], targetIntensity, t);
            }

            if (specialLight != null)
            {
                float specialTargetIntensity = activateSpecialLight ? specialLightInitialIntensity : 0;
                specialLight.intensity = Mathf.Lerp(specialLightCurrentIntensity, specialTargetIntensity, t);
            }

            yield return null;
        }

        // Asegurarse de que la intensidad final sea la deseada
        foreach (Light light in lights)
        {
            light.intensity = targetIntensity;
        }

        if (specialLight != null)
        {
            specialLight.intensity = activateSpecialLight ? specialLightInitialIntensity : 0;
        }

        // Restaurar las luces al estado inicial si el video terminó
        if (!video.isPlaying)
        {
            foreach (Light light in lights)
            {
                light.intensity = initialIntensities[light];
            }

            if (specialLight != null)
            {
                specialLight.intensity = 0; // Apagar el foco especial
            }
        }
    }
}