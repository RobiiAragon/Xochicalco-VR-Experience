using UnityEngine;
using UnityEditor;

public class XRPortalSetup : MonoBehaviour 
{
    [MenuItem("Tools/XR Portal Setup/Configure XR Origin for Portals")]
    public static void ConfigureXROriginForPortals()
    {
        // Buscar XR Origin
        var xrOrigin = FindFirstObjectByType<Unity.XR.CoreUtils.XROrigin>();
        if (xrOrigin == null)
        {
            Debug.LogError("❌ No se encontró XR Origin en la escena!");
            return;
        }

        Debug.Log("🔧 Configurando XR Origin para portales...");

        // 1. Agregar VRPortalTraveller (mejorado para VR)
        var portalTraveller = xrOrigin.GetComponent<VRPortalTraveller>();
        if (portalTraveller == null)
        {
            // Remover el PortalTraveller básico si existe
            var basicTraveller = xrOrigin.GetComponent<PortalTraveller>();
            if (basicTraveller != null)
            {
                DestroyImmediate(basicTraveller);
                Debug.Log("🗑️ Removido PortalTraveller básico");
            }
            
            portalTraveller = xrOrigin.gameObject.AddComponent<VRPortalTraveller>();
            Debug.Log("✅ Agregado VRPortalTraveller (mejorado para VR) a XR Origin");
        }

        // 2. Agregar collider trigger
        Collider col = xrOrigin.GetComponent<Collider>();
        if (col == null)
        {
            CapsuleCollider capsule = xrOrigin.gameObject.AddComponent<CapsuleCollider>();
            capsule.isTrigger = true;
            capsule.height = 1.8f;
            capsule.radius = 0.3f;
            capsule.center = new Vector3(0, 0.9f, 0);
            Debug.Log("✅ Agregado trigger collider a XR Origin");
        }
        else if (!col.isTrigger)
        {
            col.isTrigger = true;
            Debug.Log("✅ Configurado collider existente como trigger");
        }

        // 3. Agregar rigidbody kinematic
        Rigidbody rb = xrOrigin.GetComponent<Rigidbody>();
        if (rb == null)
        {
            rb = xrOrigin.gameObject.AddComponent<Rigidbody>();
            rb.isKinematic = true;
            Debug.Log("✅ Agregado Rigidbody kinematic a XR Origin");
        }
        else if (!rb.isKinematic)
        {
            rb.isKinematic = true;
            Debug.Log("✅ Configurado Rigidbody como kinematic");
        }

        // 4. Etiquetar como Player
        if (!xrOrigin.CompareTag("Player"))
        {
            xrOrigin.tag = "Player";
            Debug.Log("✅ Etiquetado XR Origin como Player");
        }

        // 5. Configurar cámara para portales
        Camera xrCamera = xrOrigin.GetComponentInChildren<Camera>();
        if (xrCamera != null)
        {
            // Agregar XRMainCamera si no existe
            var xrMainCamera = xrCamera.GetComponent<XRMainCamera>();
            if (xrMainCamera == null)
            {
                xrCamera.gameObject.AddComponent<XRMainCamera>();
                Debug.Log("✅ Agregado XRMainCamera a la cámara XR");
            }
        }

        Debug.Log("🎯 ¡Configuración de XR Origin completada!");
        Debug.Log($"   - XR Origin: {xrOrigin.name}");
        Debug.Log($"   - Cámara: {(xrCamera ? xrCamera.name : "No encontrada")}");
        Debug.Log($"   - Posición: {xrOrigin.transform.position}");
        Debug.Log("🚶‍♂️ ¡Ahora puedes caminar a través de portales en VR!");

        // Seleccionar el XR Origin en la jerarquía
        Selection.activeGameObject = xrOrigin.gameObject;
    }

    [MenuItem("Tools/XR Portal Setup/Test Portal Configuration")]
    public static void TestPortalConfiguration()
    {
        var xrOrigin = FindFirstObjectByType<Unity.XR.CoreUtils.XROrigin>();
        if (xrOrigin == null)
        {
            Debug.LogError("❌ No se encontró XR Origin!");
            return;
        }

        Debug.Log("=== PRUEBA DE CONFIGURACIÓN DE PORTALES ===");
        Debug.Log($"Nombre: {xrOrigin.name}");
        Debug.Log($"Tag: {xrOrigin.tag}");
        Debug.Log($"Posición: {xrOrigin.transform.position}");

        // Verificar componentes
        var vrTraveller = xrOrigin.GetComponent<VRPortalTraveller>();
        var basicTraveller = xrOrigin.GetComponent<PortalTraveller>();
        Debug.Log($"VRPortalTraveller: {(vrTraveller ? "✅ Encontrado" : "❌ Faltante")}");
        Debug.Log($"PortalTraveller básico: {(basicTraveller && !vrTraveller ? "⚠️ Usar VR version" : "✅ OK")}");

        var collider = xrOrigin.GetComponent<Collider>();
        Debug.Log($"Collider: {(collider ? $"✅ {collider.GetType().Name} (Trigger: {collider.isTrigger})" : "❌ Faltante")}");

        var rigidbody = xrOrigin.GetComponent<Rigidbody>();
        Debug.Log($"Rigidbody: {(rigidbody ? $"✅ Encontrado (Kinematic: {rigidbody.isKinematic})" : "❌ Faltante")}");

        var camera = xrOrigin.GetComponentInChildren<Camera>();
        Debug.Log($"Cámara: {(camera ? camera.name : "❌ No encontrada")}");

        if (camera != null)
        {
            var xrMainCamera = camera.GetComponent<XRMainCamera>();
            Debug.Log($"XRMainCamera: {(xrMainCamera ? "✅ Encontrado" : "❌ Faltante")}");
        }

        // Verificar portales
        var portals = FindObjectsByType<Portal>(FindObjectsSortMode.None);
        Debug.Log($"Portales encontrados: {portals.Length}");

        if (portals.Length == 0)
        {
            Debug.LogWarning("⚠️ No se encontraron portales en la escena. Agrega algunos portales para probar.");
        }
        else
        {
            foreach (var portal in portals)
            {
                Debug.Log($"  - {portal.name} (Enlazado: {(portal.linkedPortal ? portal.linkedPortal.name : "Ninguno")})");
            }
        }
    }

    [MenuItem("Tools/XR Portal Setup/Setup VR Hands for Portals")]
    public static void SetupVRHandsForPortals()
    {
        Debug.Log("🖐️ Configurando manos VR para portales...");

        var xrOrigin = FindFirstObjectByType<Unity.XR.CoreUtils.XROrigin>();
        if (xrOrigin == null)
        {
            Debug.LogError("❌ No se encontró XR Origin!");
            return;
        }

        Transform[] allTransforms = xrOrigin.GetComponentsInChildren<Transform>();
        int handsSetup = 0;

        foreach (Transform child in allTransforms)
        {
            if (child.name.ToLower().Contains("hand") && 
                (child.name.ToLower().Contains("left") || child.name.ToLower().Contains("right")))
            {
                // Agregar VRPortalTraveller a la mano
                var vrTraveller = child.GetComponent<VRPortalTraveller>();
                if (vrTraveller == null)
                {
                    // Remover PortalTraveller básico si existe
                    var basicTraveller = child.GetComponent<PortalTraveller>();
                    if (basicTraveller != null)
                    {
                        DestroyImmediate(basicTraveller);
                    }
                    
                    vrTraveller = child.gameObject.AddComponent<VRPortalTraveller>();
                    Debug.Log($"✅ Agregado VRPortalTraveller a {child.name}");
                }

                // Agregar collider trigger pequeño para detección de mano
                Collider col = child.GetComponent<Collider>();
                if (col == null)
                {
                    SphereCollider sphere = child.gameObject.AddComponent<SphereCollider>();
                    sphere.isTrigger = true;
                    sphere.radius = 0.05f; // Radio pequeño para mano
                    Debug.Log($"✅ Agregado trigger collider a {child.name}");
                }

                // Configurar objeto gráfico (la visual de la mano)
                if (vrTraveller.graphicsObject == null)
                {
                    MeshRenderer handRenderer = child.GetComponentInChildren<MeshRenderer>();
                    if (handRenderer != null)
                    {
                        vrTraveller.graphicsObject = handRenderer.gameObject;
                        Debug.Log($"✅ Configurado objeto gráfico para {child.name}");
                    }
                }

                handsSetup++;
            }
        }

        Debug.Log($"🎯 ¡Configuración de manos VR completada! Configuradas {handsSetup} manos");
        
        if (handsSetup == 0)
        {
            Debug.LogWarning("⚠️ No se encontraron manos VR. Asegúrate de que tu XR Origin tenga objetos de mano con 'hand' y 'left'/'right' en sus nombres");
        }
    }

    [MenuItem("Tools/XR Portal Setup/Fix Portal Camera Issues")]
    public static void FixPortalCameraIssues()
    {
        Debug.Log("🔧 Arreglando problemas de cámara de portales...");

        var portals = FindObjectsByType<Portal>(FindObjectsSortMode.None);
        
        foreach (var portal in portals)
        {
            // Asegurar que cada portal tenga una cámara configurada correctamente
            Camera portalCam = portal.GetComponentInChildren<Camera>();
            if (portalCam == null)
            {
                GameObject camObj = new GameObject("Portal Camera");
                camObj.transform.SetParent(portal.transform);
                camObj.transform.localPosition = Vector3.zero;
                portalCam = camObj.AddComponent<Camera>();
                Debug.Log($"✅ Agregada cámara a portal: {portal.name}");
            }

            // Configurar la cámara del portal para VR
            portalCam.enabled = false;
            portalCam.depth = -1;
            portalCam.clearFlags = CameraClearFlags.Skybox;
            
            Debug.Log($"🔧 Configurada cámara para portal: {portal.name}");
        }

        Debug.Log($"🎯 ¡Configuración de cámaras de portal completada para {portals.Length} portales!");
    }
}