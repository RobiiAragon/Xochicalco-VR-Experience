using UnityEngine;

public class PortalTraveller : MonoBehaviour 
{
    public GameObject graphicsObject;
    public GameObject graphicsClone { get; set; }
    public Vector3 previousOffsetFromPortal { get; set; }
    
    [HideInInspector]
    public Material[] originalMaterials { get; set; }
    [HideInInspector]
    public Material[] cloneMaterials { get; set; }

    MeshRenderer[] originalRenderers;
    MeshRenderer[] cloneRenderers;

    void Awake() {
        if (graphicsObject == null) {
            graphicsObject = gameObject;
        }
        
        originalRenderers = graphicsObject.GetComponentsInChildren<MeshRenderer>();
        originalMaterials = new Material[originalRenderers.Length];
        
        for (int i = 0; i < originalRenderers.Length; i++) {
            originalMaterials[i] = originalRenderers[i].material;
        }
    }

    public virtual void Teleport(Transform fromPortal, Transform toPortal, Vector3 pos, Quaternion rot) {
        transform.position = pos;
        transform.rotation = rot;
    }

    public virtual void EnterPortalThreshold() {
        if (graphicsClone == null) {
            graphicsClone = Instantiate(graphicsObject);
            cloneRenderers = graphicsClone.GetComponentsInChildren<MeshRenderer>();
            cloneMaterials = new Material[cloneRenderers.Length];
            
            for (int i = 0; i < cloneRenderers.Length; i++) {
                cloneMaterials[i] = cloneRenderers[i].material;
            }
        }
        graphicsClone.transform.parent = graphicsObject.transform.parent;
        graphicsClone.transform.localScale = graphicsObject.transform.localScale;
    }

    public virtual void ExitPortalThreshold() {
        if (graphicsClone != null) {
            DestroyImmediate(graphicsClone);
        }
    }

    public void SetSliceOffsetDst(float dst, bool clone) {
        Material[] materials = clone ? cloneMaterials : originalMaterials;
        for (int i = 0; i < materials.Length; i++) {
            materials[i].SetFloat("sliceOffsetDst", dst);
        }
    }
}