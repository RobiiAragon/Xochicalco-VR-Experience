using UnityEngine;

namespace Xochicalco.PortalSystem
{
    public class OldPortalSystemDisabler : MonoBehaviour
    {
        [Header("Auto Disable Old Portal System")]
        [SerializeField] private bool disableOnStart = true;
        
        void Start()
        {
            if (disableOnStart)
            {
                DisableOldPortalSystem();
            }
        }
        
        [ContextMenu("Disable Old Portal System")]
        public void DisableOldPortalSystem()
        {
            Debug.Log("🧹 Disabling old portal system components...");
            
            // Find and disable old MainCamera components
            var oldMainCameras = FindObjectsOfType<MainCamera>();
            foreach (var oldMainCam in oldMainCameras)
            {
                oldMainCam.enabled = false;
                Debug.Log($"✅ Disabled old MainCamera on {oldMainCam.name}");
            }
            
            // Find old Portal components (not our new ones)
            var allPortalComponents = FindObjectsOfType<MonoBehaviour>();
            foreach (var component in allPortalComponents)
            {
                // Check if it's the old Portal class (not in our namespace)
                if (component.GetType().Name == "Portal" && 
                    component.GetType().Namespace != "Xochicalco.PortalSystem")
                {
                    component.enabled = false;
                    Debug.Log($"✅ Disabled old Portal component on {component.name}");
                }
            }
            
            // Find and disable old PortalTraveller components
            foreach (var component in allPortalComponents)
            {
                if (component.GetType().Name == "PortalTraveller" && 
                    component.GetType().Namespace != "Xochicalco.PortalSystem")
                {
                    component.enabled = false;
                    Debug.Log($"✅ Disabled old PortalTraveller component on {component.name}");
                }
            }
            
            Debug.Log("🎯 Old portal system disabled successfully!");
        }
        
        [ContextMenu("Re-enable Old Portal System")]
        public void ReEnableOldPortalSystem()
        {
            Debug.Log("🔄 Re-enabling old portal system components...");
            
            // Re-enable old MainCamera components
            var oldMainCameras = FindObjectsOfType<MainCamera>();
            foreach (var oldMainCam in oldMainCameras)
            {
                oldMainCam.enabled = true;
                Debug.Log($"✅ Re-enabled old MainCamera on {oldMainCam.name}");
            }
            
            // Re-enable old Portal components
            var allPortalComponents = FindObjectsOfType<MonoBehaviour>();
            foreach (var component in allPortalComponents)
            {
                if (component.GetType().Name == "Portal" && 
                    component.GetType().Namespace != "Xochicalco.PortalSystem")
                {
                    component.enabled = true;
                    Debug.Log($"✅ Re-enabled old Portal component on {component.name}");
                }
            }
            
            Debug.Log("🎯 Old portal system re-enabled!");
        }
        
        void OnGUI()
        {
            GUI.Label(new Rect(10, 80, 300, 40), 
                "Old Portal System Disabler\nPress O to disable, P to re-enable");
        }
        
        void Update()
        {
            if (Input.GetKeyDown(KeyCode.O))
            {
                DisableOldPortalSystem();
            }
            
            if (Input.GetKeyDown(KeyCode.P))
            {
                ReEnableOldPortalSystem();
            }
        }
    }
}