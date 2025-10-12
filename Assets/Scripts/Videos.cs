using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Video;

public class Videos : MonoBehaviour
{
    public VideoPlayer video;

    // Se ejecuta al iniciar el script
    void Start()
    {
        // Obtiene el componente VideoPlayer de este objeto
        video = GetComponent<VideoPlayer>();
        // Detiene el video al iniciar
        video.Stop();
    }

    // Se ejecuta cuando otro objeto con un Collider entra en el trigger
    private void OnTriggerEnter(Collider other)
    {
        // Reproduce el video solo si no se está reproduciendo
        if (!video.isPlaying)
        {
            video.Play();
        }
    }

    // Se ejecuta cuando otro objeto con un Collider sale del trigger
    private void OnTriggerExit(Collider other)
    {
        // El video no se detiene al salir del trigger
    }
}