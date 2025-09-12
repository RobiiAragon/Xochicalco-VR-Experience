using UnityEngine;
using UnityEditor;

public class PortalPrefabConverter : MonoBehaviour
{
    [MenuItem("Tools/Portal System/Create VR Portal Pair")]
    public static void CreateVRPortalPair()
    {
        Debug.Log("🚪 Creando par de portales para VR...");

        // Crear primer portal
        GameObject portal1 = CreatePortalGameObject("Portal A", new Vector3(-5, 0, 0), Quaternion.Euler(0, 90, 0));
        // Crear segundo portal
        GameObject portal2 = CreatePortalGameObject("Portal B", new Vector3(5, 0, 0), Quaternion.Euler(0, -90, 0));

        // Vincular portales
        var portalA = portal1.GetComponent<Portal>();
        var portalB = portal2.GetComponent<Portal>();
        
        portalA.linkedPortal = portalB;
        portalB.linkedPortal = portalA;

        Debug.Log("✅ Par de portales VR creado y vinculado!");
        
        // Seleccionar el primer portal
        Selection.activeGameObject = portal1;
    }

    private static GameObject CreatePortalGameObject(string name, Vector3 position, Quaternion rotation)
    {
        // Crear objeto principal del portal
        GameObject portalObj = new GameObject(name);
        portalObj.transform.position = position;
        portalObj.transform.rotation = rotation;

        // Agregar componentes básicos
        var portal = portalObj.AddComponent<Portal>();
        var boxCollider = portalObj.AddComponent<BoxCollider>();
        boxCollider.isTrigger = true;
        boxCollider.size = new Vector3(4, 3, 1);
        boxCollider.center = new Vector3(0, 1.5f, 0);

        var rigidbody = portalObj.AddComponent<Rigidbody>();
        rigidbody.isKinematic = true;

        // Crear marco del portal (visual simple)
        GameObject frame = GameObject.CreatePrimitive(PrimitiveType.Cube);
        frame.name = "Portal Frame";
        frame.transform.SetParent(portalObj.transform);
        frame.transform.localPosition = Vector3.zero;
        frame.transform.localScale = new Vector3(4.2f, 3.2f, 0.2f);

        // Crear pantalla del portal
        GameObject screen = GameObject.CreatePrimitive(PrimitiveType.Quad);
        screen.name = "Portal Screen";
        screen.transform.SetParent(portalObj.transform);
        screen.transform.localPosition = new Vector3(0, 1.5f, 0);
        screen.transform.localScale = new Vector3(3.8f, 2.8f, 1);

        // Configurar material de la pantalla
        var screenRenderer = screen.GetComponent<MeshRenderer>();
        portal.screen = screenRenderer;

        // Crear material básico para el portal
        Material portalMaterial = new Material(Shader.Find("Unlit/Texture"));
        portalMaterial.name = $"{name} Material";
        screenRenderer.material = portalMaterial;

        // Crear cámara del portal
        GameObject portalCamObj = new GameObject("Portal Camera");
        portalCamObj.transform.SetParent(portalObj.transform);
        portalCamObj.transform.localPosition = Vector3.zero;
        portalCamObj.transform.localRotation = Quaternion.identity;

        var portalCam = portalCamObj.AddComponent<Camera>();
        portalCam.enabled = false;
        portalCam.depth = -1;

        Debug.Log($"📦 Portal creado: {name}");
        return portalObj;
    }

    [MenuItem("Tools/Portal System/Debug Portal System")]
    public static void DebugPortalSystem()
    {
        Debug.Log("=== DEBUG DEL SISTEMA DE PORTALES ===");

        // Verificar portales
        Portal[] portals = FindObjectsByType<Portal>(FindObjectsSortMode.None);
        Debug.Log($"Portales encontrados: {portals.Length}");
        
        foreach (var portal in portals)
        {
            Debug.Log($"  - {portal.name} (Enlazado: {(portal.linkedPortal ? portal.linkedPortal.name : "Ninguno")})");
            
            // Verificar si tiene cámara
            Camera portalCam = portal.GetComponentInChildren<Camera>();
            Debug.Log($"    Cámara: {(portalCam ? "✅" : "❌")}");
            
            // Verificar si tiene pantalla
            Debug.Log($"    Pantalla: {(portal.screen ? "✅" : "❌")}");
        }

        // Verificar XR Origin
        var xrOrigin = FindFirstObjectByType<Unity.XR.CoreUtils.XROrigin>();
        if (xrOrigin != null)
        {
            Debug.Log($"XR Origin: {xrOrigin.name}");
            
            var vrTraveller = xrOrigin.GetComponent<VRPortalTraveller>();
            var basicTraveller = xrOrigin.GetComponent<PortalTraveller>();
            var xrMainCamera = xrOrigin.GetComponentInChildren<XRMainCamera>();
            var collider = xrOrigin.GetComponent<Collider>();
            var rigidbody = xrOrigin.GetComponent<Rigidbody>();
            
            Debug.Log($"  - VRPortalTraveller: {(vrTraveller ? "✅" : "❌")}");
            Debug.Log($"  - PortalTraveller básico: {(basicTraveller && !vrTraveller ? "⚠️" : "✅")}");
            Debug.Log($"  - XRMainCamera: {(xrMainCamera ? "✅" : "❌")}");
            Debug.Log($"  - Collider: {(collider ? "✅" : "❌")}");
            Debug.Log($"  - Rigidbody: {(rigidbody ? "✅" : "❌")}");
        }
        else
        {
            Debug.Log("XR Origin: ❌ No encontrado");
        }
    }

    [MenuItem("Tools/Portal System/Fix Portal Materials")]
    public static void FixPortalMaterials()
    {
        Debug.Log("🎨 Arreglando materiales de portales...");

        Portal[] portals = FindObjectsByType<Portal>(FindObjectsSortMode.None);
        
        foreach (var portal in portals)
        {
            if (portal.screen != null)
            {
                Material mat = portal.screen.material;
                if (mat != null)
                {
                    // Asegurar que el material tenga las propiedades correctas para portales
                    if (mat.HasProperty("_MainTex"))
                    {
                        Debug.Log($"✅ Material de {portal.name} tiene _MainTex");
                    }
                    else
                    {
                        // Crear un nuevo material compatible
                        Material newMat = new Material(Shader.Find("Unlit/Texture"));
                        newMat.name = $"{portal.name} Portal Material";
                        portal.screen.material = newMat;
                        Debug.Log($"🔧 Creado nuevo material para {portal.name}");
                    }
                }
                else
                {
                    // Crear material desde cero
                    Material newMat = new Material(Shader.Find("Unlit/Texture"));
                    newMat.name = $"{portal.name} Portal Material";
                    portal.screen.material = newMat;
                    Debug.Log($"✅ Creado material para {portal.name}");
                }
            }
        }

        Debug.Log($"🎯 ¡Materiales de portales arreglados para {portals.Length} portales!");
    }
}