using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Xochicalco.PortalSystem
{
    [RequireComponent(typeof(Camera))]
    public class PortalRenderer : MonoBehaviour
    {
        private Camera playerCam;

        void Awake()
        {
            playerCam = GetComponent<Camera>();
            
            // Subscribe to the render pipeline
            RenderPipelineManager.beginCameraRendering += OnBeginCameraRendering;
            Debug.Log($"🎥 PortalRenderer initialized on {name}");
        }

        void OnDestroy()
        {
            RenderPipelineManager.beginCameraRendering -= OnBeginCameraRendering;
        }

        void OnBeginCameraRendering(ScriptableRenderContext context, Camera camera)
        {
            // Only render for the main player camera
            if (camera != playerCam) return;

            Portal[] portals = FindObjectsByType<Portal>(FindObjectsSortMode.None);
            
            if (portals.Length == 0) return;

            // Pre-render setup
            foreach (var portal in portals)
            {
                portal.PrePortalRender();
            }
            
            // Render each portal
            foreach (var portal in portals)
            {
                portal.Render();
            }
            
            // Post-render cleanup
            foreach (var portal in portals)
            {
                portal.PostPortalRender();
            }
        }
    }
}