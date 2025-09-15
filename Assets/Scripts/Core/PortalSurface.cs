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
    float _maxDistSqr;
    
    void Awake()
    {
        _maxDistSqr = maxRenderingDistance * maxRenderingDistance;
        meshRenderer = myMeshRenderer != null ? myMeshRenderer : GetComponent<Renderer>();
        if (material != null && meshRenderer != null)
        {
            materialInstance = new Material(material);
            meshRenderer.material = materialInstance;
        }
    }

    void OnValidate()
    {
        if (maxRenderingDistance < 0) maxRenderingDistance = 0;
        _maxDistSqr = maxRenderingDistance * maxRenderingDistance;
    }
    
    public void UpdateMaterial(RenderTexture portalTexture, Vector3 cameraPosition)
    {
        if (materialInstance == null || meshRenderer == null) return;

        materialInstance.SetTexture("_MainTex", portalTexture);

        var toCam = cameraPosition - transform.position;
        float distSqr = toCam.sqrMagnitude;
        float normalizedDistance = _maxDistSqr > 0.0001f ? Mathf.Clamp01(distSqr / _maxDistSqr) : 0f;
        
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
        if (distSqr > _maxDistSqr)
            meshRenderer.enabled = false;
        else if (!meshRenderer.enabled)
            meshRenderer.enabled = true;
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