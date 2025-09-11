using UnityEngine;
using UnityEditor;

namespace Xochicalco.PortalSystem
{
    public class PortalSetup
    {
        [MenuItem("Tools/Portal System/Create Portal Pair")]
        public static void CreatePortalPair()
        {
            // Create portal materials if they don't exist
            Material portalMaterial = CreatePortalMaterial();
            
            // Create first portal
            GameObject portal1 = CreatePortal("Portal A", Vector3.zero, portalMaterial);
            portal1.transform.position = new Vector3(-5, 0, 0);
            
            // Create second portal
            GameObject portal2 = CreatePortal("Portal B", Vector3.zero, portalMaterial);
            portal2.transform.position = new Vector3(5, 0, 0);
            portal2.transform.rotation = Quaternion.Euler(0, 180, 0);
            
            // Link portals
            Portal portalA = portal1.GetComponent<Portal>();
            Portal portalB = portal2.GetComponent<Portal>();
            portalA.linkedPortal = portalB;
            portalB.linkedPortal = portalA;
            
            // Set up player camera for portal rendering
            SetupPlayerCamera();
            
            Debug.Log("✅ Portal pair created successfully!");
            Debug.Log("🔗 Portals are now linked and ready to use");
            
            // Select the first portal in hierarchy
            Selection.activeGameObject = portal1;
        }
        
        [MenuItem("Tools/Portal System/Create Test Objects")]
        public static void CreateTestObjects()
        {
            // Create a test cube
            GameObject testCube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            testCube.name = "Portal Test Cube";
            testCube.transform.position = new Vector3(0, 1, -2);
            
            // Add portal traveller component
            testCube.AddComponent<PortalTraveller>();
            
            // Create a test sphere  
            GameObject testSphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            testSphere.name = "Portal Test Sphere";
            testSphere.transform.position = new Vector3(2, 1, -2);
            testSphere.AddComponent<PortalTraveller>();
            
            Debug.Log("🎯 Test objects created!");
        }

        static void SetupPlayerCamera()
        {
            Camera playerCam = Camera.main;
            
            // Try to find VR camera
            var xrOrigin = Object.FindFirstObjectByType<Unity.XR.CoreUtils.XROrigin>();
            if (xrOrigin != null)
            {
                playerCam = xrOrigin.Camera;
            }
            
            if (playerCam != null)
            {
                // Add portal renderer if it doesn't exist
                PortalRenderer portalRenderer = playerCam.GetComponent<PortalRenderer>();
                if (portalRenderer == null)
                {
                    playerCam.gameObject.AddComponent<PortalRenderer>();
                    Debug.Log("🎥 Portal renderer added to player camera");
                }
            }
        }
        
        static GameObject CreatePortal(string name, Vector3 position, Material material)
        {
            // Create main portal object
            GameObject portalObj = new GameObject(name);
            portalObj.transform.position = position;
            
            // Add portal component
            Portal portal = portalObj.AddComponent<Portal>();
            
            // Create portal screen (quad)
            GameObject screen = GameObject.CreatePrimitive(PrimitiveType.Quad);
            screen.name = "Portal Screen";
            screen.transform.SetParent(portalObj.transform);
            screen.transform.localPosition = Vector3.zero;
            screen.transform.localRotation = Quaternion.identity;
            screen.transform.localScale = new Vector3(2, 3, 1);
            
            // Remove collider from screen
            Collider screenCollider = screen.GetComponent<Collider>();
            if (screenCollider) Object.DestroyImmediate(screenCollider);
            
            // Set up screen material
            MeshRenderer screenRenderer = screen.GetComponent<MeshRenderer>();
            screenRenderer.material = material;
            
            // Assign screen to portal
            portal.screen = screenRenderer;
            
            // Create trigger collider for portal detection
            BoxCollider trigger = portalObj.AddComponent<BoxCollider>();
            trigger.isTrigger = true;
            trigger.size = new Vector3(2.2f, 3.2f, 0.5f);
            
            return portalObj;
        }
        
        static Material CreatePortalMaterial()
        {
            // Try to find existing portal material
            Material existingMaterial = Resources.Load<Material>("Materials/PortalMaterial");
            if (existingMaterial != null)
                return existingMaterial;
            
            // Create new portal material using our custom URP/Portal shader
            Shader portalShader = Shader.Find("URP/Portal");
            if (portalShader == null)
            {
                Debug.LogWarning("⚠️ URP/Portal shader not found, using URP/Lit instead");
                portalShader = Shader.Find("Universal Render Pipeline/Lit");
            }
            
            Material portalMaterial = new Material(portalShader);
            portalMaterial.name = "Portal Material";
            
            // Configure material properties for portal shader
            if (portalShader.name.Contains("Portal"))
            {
                portalMaterial.SetColor("_InactiveColour", new Color(0, 0.5f, 1, 1)); // Cyan
                portalMaterial.SetInt("_DisplayMask", 1);
            }
            else
            {
                // Fallback properties for URP/Lit
                portalMaterial.SetFloat("_Metallic", 0f);
                portalMaterial.SetFloat("_Smoothness", 0.9f);
                portalMaterial.SetColor("_BaseColor", Color.cyan);
            }
            
            // Create Materials folder if it doesn't exist
            string materialsPath = "Assets/Materials";
            if (!AssetDatabase.IsValidFolder(materialsPath))
            {
                AssetDatabase.CreateFolder("Assets", "Materials");
            }
            
            // Save material as asset
            AssetDatabase.CreateAsset(portalMaterial, $"{materialsPath}/PortalMaterial.mat");
            AssetDatabase.SaveAssets();
            
            Debug.Log($"✅ Created portal material with shader: {portalShader.name}");
            
            return portalMaterial;
        }
    }
}