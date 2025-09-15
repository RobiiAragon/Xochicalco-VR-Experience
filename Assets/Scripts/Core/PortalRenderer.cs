using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

[System.Serializable]
public class PortalRenderer : MonoBehaviour
{
    [Header("Portal Settings")]
    public List<Portal> portals = new List<Portal>();
    public int recursions = 2;
    
    [Header("Quality Settings")]
    public int textureSize = 1024;
    public bool useScreenScaleFactor = false;
    [Range(0.1f, 1.0f)]
    public float screenScaleFactor = 0.5f;
    public RenderTextureFormat renderTextureFormat = RenderTextureFormat.Default;
    public int antiAliasing = 1;
    public bool disablePixelLights = true;
    public int framesNeededToUpdate = 0;
    
    [Header("Other")]
    public LayerMask renderTheseLayers = -1;
    public bool useOcclusionCulling = false;
    public bool disableRenderingWhileStillUpdatingMaterials = false;
    
    [Header("Events")]
    public UnityEngine.Events.UnityEvent beforeRenderPortal;
    public UnityEngine.Events.UnityEvent afterRenderPortal;
    
    private Camera portalCamera;
    private RenderTexture portalTexture;
    private Camera mainCamera;
    
    int _lastW, _lastH;
    bool _rtDirty;
    
    void Start()
    {
        mainCamera = Camera.main;
        if (mainCamera == null)
        {
            Camera[] cameras = FindObjectsByType<Camera>(FindObjectsSortMode.None);
            foreach (Camera cam in cameras)
            {
                if (cam.CompareTag("MainCamera"))
                {
                    mainCamera = cam;
                    break;
                }
            }
        }
        
        CreatePortalCamera();
        CreateRenderTexture();
    }
    
    void CreatePortalCamera()
    {
        GameObject cameraObj = new GameObject("PortalCamera");
        cameraObj.transform.SetParent(transform);
        portalCamera = cameraObj.AddComponent<Camera>();
        
        // Configure portal camera
        portalCamera.enabled = false;
        portalCamera.clearFlags = CameraClearFlags.Skybox;
        portalCamera.cullingMask = renderTheseLayers;
        portalCamera.useOcclusionCulling = useOcclusionCulling;
        
        // Copy settings from main camera
        if (mainCamera != null)
        {
            portalCamera.fieldOfView = mainCamera.fieldOfView;
            portalCamera.nearClipPlane = mainCamera.nearClipPlane;
            portalCamera.farClipPlane = mainCamera.farClipPlane;
        }
    }
    
    void CreateRenderTexture()
    {
        int width, height;

        if (useScreenScaleFactor)
        {
            width = Mathf.Max(32, Mathf.RoundToInt(Screen.width * screenScaleFactor));
            height = Mathf.Max(32, Mathf.RoundToInt(Screen.height * screenScaleFactor));
        }
        else
        {
            width = textureSize;
            height = textureSize;
        }

        if (portalTexture != null && width == _lastW && height == _lastH && portalTexture.format == renderTextureFormat)
            return;

        _lastW = width; _lastH = height;

        if (portalTexture != null)
            portalTexture.Release();

        portalTexture = new RenderTexture(width, height, 24, renderTextureFormat)
        {
            antiAliasing = Mathf.Max(1, antiAliasing),
            name = "PortalRT_" + gameObject.name
        };
        portalTexture.Create();
        if (portalCamera != null)
            portalCamera.targetTexture = portalTexture;
    }
    
    public void RenderPortals()
    {
        if (mainCamera == null || portalCamera == null || portals.Count == 0)
            return;

        if (_rtDirty)
        {
            _rtDirty = false;
            CreateRenderTexture();
        }

        // Preparar planos de frustum una vez
        var planes = GeometryUtility.CalculateFrustumPlanes(mainCamera);

        foreach (var portal in portals)
        {
            if (portal == null || portal.linkedPortal == null) continue;
            // Visibilidad rápida
            var scr = portal.screen;
            if (scr == null) continue;
            var rend = scr.GetComponent<Renderer>();
            if (rend == null || !GeometryUtility.TestPlanesAABB(planes, rend.bounds)) continue;
            RenderPortal(portal);
        }
    }
    
    void RenderPortal(Portal portal)
    {
        if (portal.linkedPortal == null) return;
        
        beforeRenderPortal?.Invoke();
        
        // Calculate portal camera position and rotation
        Matrix4x4 portalMatrix = portal.transform.localToWorldMatrix * 
                                portal.linkedPortal.transform.worldToLocalMatrix * 
                                mainCamera.transform.localToWorldMatrix;
        
        portalCamera.transform.SetPositionAndRotation(
            portalMatrix.GetColumn(3),
            portalMatrix.rotation
        );
        
        // Update portal material
        if (portal.portalSurface != null)
        {
            portal.portalSurface.UpdateMaterial(portalTexture, mainCamera.transform.position);
        }
        
        // Render
        if (!disableRenderingWhileStillUpdatingMaterials)
        {
            portalCamera.Render();
        }
        
        afterRenderPortal?.Invoke();
    }
    
    void OnValidate()
    {
        if (Application.isPlaying)
            _rtDirty = true;
    }

    void OnDestroy()
    {
        if (portalTexture != null)
        {
            portalTexture.Release();
#if UNITY_EDITOR
            DestroyImmediate(portalTexture);
#else
            Destroy(portalTexture);
#endif
        }
        if (portalCamera != null)
#if UNITY_EDITOR
            DestroyImmediate(portalCamera.gameObject);
#else
            Destroy(portalCamera.gameObject);
#endif
    }
}