using UnityEngine;

public class PortalSurface : MonoBehaviour
{
    [Header("Material")]
    public Material material;
    
    [Header("Rendering")]
    public float maxRenderingDistance = 50f;
    
    [Header("Color Blending")]
    public bool useColorBlending = false;
    public Color blendColor = Color.black;
    public AnimationCurve colorBlendingCurve = AnimationCurve.Linear(0, 0, 1, 1);
    
    [Header("Albedo Alpha Blending")]
    public bool useAlbedoAlphaBlending = false;
    public AnimationCurve albedoAlphaCurve = AnimationCurve.Linear(0, 1.5f, 1, 0);
    
    [Header("Refraction")]
    public bool useRefractionFading = false;
    public AnimationCurve refractionCurve = AnimationCurve.Linear(0, 0.1f, 1, 0);
    
    [Header("Advanced")]
    public Material customSkybox;
    public float clippingPlaneOffset = -0.001f;
    public bool requireObliqueProjectionMatrix = true;
    public float nearDistanceToStartDisablingObliquePM = 0.1f;
    public MeshRenderer myMeshRenderer;
    
    private Renderer meshRenderer;
    private Material materialInstance;
    
    void Awake()
    {
        meshRenderer = myMeshRenderer != null ? myMeshRenderer : GetComponent<Renderer>();
        
        if (material != null && meshRenderer != null)
        {
            materialInstance = new Material(material);
            meshRenderer.material = materialInstance;
        }
    }
    
    public void UpdateMaterial(RenderTexture portalTexture, Vector3 cameraPosition)
    {
        if (materialInstance == null) 
        {
            Debug.LogWarning($"PortalSurface on {gameObject.name}: materialInstance is null");
            return;
        }
        
        Debug.Log($"UpdateMaterial called on {gameObject.name} with texture: {portalTexture != null}");
        
        // Set portal texture
        materialInstance.SetTexture("_MainTex", portalTexture);
        
        // Calculate distance to camera
        float distance = Vector3.Distance(transform.position, cameraPosition);
        float normalizedDistance = Mathf.Clamp01(distance / maxRenderingDistance);
        
        // Update material properties based on distance
        if (useColorBlending)
        {
            float colorBlend = colorBlendingCurve.Evaluate(normalizedDistance);
            materialInstance.SetColor("_BlendColor", blendColor);
            materialInstance.SetFloat("_ColorBlend", colorBlend);
        }
        
        if (useAlbedoAlphaBlending)
        {
            float alphaBlend = albedoAlphaCurve.Evaluate(normalizedDistance);
            materialInstance.SetFloat("_AlphaBlend", alphaBlend);
        }
        
        if (useRefractionFading)
        {
            float refractionAmount = refractionCurve.Evaluate(normalizedDistance);
            materialInstance.SetFloat("_RefractionAmount", refractionAmount);
        }
        
        // Disable rendering if too far
        if (distance > maxRenderingDistance)
        {
            meshRenderer.enabled = false;
        }
        else
        {
            meshRenderer.enabled = true;
        }
    }
    
    [ContextMenu("Reset Color Blending Curve")]
    void ResetColorBlendingCurve()
    {
        colorBlendingCurve = AnimationCurve.Linear(0, 0, 0.8f, 0);
        colorBlendingCurve.AddKey(1, 1);
    }
    
    [ContextMenu("Reset Albedo Alpha Curve")]
    void ResetAlbedoAlphaCurve()
    {
        albedoAlphaCurve = AnimationCurve.Linear(0, 1.5f, 0.2f, 1.5f);
        albedoAlphaCurve.AddKey(0.8f, 0.5f);
        albedoAlphaCurve.AddKey(1, 0);
    }
    
    [ContextMenu("Reset Refraction Curve")]
    void ResetRefractionCurve()
    {
        refractionCurve = AnimationCurve.Linear(0, 0.1f, 0.8f, 0.1f);
        refractionCurve.AddKey(1, 0);
    }
}