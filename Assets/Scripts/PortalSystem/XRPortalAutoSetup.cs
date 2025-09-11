using UnityEngine;
using UnityEditor;

namespace Xochicalco.PortalSystem
{
    public class XRPortalAutoSetup : MonoBehaviour
    {
        [MenuItem("Tools/Portal System/Setup XR Origin for Portals")]
        public static void SetupXROriginForPortals()
        {
            // Find XR Origin
            var xrOrigin = FindFirstObjectByType<Unity.XR.CoreUtils.XROrigin>();
            if (xrOrigin == null)
            {
                Debug.LogError("❌ No XR Origin found in scene! Make sure you have an XR Origin GameObject.");
                return;
            }

            Debug.Log("🔧 Setting up XR Origin for Portal System...");

            // 1. Add PortalTraveller component
            PortalTraveller traveller = xrOrigin.GetComponent<PortalTraveller>();
            if (traveller == null)
            {
                traveller = xrOrigin.gameObject.AddComponent<PortalTraveller>();
                Debug.Log("✅ Added PortalTraveller to XR Origin");
            }

            // 2. Add trigger collider for portal detection
            Collider col = xrOrigin.GetComponent<Collider>();
            if (col == null)
            {
                CapsuleCollider capsule = xrOrigin.gameObject.AddComponent<CapsuleCollider>();
                capsule.isTrigger = true;
                capsule.height = 1.8f;
                capsule.radius = 0.3f;
                capsule.center = new Vector3(0, 0.9f, 0);
                Debug.Log("✅ Added trigger collider to XR Origin");
            }
            else if (!col.isTrigger)
            {
                col.isTrigger = true;
                Debug.Log("✅ Set existing collider as trigger");
            }

            // 3. Add kinematic rigidbody if needed
            Rigidbody rb = xrOrigin.GetComponent<Rigidbody>();
            if (rb == null)
            {
                rb = xrOrigin.gameObject.AddComponent<Rigidbody>();
                rb.isKinematic = true;
                Debug.Log("✅ Added kinematic Rigidbody to XR Origin");
            }
            else if (!rb.isKinematic)
            {
                rb.isKinematic = true;
                Debug.Log("✅ Set Rigidbody as kinematic");
            }

            // 4. Tag as Player if not already tagged
            if (!xrOrigin.CompareTag("Player"))
            {
                xrOrigin.tag = "Player";
                Debug.Log("✅ Tagged XR Origin as Player");
            }

            // 5. Find and configure portal renderer on camera
            Camera xrCamera = xrOrigin.GetComponentInChildren<Camera>();
            if (xrCamera != null)
            {
                PortalRenderer portalRenderer = xrCamera.GetComponent<PortalRenderer>();
                if (portalRenderer == null)
                {
                    xrCamera.gameObject.AddComponent<PortalRenderer>();
                    Debug.Log("✅ Added PortalRenderer to XR Camera");
                }
            }

            Debug.Log("🎯 XR Origin setup complete!");
            Debug.Log($"   - XR Origin: {xrOrigin.name}");
            Debug.Log($"   - Camera: {(xrCamera ? xrCamera.name : "Not found")}");
            Debug.Log($"   - Position: {xrOrigin.transform.position}");
            Debug.Log("🚶‍♂️ You can now walk through portals!");

            // Select the XR Origin in hierarchy
            Selection.activeGameObject = xrOrigin.gameObject;
        }

        [MenuItem("Tools/Portal System/Test XR Portal Setup")]
        public static void TestXRPortalSetup()
        {
            var xrOrigin = FindFirstObjectByType<Unity.XR.CoreUtils.XROrigin>();
            if (xrOrigin == null)
            {
                Debug.LogError("❌ No XR Origin found!");
                return;
            }

            Debug.Log("=== XR ORIGIN PORTAL SETUP TEST ===");
            Debug.Log($"Name: {xrOrigin.name}");
            Debug.Log($"Tag: {xrOrigin.tag}");
            Debug.Log($"Position: {xrOrigin.transform.position}");

            // Check components
            var traveller = xrOrigin.GetComponent<PortalTraveller>();
            Debug.Log($"PortalTraveller: {(traveller ? "✅ Found" : "❌ Missing")}");

            var collider = xrOrigin.GetComponent<Collider>();
            Debug.Log($"Collider: {(collider ? $"✅ {collider.GetType().Name} (Trigger: {collider.isTrigger})" : "❌ Missing")}");

            var rigidbody = xrOrigin.GetComponent<Rigidbody>();
            Debug.Log($"Rigidbody: {(rigidbody ? $"✅ Found (Kinematic: {rigidbody.isKinematic})" : "❌ Missing")}");

            var camera = xrOrigin.GetComponentInChildren<Camera>();
            Debug.Log($"Camera: {(camera ? camera.name : "❌ Not found")}");

            if (camera != null)
            {
                var portalRenderer = camera.GetComponent<PortalRenderer>();
                Debug.Log($"PortalRenderer: {(portalRenderer ? "✅ Found" : "❌ Missing")}");
            }

            // Check portals
            Portal[] portals = FindObjectsByType<Portal>(FindObjectsSortMode.None);
            Debug.Log($"Portals in scene: {portals.Length}");
            foreach (var portal in portals)
            {
                Debug.Log($"  - {portal.name} at {portal.transform.position}");
            }

            if (portals.Length == 0)
            {
                Debug.LogWarning("⚠️ No portals found! Create portals first using 'Tools → Portal System → Create Portal Pair'");
            }
        }

        [MenuItem("Tools/Portal System/Fix Eye Tracking Warning")]
        public static void DisableEyeTrackingWarning()
        {
            // Find GazeInputManager and disable it
            var gazeInputManager = FindFirstObjectByType<UnityEngine.XR.Interaction.Toolkit.Samples.StarterAssets.GazeInputManager>();
            if (gazeInputManager != null)
            {
                gazeInputManager.gameObject.SetActive(false);
                Debug.Log("✅ Disabled GazeInputManager to remove eye tracking warning");
            }
            else
            {
                Debug.Log("ℹ️ No GazeInputManager found - eye tracking warning may be from another component");
            }
        }
    }
}