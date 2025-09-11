using UnityEngine;
using UnityEditor;

namespace Xochicalco.PortalSystem
{
    public class VRHandsPortalSetup : MonoBehaviour
    {
        [MenuItem("Tools/Portal System/Setup VR Hands for Portals")]
        public static void SetupVRHandsForPortals()
        {
            Debug.Log("🖐️ Setting up VR Hands for Portal System...");

            // Find XR Origin
            var xrOrigin = FindFirstObjectByType<Unity.XR.CoreUtils.XROrigin>();
            if (xrOrigin == null)
            {
                Debug.LogError("❌ No XR Origin found!");
                return;
            }

            // Find hand objects
            Transform[] allTransforms = xrOrigin.GetComponentsInChildren<Transform>();
            int handsSetup = 0;

            foreach (Transform child in allTransforms)
            {
                if (child.name.ToLower().Contains("hand") && 
                    (child.name.ToLower().Contains("left") || child.name.ToLower().Contains("right")))
                {
                    // Add PortalTraveller to hand
                    PortalTraveller traveller = child.GetComponent<PortalTraveller>();
                    if (traveller == null)
                    {
                        traveller = child.gameObject.AddComponent<PortalTraveller>();
                        Debug.Log($"✅ Added PortalTraveller to {child.name}");
                    }

                    // Add small trigger collider for hand detection
                    Collider col = child.GetComponent<Collider>();
                    if (col == null)
                    {
                        SphereCollider sphere = child.gameObject.AddComponent<SphereCollider>();
                        sphere.isTrigger = true;
                        sphere.radius = 0.05f; // Small radius for hand
                        Debug.Log($"✅ Added trigger collider to {child.name}");
                    }

                    // Set up graphics object (the hand visual)
                    if (traveller.graphicsObject == null)
                    {
                        MeshRenderer handRenderer = child.GetComponentInChildren<MeshRenderer>();
                        if (handRenderer != null)
                        {
                            traveller.graphicsObject = handRenderer.gameObject;
                            Debug.Log($"✅ Set graphics object for {child.name}");
                        }
                    }

                    handsSetup++;
                }
            }

            Debug.Log($"🎯 VR Hands setup complete! Configured {handsSetup} hands");
            
            if (handsSetup == 0)
            {
                Debug.LogWarning("⚠️ No VR hands found. Make sure your XR Origin has hand objects with 'hand' and 'left'/'right' in their names");
                
                // List all children for debugging
                Debug.Log("📋 Available children in XR Origin:");
                foreach (Transform child in allTransforms)
                {
                    if (child != xrOrigin.transform)
                        Debug.Log($"  - {child.name}");
                }
            }
        }

        [MenuItem("Tools/Portal System/Apply Portal Material to Hands")]
        public static void ApplyPortalMaterialToHands()
        {
            Debug.Log("🎨 Applying portal clipping material to VR hands...");

            // Find the portal clipping material
            Material clippingMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/PortalClippingMaterial.mat");
            if (clippingMat == null)
            {
                // Create clipping material
                Shader clippingShader = Shader.Find("URP/PortalClipping");
                if (clippingShader != null)
                {
                    clippingMat = new Material(clippingShader);
                    clippingMat.name = "Portal Clipping Material";
                    clippingMat.SetColor("_BaseColor", Color.white);
                    
                    // Create Materials folder if it doesn't exist
                    if (!AssetDatabase.IsValidFolder("Assets/Materials"))
                    {
                        AssetDatabase.CreateFolder("Assets", "Materials");
                    }
                    
                    AssetDatabase.CreateAsset(clippingMat, "Assets/Materials/PortalClippingMaterial.mat");
                    AssetDatabase.SaveAssets();
                    Debug.Log("✅ Created portal clipping material");
                }
                else
                {
                    Debug.LogError("❌ URP/PortalClipping shader not found!");
                    return;
                }
            }

            // Apply to all hand renderers
            var xrOrigin = FindFirstObjectByType<Unity.XR.CoreUtils.XROrigin>();
            if (xrOrigin == null)
            {
                Debug.LogError("❌ No XR Origin found!");
                return;
            }

            MeshRenderer[] renderers = xrOrigin.GetComponentsInChildren<MeshRenderer>();
            int materialsApplied = 0;

            foreach (MeshRenderer renderer in renderers)
            {
                if (renderer.name.ToLower().Contains("hand"))
                {
                    renderer.material = clippingMat;
                    Debug.Log($"✅ Applied clipping material to {renderer.name}");
                    materialsApplied++;
                }
            }

            Debug.Log($"🎯 Applied portal clipping material to {materialsApplied} hand renderers");
        }

        [MenuItem("Tools/Portal System/Debug VR Hands")]
        public static void DebugVRHands()
        {
            var xrOrigin = FindFirstObjectByType<Unity.XR.CoreUtils.XROrigin>();
            if (xrOrigin == null)
            {
                Debug.LogError("❌ No XR Origin found!");
                return;
            }

            Debug.Log("=== VR HANDS DEBUG ===");

            Transform[] allTransforms = xrOrigin.GetComponentsInChildren<Transform>();
            foreach (Transform child in allTransforms)
            {
                if (child.name.ToLower().Contains("hand"))
                {
                    var traveller = child.GetComponent<PortalTraveller>();
                    var collider = child.GetComponent<Collider>();
                    var renderer = child.GetComponentInChildren<MeshRenderer>();

                    Debug.Log($"Hand: {child.name}");
                    Debug.Log($"  - PortalTraveller: {(traveller ? "✅" : "❌")}");
                    Debug.Log($"  - Collider: {(collider ? "✅" : "❌")}");
                    Debug.Log($"  - Renderer: {(renderer ? "✅" : "❌")}");
                    Debug.Log($"  - Position: {child.position}");
                }
            }
        }
    }
}