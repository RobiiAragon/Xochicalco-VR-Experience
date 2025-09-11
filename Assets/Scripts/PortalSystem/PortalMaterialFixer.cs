using UnityEngine;

namespace Xochicalco.PortalSystem
{
    public static class PortalMaterialFixer
    {
        [System.Obsolete("Use from Unity Console")]
        public static void FixPortalMaterials()
        {
            Debug.Log("🔧 Starting Portal Material Fix...");
            
            // Find all portals in the scene
            Portal[] portals = Object.FindObjectsOfType<Portal>();
            
            if (portals.Length == 0)
            {
                Debug.LogWarning("⚠️ No portals found in scene!");
                return;
            }
            
            // Load the portal shader
            Shader portalShader = Shader.Find("Universal Render Pipeline/Portal");
            if (portalShader == null)
            {
                portalShader = Shader.Find("URP/Portal");
                if (portalShader == null)
                {
                    Debug.LogError("❌ Portal shader not found! Please ensure PortalShader.shader is in the project.");
                    return;
                }
            }
            
            Debug.Log($"✅ Found portal shader: {portalShader.name}");
            
            foreach (Portal portal in portals)
            {
                if (portal.screen == null)
                {
                    Debug.LogWarning($"⚠️ Portal {portal.name} has no screen assigned!");
                    continue;
                }
                
                // Check if the material exists and has the right shader
                Material screenMaterial = portal.screen.material;
                
                if (screenMaterial == null)
                {
                    Debug.Log($"🔨 Creating new material for {portal.name}");
                    screenMaterial = new Material(portalShader);
                    portal.screen.material = screenMaterial;
                }
                else if (screenMaterial.shader != portalShader)
                {
                    Debug.Log($"🔄 Updating shader for {portal.name} from {screenMaterial.shader.name} to {portalShader.name}");
                    screenMaterial.shader = portalShader;
                }
                
                // Set up material properties
                screenMaterial.SetInt("_DisplayMask", 1);
                
                // Check if texture is properly set
                if (screenMaterial.GetTexture("_MainTex") != null)
                {
                    Debug.Log($"✅ {portal.name} has texture: {screenMaterial.GetTexture("_MainTex").name}");
                }
                else
                {
                    Debug.Log($"⚠️ {portal.name} has no _MainTex assigned");
                }
                
                Debug.Log($"🎯 Portal {portal.name} material fixed - Shader: {screenMaterial.shader.name}");
            }
            
            Debug.Log("🎉 Portal material fix complete!");
        }
        
        [System.Obsolete("Use from Unity Console")]
        public static void DebugPortalMaterials()
        {
            Debug.Log("=== PORTAL MATERIALS DEBUG ===");
            
            Portal[] portals = Object.FindObjectsOfType<Portal>();
            
            foreach (Portal portal in portals)
            {
                if (portal.screen != null && portal.screen.material != null)
                {
                    Material mat = portal.screen.material;
                    Debug.Log($"Portal: {portal.name}");
                    Debug.Log($"  - Shader: {mat.shader.name}");
                    Debug.Log($"  - MainTex: {(mat.GetTexture("_MainTex") != null ? mat.GetTexture("_MainTex").name : "None")}");
                    Debug.Log($"  - DisplayMask: {mat.GetInt("_DisplayMask")}");
                    
                    // Check if it has portal shader properties
                    if (mat.HasProperty("_MainTex"))
                        Debug.Log($"  ✅ Has _MainTex property");
                    else
                        Debug.Log($"  ❌ Missing _MainTex property");
                        
                    if (mat.HasProperty("_DisplayMask"))
                        Debug.Log($"  ✅ Has _DisplayMask property");
                    else
                        Debug.Log($"  ❌ Missing _DisplayMask property");
                }
                else
                {
                    Debug.LogWarning($"❌ Portal {portal.name} has no screen or material!");
                }
            }
        }
        
        [System.Obsolete("Use from Unity Console")]
        public static void ForcePortalRender()
        {
            Debug.Log("🎬 Forcing portal render...");
            
            Portal[] portals = Object.FindObjectsOfType<Portal>();
            
            foreach (Portal portal in portals)
            {
                if (portal.linkedPortal != null)
                {
                    // Force render by calling the render method directly
                    var renderMethod = typeof(Portal).GetMethod("Render", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    if (renderMethod != null)
                    {
                        renderMethod.Invoke(portal, null);
                        Debug.Log($"🎬 Forced render for {portal.name}");
                    }
                }
            }
        }
    }
}