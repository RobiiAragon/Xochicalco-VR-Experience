using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Video;

public class Videos : MonoBehaviour
{
    public VideoPlayer video;
    public List<Light> lights; // Lista de luces en la escena
    public float fadeDuration = 1.0f; // Duración del cambio gradual de intensidad
    public Light specialLight; // Foco que inicia apagado
    private float[] initialIntensities; // Intensidades iniciales de las luces
    private float specialLightInitialIntensity; // Intensidad inicial del foco especial
    private bool hasPlayed = false; // Bandera para evitar que el video se reproduzca más de una vez

    // Se ejecuta al iniciar el script
    void Start()
    {
        // Obtiene el componente VideoPlayer de este objeto
        video = GetComponent<VideoPlayer>();
        // Detiene el video al iniciar
        video.Stop();

        // Guardar las intensidades iniciales de las luces
        initialIntensities = new float[lights.Count];
        for (int i = 0; i < lights.Count; i++)
        {
            initialIntensities[i] = lights[i].intensity;
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
        // Reproduce el video solo si no se ha reproducido antes
        if (!hasPlayed && !video.isPlaying)
        {
            hasPlayed = true;
            StartCoroutine(FadeLights(0, true, () => video.Play())); // Apagar luces y luego reproducir el video
        }
    }

    // Se ejecuta cuando el video termina
    private void OnVideoEnd(VideoPlayer vp)
    {
        StartCoroutine(FadeLightsBackToOriginal()); // Restaurar las luces gradualmente
    }

    // Corrutina para cambiar la intensidad de las luces gradualmente
    private IEnumerator FadeLights(float targetIntensity, bool activateSpecialLight, System.Action onComplete = null)
    {
        float startTime = Time.time;

        while (Time.time - startTime < fadeDuration)
        {
            float t = (Time.time - startTime) / fadeDuration;
            for (int i = 0; i < lights.Count; i++)
            {
                lights[i].intensity = Mathf.Lerp(lights[i].intensity, targetIntensity, t);
            }

            if (specialLight != null)
            {
                float specialTargetIntensity = activateSpecialLight ? specialLightInitialIntensity : 0;
                specialLight.intensity = Mathf.Lerp(specialLight.intensity, specialTargetIntensity, t);
            }

            yield return null;
        }

        // Asegurar valores finales
        for (int i = 0; i < lights.Count; i++)
        {
            lights[i].intensity = targetIntensity;
        }

        if (specialLight != null)
        {
            specialLight.intensity = activateSpecialLight ? specialLightInitialIntensity : 0;
        }

        onComplete?.Invoke(); // Llamar al callback si se proporciona
    }

    // Corrutina para restaurar las luces a su intensidad inicial
    private IEnumerator FadeLightsBackToOriginal()
    {
        float startTime = Time.time;

        while (Time.time - startTime < fadeDuration)
        {
            float t = (Time.time - startTime) / fadeDuration;
            for (int i = 0; i < lights.Count; i++)
            {
                lights[i].intensity = Mathf.Lerp(lights[i].intensity, initialIntensities[i], t);
            }

            if (specialLight != null)
            {
                specialLight.intensity = Mathf.Lerp(specialLight.intensity, 0, t);
            }

            yield return null;
        }

        // Asegurar valores finales
        for (int i = 0; i < lights.Count; i++)
        {
            lights[i].intensity = initialIntensities[i];
        }

        if (specialLight != null)
        {
            specialLight.intensity = 0;
        }
    }
}