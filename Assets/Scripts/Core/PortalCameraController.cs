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
        // Render all portals before the main camera renders
        if (portalRenderers != null)
        {
            Debug.Log("OnPreCull: Rendering portals...");
            foreach (PortalRenderer renderer in portalRenderers)
            {
                if (renderer != null && renderer.enabled)
                {
                    renderer.RenderPortals();
                }
            }
        }
    }
}