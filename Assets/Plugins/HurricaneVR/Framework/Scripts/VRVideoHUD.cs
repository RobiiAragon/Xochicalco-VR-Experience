namespace HurricaneVR.Framework
{
    using UnityEngine;
    using UnityEngine.UI;
    using UnityEngine.Video;

    public class VRVideoHUD : MonoBehaviour
    {
        public static VRVideoHUD Instance { get; private set; }

        public VideoClip videoClip; // Asignar el video en el inspector
        public Vector3 localPosition = new Vector3(0.25f, -0.18f, 0.6f); // Posición relativa a la cámara
        public Vector2 size = new Vector2(400, 225); // Tamaño del HUD
        public float worldScale = 0.001f; // Escala para el Canvas en espacio mundial

        private Canvas canvas;
        private RawImage rawImage;
        private VideoPlayer videoPlayer;
        private RenderTexture renderTexture;

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;

            // Asegurarse de que es un objeto raíz antes de llamar a DontDestroyOnLoad
            transform.SetParent(null);
            DontDestroyOnLoad(gameObject);

            CreateHUD();
            Hide(); // Inicia oculto
        }

        private void CreateHUD()
        {
            Camera camera = Camera.main;
            if (camera == null) return;

            // Crear Canvas
            GameObject canvasObject = new GameObject("VRVideoHUD_Canvas");
            canvasObject.transform.SetParent(camera.transform, false);
            canvasObject.transform.localPosition = localPosition;
            canvasObject.transform.localRotation = Quaternion.identity;
            canvasObject.transform.localScale = Vector3.one * worldScale;

            canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;

            RectTransform canvasRect = canvas.GetComponent<RectTransform>();
            canvasRect.sizeDelta = size;

            // Crear RawImage
            GameObject rawImageObject = new GameObject("VideoRawImage");
            rawImageObject.transform.SetParent(canvasObject.transform, false);
            rawImage = rawImageObject.AddComponent<RawImage>();
            RectTransform rawImageRect = rawImage.GetComponent<RectTransform>();
            rawImageRect.anchorMin = Vector2.zero;
            rawImageRect.anchorMax = Vector2.one;
            rawImageRect.offsetMin = Vector2.zero;
            rawImageRect.offsetMax = Vector2.zero;

            // Configurar VideoPlayer
            videoPlayer = canvasObject.AddComponent<VideoPlayer>();
            videoPlayer.playOnAwake = false;
            videoPlayer.isLooping = true;
            videoPlayer.audioOutputMode = VideoAudioOutputMode.None;
            videoPlayer.clip = videoClip;

            renderTexture = new RenderTexture((int)size.x, (int)size.y, 0);
            videoPlayer.targetTexture = renderTexture;
            rawImage.texture = renderTexture;
        }

        public void Show()
        {
            if (canvas != null) canvas.gameObject.SetActive(true);
            if (videoPlayer != null && videoPlayer.clip != null) videoPlayer.Play();
        }

        public void Hide()
        {
            if (videoPlayer != null) videoPlayer.Stop();
            if (canvas != null) canvas.gameObject.SetActive(false);
        }

        void OnDestroy()
        {
            if (renderTexture != null) renderTexture.Release();
            if (Instance == this) Instance = null;
        }
    }
}