using UnityEngine;

public class PortalCameraController : MonoBehaviour
{
    private PortalRenderer[] portalRenderers;
    
    void Start()
    {
        // Find all portal renderers in the scene
        portalRenderers = FindObjectsByType<PortalRenderer>(FindObjectsSortMode.None);
        Debug.Log($"PortalCameraController found {portalRenderers.Length} portal renderers");
    }
    
    void OnPreCull()
    {
        if (portalRenderers == null || portalRenderers.Length == 0) return;
        for (int i = 0; i < portalRenderers.Length; i++)
        {
            var r = portalRenderers[i];
            if (r != null && r.enabled)
                r.RenderPortals();
        }
    }
}