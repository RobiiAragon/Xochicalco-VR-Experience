using UnityEngine;

namespace Xochicalco.PortalSystem
{
    public class XRPortalSetup : MonoBehaviour
    {
        [Header("XR Setup")]
        [SerializeField] private bool setupOnStart = true;
        [SerializeField] private string playerTag = "Player";

        private void Start()
        {
            if (setupOnStart)
            {
                SetupXROriginForPortals();
            }
        }

        [ContextMenu("Setup XR Origin for Portals")]
        public void SetupXROriginForPortals()
        {
            // Buscar XR Origin por nombre (más compatible)
            GameObject xrOriginObj = GameObject.Find("XR Origin");
            if (xrOriginObj == null)
            {
                // Buscar objetos que contengan "XR Origin" en el nombre
                GameObject[] allObjects = FindObjectsByType<GameObject>(FindObjectsSortMode.None);
                foreach (GameObject obj in allObjects)
                {
                    if (obj.name.Contains("XR Origin") || obj.name.Contains("XROrigin"))
                    {
                        xrOriginObj = obj;
                        break;
                    }
                }
            }

            if (xrOriginObj == null)
            {
                Debug.LogWarning("❌ No XR Origin found in scene!");
                return;
            }

            Debug.Log("🔍 Setting up XR Origin for Portal System...");

            // 1. Configurar tag del XR Origin
            if (!xrOriginObj.CompareTag(playerTag))
            {
                xrOriginObj.tag = playerTag;
                Debug.Log($"✅ XR Origin tagged as '{playerTag}'");
            }

            // 2. Asegurar que tiene Rigidbody para detectar colliders
            Rigidbody rb = xrOriginObj.GetComponent<Rigidbody>();
            if (rb == null)
            {
                rb = xrOriginObj.AddComponent<Rigidbody>();
                rb.isKinematic = true; // Kinematic para que no interfiera con XR tracking
                Debug.Log("✅ Added kinematic Rigidbody to XR Origin");
            }

            // 3. Agregar collider si no tiene
            Collider col = xrOriginObj.GetComponent<Collider>();
            if (col == null)
            {
                CapsuleCollider capsule = xrOriginObj.AddComponent<CapsuleCollider>();
                capsule.isTrigger = true; // Trigger para no bloquear movimiento
                capsule.height = 1.8f;
                capsule.radius = 0.3f;
                capsule.center = new Vector3(0, 0.9f, 0);
                Debug.Log("✅ Added trigger CapsuleCollider to XR Origin");
            }

            // 4. Buscar cámara en XR Origin
            Camera xrCamera = xrOriginObj.GetComponentInChildren<Camera>();
            if (xrCamera != null)
            {
                ConfigurePortalCameras(xrCamera);
            }
            else
            {
                Debug.LogWarning("⚠️ No camera found in XR Origin children");
            }

            // 5. Mostrar información del setup
            Debug.Log("🎯 XR Origin Portal Setup Complete!");
            Debug.Log($"   - XR Origin: {xrOriginObj.name}");
            Debug.Log($"   - Camera: {(xrCamera ? xrCamera.name : "Not found")}");
            Debug.Log($"   - Tag: {xrOriginObj.tag}");
            Debug.Log($"   - Rigidbody: {(rb.isKinematic ? "Kinematic" : "Dynamic")}");
            Debug.Log($"   - Collider: {(col ? col.GetType().Name : "None")}");
        }

        private void ConfigurePortalCameras(Camera xrCamera)
        {
            if (xrCamera == null)
            {
                Debug.LogWarning("⚠️ XR Camera not found!");
                return;
            }

            // Configurar portales URP
            PortalControllerURP[] urpPortals = FindObjectsByType<PortalControllerURP>(FindObjectsSortMode.None);
            foreach (var portal in urpPortals)
            {
                portal.SetPlayerCamera(xrCamera.transform);
            }

            // Configurar portales clásicos si existen (mantenemos por compatibilidad pero usamos URP)
            PortalControllerURP[] classicPortals = FindObjectsByType<PortalControllerURP>(FindObjectsSortMode.None);
            foreach (var portal in classicPortals)
            {
                portal.SetPlayerCamera(xrCamera.transform);
            }

            Debug.Log($"✅ Configured {urpPortals.Length + classicPortals.Length} portals with XR camera");
        }

        [ContextMenu("Debug XR Setup")]
        public void DebugXRSetup()
        {
            GameObject xrOriginObj = GameObject.Find("XR Origin");
            if (xrOriginObj == null)
            {
                Debug.Log("❌ No XR Origin found");
                return;
            }

            Debug.Log("=== XR ORIGIN DEBUG INFO ===");
            Debug.Log($"Name: {xrOriginObj.name}");
            Debug.Log($"Tag: {xrOriginObj.tag}");
            Debug.Log($"Position: {xrOriginObj.transform.position}");
            
            Camera xrCamera = xrOriginObj.GetComponentInChildren<Camera>();
            Debug.Log($"Camera: {(xrCamera ? xrCamera.name : "NULL")}");
            Debug.Log($"Camera Position: {(xrCamera ? xrCamera.transform.position.ToString() : "NULL")}");
            
            Rigidbody rb = xrOriginObj.GetComponent<Rigidbody>();
            Debug.Log($"Rigidbody: {(rb ? $"Kinematic={rb.isKinematic}" : "NULL")}");
            
            Collider col = xrOriginObj.GetComponent<Collider>();
            Debug.Log($"Collider: {(col ? $"{col.GetType().Name}, Trigger={col.isTrigger}" : "NULL")}");

            // Debug portales
            PortalControllerURP[] portals = FindObjectsByType<PortalControllerURP>(FindObjectsSortMode.None);
            Debug.Log($"Portals found: {portals.Length}");
            foreach (var portal in portals)
            {
                Debug.Log($"  - {portal.name} at {portal.transform.position}");
            }
        }

        [ContextMenu("Test Portal Detection")]
        public void TestPortalDetection()
        {
            GameObject xrOriginObj = GameObject.Find("XR Origin");
            if (xrOriginObj == null)
            {
                Debug.Log("❌ No XR Origin found for testing");
                return;
            }

            PortalControllerURP[] portals = FindObjectsByType<PortalControllerURP>(FindObjectsSortMode.None);
            if (portals.Length == 0)
            {
                Debug.Log("❌ No portals found for testing");
                return;
            }

            // Mover XR Origin frente al primer portal
            Vector3 portalPos = portals[0].transform.position;
            Vector3 portalForward = portals[0].transform.forward;
            xrOriginObj.transform.position = portalPos - portalForward * 2f + Vector3.up * 0.1f;

            Debug.Log($"🚀 Moved XR Origin to test position in front of {portals[0].name}");
            Debug.Log($"   XR Position: {xrOriginObj.transform.position}");
            Debug.Log($"   Portal Position: {portalPos}");
            Debug.Log("   Walk forward to test portal teleportation!");
        }
    }
}