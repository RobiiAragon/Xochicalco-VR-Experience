using UnityEngine;

namespace Xochicalco.PortalSystem
{
    public class PortalAutoFixer : MonoBehaviour
    {
        [Header("Auto Fix Options")]
        [SerializeField] private bool fixOnStart = true;
        [SerializeField] private bool fixVRHands = true;
        [SerializeField] private bool fixPortalMaterials = true;
        [SerializeField] private bool disableOldPortalSystem = true;
        [SerializeField] private KeyCode manualFixKey = KeyCode.F;
        
        void Start()
        {
            if (fixOnStart)
            {
                Invoke(nameof(DoFixes), 1f); // Wait 1 second for everything to initialize
            }
        }
        
        void Update()
        {
            // Allow manual fixing with F key
            if (Input.GetKeyDown(manualFixKey))
            {
                DoFixes();
            }
        }
        
        public void DoFixes()
        {
            Debug.Log("🔧 PortalAutoFixer: Starting automatic fixes...");
            
            if (fixVRHands)
            {
                FixVRHands();
            }
            
            if (fixPortalMaterials)
            {
                FixPortalMaterials();
            }
            
            Debug.Log("✅ PortalAutoFixer: All fixes complete!");
        }
        
        void FixVRHands()
        {
            Debug.Log("🖐️ Fixing VR Hands for portal clipping...");
            
            // Find XR Origin
            var xrOrigin = FindFirstObjectByType<Unity.XR.CoreUtils.XROrigin>();
            if (xrOrigin == null)
            {
                Debug.LogWarning("⚠️ No XR Origin found!");
                return;
            }
            
            // Find hand objects
            var handObjects = xrOrigin.GetComponentsInChildren<Transform>();
            int handsFixed = 0;
            
            foreach (var handTransform in handObjects)
            {
                string name = handTransform.name.ToLower();
                
                // Look for hand-related objects
                if (name.Contains("hand") && !name.Contains("manager") && !name.Contains("container"))
                {
                    // Check if it has a renderer (visual component)
                    var renderer = handTransform.GetComponent<Renderer>();
                    if (renderer != null)
                    {
                        // Add PortalTraveller if not present
                        var portalTraveller = handTransform.GetComponent<PortalTraveller>();
                        if (portalTraveller == null)
                        {
                            portalTraveller = handTransform.gameObject.AddComponent<PortalTraveller>();
                            Debug.Log($"✅ Added PortalTraveller to {handTransform.name}");
                        }
                        
                        // Add collider if not present
                        var collider = handTransform.GetComponent<Collider>();
                        if (collider == null)
                        {
                            var boxCollider = handTransform.gameObject.AddComponent<BoxCollider>();
                            boxCollider.isTrigger = true;
                            boxCollider.size = Vector3.one * 0.1f; // Small collider for hands
                            Debug.Log($"✅ Added Collider to {handTransform.name}");
                        }
                        
                        // Try to apply portal clipping material
                        ApplyPortalClippingMaterial(renderer);
                        
                        handsFixed++;
                    }
                }
            }
            
            Debug.Log($"🖐️ Fixed {handsFixed} hand objects for portal clipping");
        }
        
        void ApplyPortalClippingMaterial(Renderer renderer)
        {
            // Find or create portal clipping material
            Shader clippingShader = Shader.Find("Universal Render Pipeline/PortalClipping");
            if (clippingShader == null)
            {
                clippingShader = Shader.Find("URP/PortalClipping");
            }
            
            if (clippingShader != null)
            {
                Material clippingMaterial = new Material(clippingShader);
                renderer.material = clippingMaterial;
                Debug.Log($"✅ Applied portal clipping material to {renderer.name}");
            }
            else
            {
                Debug.LogWarning("⚠️ Portal clipping shader not found");
            }
        }
        
        void FixPortalMaterials()
        {
            Debug.Log("🎭 Fixing Portal Materials...");
            
            // Find all portals
            Portal[] portals = FindObjectsOfType<Portal>();
            
            if (portals.Length == 0)
            {
                Debug.LogWarning("⚠️ No portals found!");
                return;
            }
            
            // Try to find portal shader
            Shader portalShader = Shader.Find("Universal Render Pipeline/Portal");
            if (portalShader == null)
            {
                portalShader = Shader.Find("URP/Portal");
            }
            if (portalShader == null)
            {
                portalShader = Shader.Find("Unlit/Texture"); // Fallback
            }
            
            foreach (Portal portal in portals)
            {
                if (portal.screen != null)
                {
                    Material screenMaterial = portal.screen.material;
                    
                    if (screenMaterial == null)
                    {
                        screenMaterial = new Material(portalShader);
                        portal.screen.material = screenMaterial;
                        Debug.Log($"✅ Created new material for {portal.name}");
                    }
                    
                    // Ensure proper shader
                    if (screenMaterial.shader != portalShader)
                    {
                        screenMaterial.shader = portalShader;
                        Debug.Log($"✅ Updated shader for {portal.name}");
                    }
                    
                    // Set display mask
                    if (screenMaterial.HasProperty("_DisplayMask"))
                    {
                        screenMaterial.SetInt("_DisplayMask", 1);
                    }
                    
                    Debug.Log($"🎯 Fixed material for portal: {portal.name}");
                }
            }
            
            Debug.Log($"🎭 Fixed materials for {portals.Length} portals");
        }
        
        void OnGUI()
        {
            // Show instructions on screen
            GUI.Label(new Rect(10, 10, 300, 60), 
                $"Portal Auto Fixer Active\nPress {manualFixKey} to manually fix\nAuto fix on start: {fixOnStart}");
        }
    }
}